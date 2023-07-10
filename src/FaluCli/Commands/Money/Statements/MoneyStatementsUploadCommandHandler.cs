using ClosedXML.Excel;
using Falu.Client;

namespace Falu.Commands.Money.Statements;

internal class MoneyStatementsUploadCommandHandler : ICommandHandler
{
    private readonly FaluCliClient client;
    private readonly ILogger logger;

    public MoneyStatementsUploadCommandHandler(FaluCliClient client, ILogger<MoneyStatementsUploadCommandHandler> logger)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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

        var fileName = Path.GetFileName(filePath);
        logger.LogInformation("Uploading {FileName} ({FileSize})", fileName, size.ToBinaryString());
        using var fileContent = File.OpenRead(filePath);
        var response = await client.MoneyStatements.UploadAsync(provider: provider,
                                                                objectKind: objectKind,
                                                                fileName: fileName,
                                                                fileContent: fileContent,
                                                                cancellationToken: cancellationToken);
        response.EnsureSuccess();

        var extracted = response.Resource!.Extracted;
        var receiptNumbers = extracted.Select(r => r.Mpesa?.Receipt).ToList();
        logger.LogInformation("Uploaded statement successfully. Imported/Updated {ImportedCount} records.", extracted.Count);
        if (extracted.Count > 0)
        {
            logger.LogDebug("Imported/Updated Receipt Numbers:\r\n- {ReceiptNumbers}", string.Join("\r\n- ", receiptNumbers));
        }

        return 0;
    }
}
