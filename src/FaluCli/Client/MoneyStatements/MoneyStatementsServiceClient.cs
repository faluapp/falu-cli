using Falu.Core;
using System.Net.Http.Headers;
using System.Text.Json.Serialization.Metadata;
using SC = Falu.FaluCliJsonSerializerContext;

namespace Falu.Client.MoneyStatements;

internal class MoneyStatementsServiceClient : BaseServiceClient<MoneyStatement>,
                                              ISupportsListing<MoneyStatement, MoneyStatementsListOptions>
{
    public MoneyStatementsServiceClient(HttpClient backChannel, FaluClientOptions options) : base(backChannel, options) { }

    /// <inheritdoc/>
    protected override string BasePath => "/v1/money/statements";

    /// <inheritdoc/>
    protected override JsonTypeInfo<MoneyStatement> JsonTypeInfo => SC.Default.MoneyStatement;

    /// <inheritdoc/>
    protected override JsonTypeInfo<List<MoneyStatement>> ListJsonTypeInfo => SC.Default.ListMoneyStatement;

    /// <summary>Retrieve money statements.</summary>
    /// <inheritdoc/>
    public virtual Task<ResourceResponse<List<MoneyStatement>>> ListAsync(MoneyStatementsListOptions? options = null,
                                                                          RequestOptions? requestOptions = null,
                                                                          CancellationToken cancellationToken = default)
    {
        return ListResourcesAsync(options, requestOptions, cancellationToken);
    }

    /// <summary>Retrieve money statements recursively.</summary>
    /// <inheritdoc/>
    public virtual IAsyncEnumerable<MoneyStatement> ListRecursivelyAsync(MoneyStatementsListOptions? options = null,
                                                                         RequestOptions? requestOptions = null,
                                                                         CancellationToken cancellationToken = default)
    {
        return ListResourcesRecursivelyAsync(options, requestOptions, cancellationToken);
    }

    public virtual Task<ResourceResponse<MoneyStatementUploadResponse>> UploadAsync(string provider,
                                                                                    string objectKind,
                                                                                    string fileName,
                                                                                    Stream fileContent,
                                                                                    string? fileContentType,
                                                                                    RequestOptions? options = null,
                                                                                    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileName, nameof(fileName));
        ArgumentNullException.ThrowIfNull(fileContent, nameof(fileContent));

        var streamContent = new StreamContent(fileContent);
        if (fileContentType is not null)
        {
            streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(fileContentType);
        }

        // prepare the request and execute
        var uri = $"/v1/money/statements/upload";
        var content = new MultipartFormDataContent
        {
            { new StringContent(provider), "type" },
            { new StringContent(objectKind), "objects_kind" },
            { streamContent, "file", fileName },
        };

        return RequestAsync(uri, HttpMethod.Post, SC.Default.MoneyStatementUploadResponse, content, options, cancellationToken);
    }
}
