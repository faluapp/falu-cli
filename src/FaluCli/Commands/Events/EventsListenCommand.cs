using Res = Falu.Properties.Resources;

namespace Falu.Commands.Events;

internal class EventsListenCommand : Command
{
    public EventsListenCommand() : base("listen", "Listen to events")
    {
        this.AddOption(aliases: new[] { "--webhook-endpoint" },
                       description: Res.OptionDescriptionEventListenWebhookEndpoint,
                       format: Constants.WebhookEndpointIdFormat);

        this.AddOption<string[]>(new[] { "--event-type", "--type", "-t", },
                                 description: Res.OptionDescriptionEventListenEventTypes);

        this.AddOption<Uri>(new[] { "--forward-to", "-f", },
                            description: Res.OptionDescriptionEventListenForwardTo);

        this.AddOption(new[] { "--skip-validation", },
                       description: Res.OptionDescriptionEventListenSkipValidation,
                       defaultValue: false);

        this.AddOption<string>(new[] { "--webhook-secret", "--secret", },
                               description: Res.OptionDescriptionEventListenWebhookSecret);
    }
}
