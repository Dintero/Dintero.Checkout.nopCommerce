using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;
using System;

namespace Nop.Plugin.Payments.Dintero.Models;

public record ConfigurationModel : BaseNopModel
{
    public int ActiveStoreScopeConfiguration { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.LogEnabled")]
    public bool LogEnabled { get; set; }

    public bool LogEnabled_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.SandboxURL")]
    public string SandboxURL { get; set; }

    public bool SandboxURL_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.ProductionURL")]
    public string ProductionURL { get; set; }

    public bool ProductionURL_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.UseSandbox")]
    public bool UseSandbox { get; set; }
    public bool UseSandbox_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.TransactModeValues")]
    public int TransactModeId { get; set; }
    public bool TransactModeId_OverrideForStore { get; set; }
    public SelectList TransactModeValues { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.SecretKey")]
    public string SecretKey { get; set; }
    public bool SecretKey_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.ClientId")]
    public string ClientId { get; set; }
    public bool ClientId_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.AccountId")]
    public string AccountId { get; set; }
    public bool AccountId_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.AdditionalFee")]
    public decimal AdditionalFee { get; set; }
    public bool AdditionalFee_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.AdditionalFeePercentage")]
    public bool AdditionalFeePercentage { get; set; }
    public bool AdditionalFeePercentage_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.UseShippingAddressAsBilling")]
    public bool UseShippingAddressAsBilling { get; set; }
    public bool UseShippingAddressAsBilling_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.AccessToken")]
    public string AccessToken { get; set; }

    public bool AccessToken_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.TokenExpiresIn")]

    public int TokenExpiresIn { get; set; }

    public bool TokenExpiresIn_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.TokenExpiresInDatetimeUTC")]

    public DateTime TokenExpiresInDatetimeUTC { get; set; }

    public bool TokenExpiresInDatetimeUTC_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.ProfileId")]
    public string ProfileId { get; set; }
    public bool ProfileId_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.DefaultAddressId")]
    public int DefaultAddressId { get; set; }
    public bool DefaultAddressId_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.FallbackPhoneNo")]
    public string FallbackPhoneNo { get; set; }
    public bool FallbackPhoneNo_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.SandboxDinteroCheckoutWebSDKEndpoint")]
    public string SandboxDinteroCheckoutWebSDKEndpoint { get; set; }
    public bool SandboxDinteroCheckoutWebSDKEndpoint_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.SandboxAuthEndpoint")]
    public string SandboxAuthEndpoint { get; set; }
    public bool SandboxAuthEndpoint_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.ProductionDinteroCheckoutWebSDKEndpoint")]
    public string ProductionDinteroCheckoutWebSDKEndpoint { get; set; }
    public bool ProductionDinteroCheckoutWebSDKEndpoint_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.ProductionAuthEndpoint")]
    public string ProductionAuthEndpoint { get; set; }
    public bool ProductionAuthEndpoint_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.SandboxAuthAudience")]
    public string SandboxAuthAudience { get; set; }
    public bool SandboxAuthAudience_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Dintero.Fields.ProductionAuthAudience")]
    public string ProductionAuthAudience { get; set; }
    public bool ProductionAuthAudience_OverrideForStore { get; set; }
}