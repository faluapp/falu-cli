﻿using Falu.Core;
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
