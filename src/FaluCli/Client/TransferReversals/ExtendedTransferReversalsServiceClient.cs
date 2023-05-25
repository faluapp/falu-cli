using Falu.Client.Money;
using Falu.Core;
using Falu.TransferReversals;

namespace Falu.Client.TransferReversals;

internal class ExtendedTransferReversalsServiceClient : TransferReversalsServiceClient, ISupportsUploadingMpesaStatement
{
    public ExtendedTransferReversalsServiceClient(HttpClient backChannel, FaluClientOptions options) : base(backChannel, options) { }

    #region ISupportsUploadingMpesaStatement members

    string ISupportsUploadingMpesaStatement.ObjectKind => "transfer_reversals";

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
