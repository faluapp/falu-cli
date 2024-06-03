using ClosedXML.Excel;

namespace Falu.Commands.Money.Statements;

internal class MoneyStatementsUploadCommand : WorkspacedCommand
{
    public MoneyStatementsUploadCommand() : base("upload", "Upload a statement to Falu to resolve pending payments, transfers, refunds, or reversals for bring-your-own providers.")
    {
        this.AddArgument<string>(name: "object-kind",
                                 description: "The object type to upload the statement against.",
                                 configure: o => o.AcceptOnlyFromAmong("payments", "payment_refunds", "transfers", "transfer_reversals"));

        this.AddOption<string>(["-f", "--file"],
                               description: $"File path for the statement file (up to {Constants.MaxStatementFileSizeString}).",
                               configure: o => o.Required = true);

        this.AddOption(["--provider"],
                       description: "Type of statement",
                       defaultValue: "mpesa",
                       configure: o => o.AcceptOnlyFromAmong("mpesa"));
    }

    public override async Task<int> ExecuteAsync(CliCommandExecutionContext context, CancellationToken cancellationToken)
    {
        var objectKind = context.ParseResult.ValueForArgument<string>("object-kind")!;
        var filePath = context.ParseResult.ValueForOption<string>("--file")!;
        var provider = context.ParseResult.ValueForOption<string>("--provider")!;

        // ensure the file exists
        var info = new FileInfo(filePath);
        if (!info.Exists)
        {
            context.Logger.LogError("The file {FilePath} does not exist.", filePath);
            return -1;
        }

        // ensure the file size does not exceed the limit
        var size = ByteSizeLib.ByteSize.FromBytes(info.Length);
        if (size > Constants.MaxStatementFileSize)
        {
            context.Logger.LogError("The file provided exceeds the size limit of {SizeLimit}. Trying exporting a smaller date range.", Constants.MaxStatementFileSizeString);
            return -1;
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
                context.Logger.LogError("The provided for MPESA must be a valid Excel file without a password.");
                return -1;
            }
        }

        await using var fileContent = File.OpenRead(filePath);
        var fileFormats = new FileSignatures.FileFormat[] { new FileSignatures.Formats.Excel(), new FileSignatures.Formats.ExcelLegacy(), };
        var formatInspector = new FileSignatures.FileFormatInspector(fileFormats);
        var fileFormat = formatInspector.DetermineFileFormat(fileContent);
        if (fileFormat is null)
        {
            context.Logger.LogError("Unable to determine file format. Only Excel workbooks are allowed.");
            return -1;
        }

        var fileName = Path.GetFileName(filePath);
        var fileContentType = fileFormat.MediaType;
        context.Logger.LogInformation("Uploading {FileName} ({FileSize})", fileName, size.ToBinaryString());
        var response = await context.Client.MoneyStatements.UploadAsync(provider: provider,
                                                                           objectKind: objectKind,
                                                                           fileName: fileName,
                                                                           fileContent: fileContent,
                                                                           fileContentType: fileContentType,
                                                                           cancellationToken: cancellationToken);
        response.EnsureSuccess();

        var statement = response.Resource!;
        var extracted = statement.Extracted;
        context.Logger.LogInformation("Uploaded statement {StatementId} successfully.", statement.Id);
        context.Logger.LogInformation("Imported/Updated {ImportedCount} records.", extracted.Count);
        if (extracted.Count > 0)
        {
            var receiptNumbers = extracted.Select(r => r.Mpesa?.Receipt).ToList();
            context.Logger.LogDebug("MPESA Receipt Numbers:\r\n- {ReceiptNumbers}", string.Join("\r\n- ", receiptNumbers));
        }

        return 0;
    }
}
