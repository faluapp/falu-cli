using Falu.Client.Money;
using Falu.Core;
using Falu.Payments;

namespace Falu.Client.Payments;

internal class ExtendedPaymentsServiceClient : PaymentsServiceClient, ISupportsUploadingMpesaStatement
{
    public ExtendedPaymentsServiceClient(HttpClient backChannel, FaluClientOptions options) : base(backChannel, options) { }

    #region ISupportsUploadingMpesaStatement members

    string ISupportsUploadingMpesaStatement.ObjectKind => "payments";

    Task<ResourceResponse<List<ExtractedStatementRecord>>> ISupportsUploadingMpesaStatement.RequestAsync(string uri,
                                                                                                         HttpMethod method,
                                                                                                         HttpContent? content,
                                                                                                         RequestOptions? options,
                                                                                                         CancellationToken cancellationToken)
    {
        return base.RequestAsync(uri, method, FaluCliJsonSerializerContext.Default.ListExtractedStatementRecord, content, options, cancellationToken);
    }

    #endregion

}
