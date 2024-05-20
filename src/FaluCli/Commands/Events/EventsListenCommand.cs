using Tingle.Extensions.Primitives;
using Res = Falu.Properties.Resources;

namespace Falu.Commands.Events;

internal class EventsListenCommand : Command
{
    public EventsListenCommand() : base("listen", "Listen to events")
    {
        this.AddOption(aliases: ["--webhook-endpoint"],
                       description: Res.OptionDescriptionEventListenWebhookEndpoint,
                       format: Constants.WebhookEndpointIdFormat);

        this.AddOption<string[]>(["--event-type", "--type", "-t"],
                                 description: Res.OptionDescriptionEventListenEventTypes,
                                 validate: or =>
                                 {
                                     var values = or.GetValueOrDefault<string[]>();
                                     if (values is not null)
                                     {
                                         foreach (var v in values)
                                         {
                                             if (!Constants.EventTypeWildcardFormat.IsMatch(v))
                                             {
                                                 or.ErrorMessage = string.Format(Res.InvalidEventTypeWildcard, v);
                                                 break;
                                             }
                                         }
                                     }
                                 });

        this.AddOption<Uri>(["--forward-to", "-f"],
                            description: Res.OptionDescriptionEventListenForwardTo);

        this.AddOption(["--skip-validation"],
                       description: Res.OptionDescriptionEventListenSkipValidation,
                       defaultValue: false);

        this.AddOption<string>(["--webhook-secret", "--secret"],
                               description: Res.OptionDescriptionEventListenWebhookSecret);

        this.AddOption(["--ttl"],
                       description: Res.OptionDescriptionRealtimeConnectionTtl,
                       defaultValue: "PT60M",
                       validate: or =>
                       {
                           var value = or.GetValueOrDefault<string>();
                           if (value is not null && !Duration.TryParse(value, out _))
                           {
                               or.ErrorMessage = string.Format(Res.InvalidDurationValue, value);
                           }
                       });
    }
}
