namespace Nop.Plugin.Payments.Dintero.Infrastructure;

/// <summary>
/// Represents plugin constants
/// </summary>
public partial class Defaults
{
    /// <summary>
    /// Gets a name of the view component to display payment info in public store
    /// </summary>
    public const string PAYMENT_INFO_VIEW_COMPONENT_NAME = "DinteroPaymentInfo";

    /// <summary>
    /// Gets a name of the view component to display dintero checkout button in public store
    /// </summary>
    public const string DINTERO_CHECKOUT_VIEW_COMPONENT_NAME = "DinteroCheckout";

    /// <summary>
    /// Gets the plugin system name
    /// </summary>
    public const string SYSTEM_NAME = "Payments.Dintero";
}
