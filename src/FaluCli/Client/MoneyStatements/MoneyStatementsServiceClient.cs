using Falu.Core;
using SC = Falu.FaluCliJsonSerializerContext;

namespace Falu.Client.MoneyStatements;

internal class MoneyStatementsServiceClient : BaseServiceClient
{
    public MoneyStatementsServiceClient(HttpClient backChannel, FaluClientOptions options) : base(backChannel, options) { }

    public virtual Task<ResourceResponse<MoneyStatementUploadResponse>> UploadAsync(string provider,
                                                                                    string objectKind,
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
        var uri = $"/v1/money/statements/upload";
        var content = new MultipartFormDataContent
        {
            { new StringContent(provider), "type" },
            { new StringContent(objectKind), "objects_kind" },
            { new StreamContent(fileContent), "file", fileName },
        };

        return RequestAsync(uri, HttpMethod.Post, SC.Default.MoneyStatementUploadResponse, content, options, cancellationToken);
    }
}
