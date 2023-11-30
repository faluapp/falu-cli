using ClosedXML.Excel;
using Falu.Client;

namespace Falu.Commands.Money.Statements;

internal class MoneyStatementsUploadCommandHandler(FaluCliClient client, ILogger<MoneyStatementsUploadCommandHandler> logger) : ICommandHandler
{
    int ICommandHandler.Invoke(InvocationContext context) => throw new NotImplementedException();

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();
        var objectKind = context.ParseResult.ValueForArgument<string>("object-kind")!;
        var filePath = context.ParseResult.ValueForOption<string>("--file")!;
        var provider = context.ParseResult.ValueForOption<string>("--provider")!;

        // ensure the file exists
        var info = new FileInfo(filePath);
        if (!info.Exists)
        {
            logger.LogError("The file {FilePath} does not exist.", filePath);
            return -1;
        }

        // ensure the file size does not exceed the limit
        var size = ByteSizeLib.ByteSize.FromBytes(info.Length);
        if (size > Constants.MaxStatementFileSize)
        {
            logger.LogError("The file provided exceeds the size limit of {SizeLimit}. Trying exporting a smaller date range.", Constants.MaxStatementFileSizeString);
            return -1;
        }

        // ensure the file can be opened
        if (provider == "mpesa")
        {
            using var stream = File.OpenRead(filePath);
            try
            {
                using var workbook = new XLWorkbook(stream);
            }
            catch (Exception)
            {
                logger.LogError("The provided for MPESA must be a valid Excel file without a password.");
                return -1;
            }
        }

        using var fileContent = File.OpenRead(filePath);
        var fileFormats = new FileSignatures.FileFormat[] {
            new FileSignatures.Formats.Excel(),
            new FileSignatures.Formats.ExcelLegacy(),
        };
        var formatInspector = new FileSignatures.FileFormatInspector(fileFormats);
        var fileFormat = formatInspector.DetermineFileFormat(fileContent);
        if (fileFormat is null)
        {
            logger.LogError("Unable to determine file format. Only Excel workbooks are allowed.");
            return -1;
        }

        var fileName = Path.GetFileName(filePath);
        var fileContentType = fileFormat.MediaType;
        logger.LogInformation("Uploading {FileName} ({FileSize})", fileName, size.ToBinaryString());
        var response = await client.MoneyStatements.UploadAsync(provider: provider,
                                                                objectKind: objectKind,
                                                                fileName: fileName,
                                                                fileContent: fileContent,
                                                                fileContentType: fileContentType,
                                                                cancellationToken: cancellationToken);
        response.EnsureSuccess();

        var statement = response.Resource!;
        var extracted = statement.Extracted;
        logger.LogInformation("Uploaded statement {StatementId} successfully.", statement.Id);
        logger.LogInformation("Imported/Updated {ImportedCount} records.", extracted.Count);
        if (extracted.Count > 0)
        {
            var receiptNumbers = extracted.Select(r => r.Mpesa?.Receipt).ToList();
            logger.LogDebug("MPESA Receipt Numbers:\r\n- {ReceiptNumbers}", string.Join("\r\n- ", receiptNumbers));
        }

        return 0;
    }
}
