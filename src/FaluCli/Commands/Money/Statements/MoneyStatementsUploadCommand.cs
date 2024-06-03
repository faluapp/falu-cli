using ClosedXML.Excel;
using Falu.Client;

namespace Falu.Commands.Money.Statements;

internal class MoneyStatementsUploadCommand : Command
{
    public MoneyStatementsUploadCommand() : base("upload", "Upload a statement to Falu to resolve pending payments, transfers, refunds, or reversals for bring-your-own providers.")
    {
        this.AddArgument<string>(name: "object-kind",
                                 description: "The object type to upload the statement against.",
                                 configure: o => o.FromAmong("payments", "payment_refunds", "transfers", "transfer_reversals"));

        this.AddOption<string>(["-f", "--file"],
                               description: $"File path for the statement file (up to {Constants.MaxStatementFileSizeString}).",
                               configure: o => o.IsRequired = true);

        this.AddOption(["--provider"],
                       description: "Type of statement",
                       defaultValue: "mpesa",
                       configure: o => o.FromAmong("mpesa"));

        this.SetHandler(HandleAsync);
    }

    private static async Task HandleAsync(InvocationContext context)
    {
        var cancellationToken = context.GetCancellationToken();
        var client = context.GetRequiredService<FaluCliClient>();
        var logger = context.GetRequiredService<ILogger<MoneyStatementsUploadCommand>>();

        var objectKind = context.ParseResult.ValueForArgument<string>("object-kind")!;
        var filePath = context.ParseResult.ValueForOption<string>("--file")!;
        var provider = context.ParseResult.ValueForOption<string>("--provider")!;

        // ensure the file exists
        var info = new FileInfo(filePath);
        if (!info.Exists)
        {
            logger.LogError("The file {FilePath} does not exist.", filePath);
            context.ExitCode = -1;
            return;
        }

        // ensure the file size does not exceed the limit
        var size = ByteSizeLib.ByteSize.FromBytes(info.Length);
        if (size > Constants.MaxStatementFileSize)
        {
            logger.LogError("The file provided exceeds the size limit of {SizeLimit}. Trying exporting a smaller date range.", Constants.MaxStatementFileSizeString);
            context.ExitCode = -1;
            return;
        }

        // ensure the file can be opened
        if (provider == "mpesa")
        {
            await using var stream = File.OpenRead(filePath);
            try
            {
                using var workbook = new XLWorkbook(stream);
            }
            catch (Exception)
            {
                logger.LogError("The provided for MPESA must be a valid Excel file without a password.");
                context.ExitCode = -1;
                return;
            }
        }

        await using var fileContent = File.OpenRead(filePath);
        var fileFormats = new FileSignatures.FileFormat[] { new FileSignatures.Formats.Excel(), new FileSignatures.Formats.ExcelLegacy(), };
        var formatInspector = new FileSignatures.FileFormatInspector(fileFormats);
        var fileFormat = formatInspector.DetermineFileFormat(fileContent);
        if (fileFormat is null)
        {
            logger.LogError("Unable to determine file format. Only Excel workbooks are allowed.");
            context.ExitCode = -1;
            return;
        }

        var fileName = Path.GetFileName(filePath);
        var fileContentType = fileFormat.MediaType;
        logger.LogInformation("Uploading {FileName} ({FileSize})", fileName, size.ToBinaryString());
        var response = await client.MoneyStatements.UploadAsync(provider: provider, objectKind: objectKind, fileName: fileName, fileContent: fileContent, fileContentType: fileContentType, cancellationToken: cancellationToken);
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
    }
}
