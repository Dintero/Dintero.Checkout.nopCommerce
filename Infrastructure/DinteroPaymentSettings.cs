using Nop.Core.Configuration;
using Nop.Plugin.Payments.Dintero.Models;
using System;

namespace Nop.Plugin.Payments.Dintero.Infrastructure;

public class DinteroPaymentSettings : ISettings
{
    /// <summary>
    /// Gets or sets a log enabled
    /// </summary>
    public bool LogEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use sandbox environment
    /// </summary>
    public bool UseSandbox { get; set; }

    /// <summary>
    /// Gets or sets the sandbox url
    /// </summary>
    public string SandboxURL { get; set; }

    /// <summary>
    /// Gets or sets the production url
    /// </summary>
    public string ProductionURL { get; set; }

    /// <summary>
    /// Gets or sets the payment processor transaction mode
    /// </summary>
    public TransactMode TransactMode { get; set; }

    /// <summary>
    /// Gets or sets the Dintero transaction key
    /// </summary>
    public string SecretKey { get; set; }

    /// <summary>
    /// Gets or sets the Dintero client ID
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// Gets or sets the Dintero account ID
    /// </summary>
    public string AccountId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use a shipping address as a billing address 
    /// </summary>
    public bool UseShippingAddressAsBilling { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
    /// </summary>
    public bool AdditionalFeePercentage { get; set; }

    /// <summary>
    /// Gets or sets an additional fee
    /// </summary>
    public decimal AdditionalFee { get; set; }

    /// <summary>
    /// Gets or sets an access token
    /// </summary>
    public string AccessToken { get; set; }

    /// <summary>
    /// Gets or sets an token expires in
    /// </summary>
    public int TokenExpiresIn { get; set; }

    /// <summary>
    /// Gets or sets an token expires in datetime UTC
    /// </summary>
    public DateTime TokenExpiresInDatetimeUTC { get; set; }

    /// <summary>
    /// Gets or sets an profile identifier
    /// </summary>
    public string ProfileId { get; set; }

    /// <summary>
    /// Gets or sets an default address identifier
    /// </summary>
    public int DefaultAddressId { get; set; }

    /// <summary>
    /// Gets or sets an fallback phone number
    /// </summary>
    public string FallbackPhoneNo { get; set; }

    /// <summary>
    /// Gets or sets an production dintero checkout web SDK endpoint
    /// </summary>
    public string ProductionDinteroCheckoutWebSDKEndpoint { get; set; }

    /// <summary>
    /// Gets or sets an production dintero auth endpoint
    /// </summary>
    public string ProductionAuthEndpoint { get; set; }

    /// <summary>
    /// Gets or sets an sand box dintero checkout web SDK endpoint
    /// </summary>
    public string SandboxDinteroCheckoutWebSDKEndpoint { get; set; }

    /// <summary>
    /// Gets or sets an sand box dintero auth endpoint
    /// </summary>
    public string SandboxAuthEndpoint { get; set; }

    /// <summary>
    /// Gets or sets an sand box dintero auth audience
    /// </summary>
    public string SandboxAuthAudience { get; set; }

    /// <summary>
    /// Gets or sets an sand box dintero auth audience
    /// </summary>
    public string ProductionAuthAudience { get; set; }
}
