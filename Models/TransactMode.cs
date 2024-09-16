namespace Nop.Plugin.Payments.Dintero.Models;

/// <summary>
/// Represents Dintero payment processor transaction mode
/// </summary>
public enum TransactMode
{
    /// <summary>
    /// Authorize
    /// </summary>
    Authorize = 1,

    /// <summary>
    /// Authorize and capture
    /// </summary>
    AuthorizeAndCapture = 2
}
