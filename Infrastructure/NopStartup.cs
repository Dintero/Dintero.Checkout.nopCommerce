using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Payments.Dintero.Controllers;
using Nop.Plugin.Payments.Dintero.Services;
using Nop.Services.Orders;
using Nop.Web.Controllers;

namespace Nop.Plugin.Payments.Dintero.Infrastructure;

/// <summary>
/// Represents object for the configuring services on application startup
/// </summary>
public class NopStartup : INopStartup
{
    /// <summary>
    /// Add and configure any of the middleware
    /// </summary>
    /// <param name="services">Collection of service descriptors</param>
    /// <param name="configuration">Configuration of the application</param>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        //override services
        services.AddSingleton(typeof(IDinteroHttpClient), typeof(DinteroHttpClient));
        services.AddSingleton(typeof(IOverrideOrderProcessingService), typeof(OverrideOrderProcessingService));
        services.AddSingleton(typeof(CheckoutController), typeof(OverrideCheckoutController));
        services.AddSingleton(typeof(OrderProcessingService), typeof(OverrideOrderProcessingService));
        services.AddSingleton(typeof(IOrderProcessingService), typeof(OverrideOrderProcessingService));
    }

    /// <summary>
    /// Configure the using of added middleware
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public void Configure(IApplicationBuilder application)
    {
    }

    /// <summary>
    /// Gets order of this startup configuration implementation
    /// </summary>
    public int Order => int.MaxValue;
}