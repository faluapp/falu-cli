﻿using Falu.Core;

namespace Falu.Client.Money;

internal static class ISupportsUploadingMpesaStatementExtensions
{
    public static Task<ResourceResponse<List<ExtractedStatementRecord>>> UploadMpesaAsync(this ISupportsUploadingMpesaStatement client,
                                                                                          string fileName,
                                                                                          Stream fileContent,
                                                                                          RequestOptions? options = null,
                                                                                          CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client, nameof(client));
        ArgumentNullException.ThrowIfNull(fileName, nameof(fileName));
        ArgumentNullException.ThrowIfNull(fileContent, nameof(fileContent));

        // all requests for uploading statements should be live
        options ??= new RequestOptions();
        options.Live = true;

        // prepare the request and execute
        var uri = $"/v1/money/statements/upload/{client.ObjectKind}";
        var content = new MultipartFormDataContent
        {
            { new StringContent("mpesa"), "type" },
            { new StreamContent(fileContent), "file", fileName },
        };
        return client.RequestAsync<List<ExtractedStatementRecord>>(uri, HttpMethod.Post, content, options, cancellationToken);
    }
}
