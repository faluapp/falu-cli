using Falu.Client.Events;
using Falu.Client.MoneyStatements;
using Falu.Client.Realtime;
using Microsoft.Extensions.Options;

namespace Falu.Client;

internal class FaluCliClient : FaluClient
{
    public FaluCliClient(HttpClient backChannel, IOptionsSnapshot<FaluClientOptions> optionsAccessor)
        : base(backChannel, optionsAccessor)
    {
        Events = new ExtendedEventsServiceClient(BackChannel, Options);
        MoneyStatements = new MoneyStatementsServiceClient(BackChannel, Options);
        Realtime = new RealtimeServiceClient(BackChannel, Options);
    }

    public new ExtendedEventsServiceClient Events { get; protected set; }
    public MoneyStatementsServiceClient MoneyStatements { get; protected set; }
    public RealtimeServiceClient Realtime { get; protected set; }
}
