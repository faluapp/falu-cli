using Falu.Core;
using SC = Falu.FaluCliJsonSerializerContext;

namespace Falu.Client.MoneyStatements;

internal class MoneyStatementsServiceClient : BaseServiceClient
{
    public MoneyStatementsServiceClient(HttpClient backChannel, FaluClientOptions options) : base(backChannel, options) { }

    public virtual Task<ResourceResponse<List<ExtractedStatementRecord>>> UploadAsync(string objectKind,
                                                                                      string provider,
                                                                                      string fileName,
                                                                                      Stream fileContent,
                                                                                      RequestOptions? options = null,
                                                                                      CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileName, nameof(fileName));
        ArgumentNullException.ThrowIfNull(fileContent, nameof(fileContent));

        // all requests for uploading statements should be live
        options ??= new RequestOptions();
        options.Live = true; // TODO: validate this in the command handler instead

        // prepare the request and execute
        var uri = $"/v1/money/statements/upload/{objectKind}";
        var content = new MultipartFormDataContent
        {
            { new StringContent(provider), "type" },
            { new StreamContent(fileContent), "file", fileName },
        };

        return RequestAsync(uri, HttpMethod.Post, SC.Default.ListExtractedStatementRecord, content, options, cancellationToken);
    }
}
