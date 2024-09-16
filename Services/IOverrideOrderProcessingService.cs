using Nop.Services.Orders;
using Nop.Services.Payments;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.Dintero.Services;

/// <summary>
/// dintero http client interface
/// </summary>
public interface IOverrideOrderProcessingService
{
    /// <summary>
    /// Places an order
    /// </summary>
    /// <param name="processPaymentRequest">Process payment request</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the place order result
    /// </returns>
    Task<PlaceOrderResult> PlaceOrderAsync(ProcessPaymentRequest processPaymentRequest);
}
