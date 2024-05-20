using System.Net;
using Tingle.Extensions.Primitives;
using Res = Falu.Properties.Resources;

namespace Falu.Commands.RequestLogs;

internal class RequestLogsTailCommand : Command
{
    public RequestLogsTailCommand() : base("tail", "Tail request logs")
    {
        this.AddOption<IPNetwork[]>(["--ip-network", "--network"],
                                    description: "The IP network to filter for.");

        this.AddOption<IPAddress[]>(["--ip-address", "--ip"],
                                    description: "The IP address to filter for.");

        this.AddOption<string[]>(["--http-method", "--method"],
                                 description: "The HTTP method to filter for.",
                                 configure: o => o.FromAmong("get", "patch", "post", "put", "delete"));

        this.AddOption<string[]>(["--request-path", "--path"],
                                 description: "The request path to filter for. For example: \"/v1/messages\"",
                                 validate: or =>
                                 {
                                     var values = or.GetValueOrDefault<string[]>();
                                     if (values is not null)
                                     {
                                         foreach (var v in values)
                                         {
                                             if (Constants.RequestPathWildcardFormat.IsMatch(v))
                                             {
                                                 or.ErrorMessage = string.Format(Res.InvalidHttpRequestPath, v);
                                                 break;
                                             }
                                         }
                                     }
                                 });

        this.AddOption<string[]>(["--source"],
                                 description: "The request source to filter for.",
                                 configure: o => o.FromAmong("dashboard", "api"));

        this.AddOption<int[]>(["--status-code"],
                              description: "The HTTP status code to filter for.",
                              validate: (or) =>
                              {
                                  var values = or.GetValueOrDefault<int[]>();
                                  if (values is not null)
                                  {
                                      foreach (var v in values)
                                      {
                                          if (v < 200 || v > 599)
                                          {
                                              or.ErrorMessage = string.Format(Res.InvalidHttpStatusCode, v);
                                              break;
                                          }
                                      }
                                  }
                              });

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
