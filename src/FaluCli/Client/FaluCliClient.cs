using Falu.Client.Events;
using Falu.Client.MoneyStatements;
using Falu.Client.Realtime;
using Falu.Client.Workspaces;
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
        Workspaces = new WorkspacesServiceClient(BackChannel, Options);
    }

    public new ExtendedEventsServiceClient Events { get; protected set; }
    public MoneyStatementsServiceClient MoneyStatements { get; protected set; }
    public RealtimeServiceClient Realtime { get; protected set; }
    public WorkspacesServiceClient Workspaces { get; protected set; }
}
