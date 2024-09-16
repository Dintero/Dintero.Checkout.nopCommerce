using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;
using Nop.Web.Infrastructure;

namespace Nop.Plugin.Payments.Dintero.Infrastructure;

/// <summary>
/// Represents plugin route provider
/// </summary>
public class RouteProvider : BaseRouteProvider, IRouteProvider
{
    /// <summary>
    /// Register routes
    /// </summary>
    /// <param name="endpointRouteBuilder">Route builder</param>
    public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
    {
        var lang = GetLanguageRoutePattern();

        endpointRouteBuilder.MapControllerRoute("Checkout", $"{lang}/checkout/",
            new { controller = "OverrideCheckout", action = "DinteroIndex" });

        endpointRouteBuilder.MapControllerRoute("DinteroCheckout", "DinteroCheckout",
              new { controller = "DinteroCheckout", action = "DinteroCheckout" });

        //IPN
        endpointRouteBuilder.MapControllerRoute("Plugin.Payments.Dintero.IPNHandler", "Plugins/DinteroHandler/IPNHandler",
            new { controller = "DinteroHandler", action = "IPNHandler" });

        //CallBack
        endpointRouteBuilder.MapControllerRoute("Plugin.Payments.Dintero.CallBackHandler", "Plugins/DinteroHandler/CallBackHandler",
            new { controller = "DinteroHandler", action = "CallBackHandler" });
    }

    /// <summary>
    /// Gets a priority of route provider
    /// </summary>
    public int Priority => 1;
}