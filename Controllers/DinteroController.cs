using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.Dintero.Infrastructure;
using Nop.Plugin.Payments.Dintero.Models;
using Nop.Plugin.Payments.Dintero.Services;
using Nop.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.Dintero.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public class DinteroController : BasePaymentController
{
    #region Fields

    private readonly ILocalizationService _localizationService;
    private readonly INotificationService _notificationService;
    private readonly IPermissionService _permissionService;
    private readonly ISettingService _settingService;
    private readonly IStoreContext _storeContext;
    private readonly IDinteroHttpClient _dinteroHttpClient;

    #endregion

    #region Ctor

    public DinteroController(ILocalizationService localizationService,
        INotificationService notificationService,
        IPermissionService permissionService,
        ISettingService settingService,
        IStoreContext storeContext,
        IDinteroHttpClient dinteroHttpClient)
    {
        _localizationService = localizationService;
        _notificationService = notificationService;
        _permissionService = permissionService;
        _settingService = settingService;
        _storeContext = storeContext;
        _dinteroHttpClient = dinteroHttpClient;
    }

    #endregion

    #region Methods

    public async Task<IActionResult> Configure()
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
            return AccessDeniedView();

        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var dinteroPaymentSettings = await _settingService.LoadSettingAsync<DinteroPaymentSettings>(storeScope);

        var model = new ConfigurationModel
        {
            UseSandbox = dinteroPaymentSettings.UseSandbox,
            SandboxURL = dinteroPaymentSettings.SandboxURL,
            ProductionURL = dinteroPaymentSettings.ProductionURL,
            UseShippingAddressAsBilling = dinteroPaymentSettings.UseShippingAddressAsBilling,
            TransactModeId = Convert.ToInt32(dinteroPaymentSettings.TransactMode),
            SecretKey = dinteroPaymentSettings.SecretKey,
            ClientId = dinteroPaymentSettings.ClientId,
            AccountId = dinteroPaymentSettings.AccountId,
            AdditionalFee = dinteroPaymentSettings.AdditionalFee,
            AdditionalFeePercentage = dinteroPaymentSettings.AdditionalFeePercentage,
            TransactModeValues = await dinteroPaymentSettings.TransactMode.ToSelectListAsync(),
            ActiveStoreScopeConfiguration = storeScope,
            LogEnabled = dinteroPaymentSettings.LogEnabled,
            ProfileId = dinteroPaymentSettings.ProfileId,
            DefaultAddressId = dinteroPaymentSettings.DefaultAddressId,
            FallbackPhoneNo = dinteroPaymentSettings.FallbackPhoneNo,
            ProductionDinteroCheckoutWebSDKEndpoint = dinteroPaymentSettings.ProductionDinteroCheckoutWebSDKEndpoint,
            ProductionAuthEndpoint = dinteroPaymentSettings.ProductionAuthEndpoint,
            SandboxDinteroCheckoutWebSDKEndpoint = dinteroPaymentSettings.SandboxDinteroCheckoutWebSDKEndpoint,
            SandboxAuthEndpoint = dinteroPaymentSettings.SandboxAuthEndpoint,
            SandboxAuthAudience = dinteroPaymentSettings.SandboxAuthAudience,
            ProductionAuthAudience = dinteroPaymentSettings.ProductionAuthAudience
        };

        if (storeScope > 0)
        {
            model.UseSandbox_OverrideForStore = await _settingService.SettingExistsAsync(dinteroPaymentSettings, x => x.UseSandbox, storeScope);
            model.SandboxURL_OverrideForStore = await _settingService.SettingExistsAsync(dinteroPaymentSettings, x => x.SandboxURL, storeScope);
            model.ProductionURL_OverrideForStore = await _settingService.SettingExistsAsync(dinteroPaymentSettings, x => x.ProductionURL, storeScope);
            model.UseShippingAddressAsBilling_OverrideForStore = await _settingService.SettingExistsAsync(dinteroPaymentSettings, x => x.UseShippingAddressAsBilling, storeScope);
            model.TransactModeId_OverrideForStore = await _settingService.SettingExistsAsync(dinteroPaymentSettings, x => x.TransactMode, storeScope);
            model.SecretKey_OverrideForStore = await _settingService.SettingExistsAsync(dinteroPaymentSettings, x => x.SecretKey, storeScope);
            model.ClientId_OverrideForStore = await _settingService.SettingExistsAsync(dinteroPaymentSettings, x => x.ClientId, storeScope);
            model.AccountId_OverrideForStore = await _settingService.SettingExistsAsync(dinteroPaymentSettings, x => x.AccountId, storeScope);
            model.AdditionalFee_OverrideForStore = await _settingService.SettingExistsAsync(dinteroPaymentSettings, x => x.AdditionalFee, storeScope);
            model.AdditionalFeePercentage_OverrideForStore = await _settingService.SettingExistsAsync(dinteroPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
            model.LogEnabled_OverrideForStore = await _settingService.SettingExistsAsync(dinteroPaymentSettings, x => x.LogEnabled, storeScope);
            model.ProfileId_OverrideForStore = await _settingService.SettingExistsAsync(dinteroPaymentSettings, x => x.ProfileId, storeScope);
            model.DefaultAddressId_OverrideForStore = await _settingService.SettingExistsAsync(dinteroPaymentSettings, x => x.DefaultAddressId, storeScope);
            model.FallbackPhoneNo_OverrideForStore = await _settingService.SettingExistsAsync(dinteroPaymentSettings, x => x.FallbackPhoneNo, storeScope);
            model.ProductionDinteroCheckoutWebSDKEndpoint_OverrideForStore = await _settingService.SettingExistsAsync(dinteroPaymentSettings, x => x.ProductionDinteroCheckoutWebSDKEndpoint, storeScope);
            model.ProductionAuthEndpoint_OverrideForStore = await _settingService.SettingExistsAsync(dinteroPaymentSettings, x => x.ProductionAuthEndpoint, storeScope);
            model.SandboxDinteroCheckoutWebSDKEndpoint_OverrideForStore = await _settingService.SettingExistsAsync(dinteroPaymentSettings, x => x.SandboxDinteroCheckoutWebSDKEndpoint, storeScope);
            model.SandboxAuthEndpoint_OverrideForStore = await _settingService.SettingExistsAsync(dinteroPaymentSettings, x => x.SandboxAuthEndpoint, storeScope);
            model.SandboxAuthAudience_OverrideForStore = await _settingService.SettingExistsAsync(dinteroPaymentSettings, x => x.SandboxAuthAudience, storeScope);
            model.ProductionAuthAudience_OverrideForStore = await _settingService.SettingExistsAsync(dinteroPaymentSettings, x => x.ProductionAuthAudience, storeScope);
        }

        return View("~/Plugins/Payments.Dintero/Views/Configure.cshtml", model);
    }

    [HttpPost]
    [FormValueRequired("save")]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
            return AccessDeniedView();

        if (!ModelState.IsValid)
            return await Configure();

        //load settings for a chosen store scope
        var storeScope =await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var dinteroPaymentSettings = await _settingService.LoadSettingAsync<DinteroPaymentSettings>(storeScope);

        bool hasToGenerateToken = false;
        if (dinteroPaymentSettings.AccountId != model.AccountId
            || dinteroPaymentSettings.ClientId != model.ClientId
            || dinteroPaymentSettings.SecretKey != model.SecretKey)
            hasToGenerateToken = true;

        //save settings
        dinteroPaymentSettings.UseSandbox = model.UseSandbox;
        dinteroPaymentSettings.SandboxURL = model.SandboxURL;
        dinteroPaymentSettings.ProductionURL = model.ProductionURL;
        dinteroPaymentSettings.UseShippingAddressAsBilling = model.UseShippingAddressAsBilling;
        dinteroPaymentSettings.TransactMode = (TransactMode)model.TransactModeId;
        dinteroPaymentSettings.SecretKey = model.SecretKey;
        dinteroPaymentSettings.ClientId = model.ClientId;
        dinteroPaymentSettings.AccountId = model.AccountId;
        dinteroPaymentSettings.AdditionalFee = model.AdditionalFee;
        dinteroPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
        dinteroPaymentSettings.LogEnabled = model.LogEnabled;
        dinteroPaymentSettings.ProfileId = model.ProfileId;
        dinteroPaymentSettings.DefaultAddressId = model.DefaultAddressId;
        dinteroPaymentSettings.FallbackPhoneNo = model.FallbackPhoneNo;
        dinteroPaymentSettings.SandboxDinteroCheckoutWebSDKEndpoint = model.SandboxDinteroCheckoutWebSDKEndpoint;
        dinteroPaymentSettings.SandboxAuthEndpoint = model.SandboxAuthEndpoint;
        dinteroPaymentSettings.ProductionDinteroCheckoutWebSDKEndpoint = model.ProductionDinteroCheckoutWebSDKEndpoint;
        dinteroPaymentSettings.ProductionAuthEndpoint = model.ProductionAuthEndpoint;
        dinteroPaymentSettings.SandboxAuthAudience = model.SandboxAuthAudience;
        dinteroPaymentSettings.ProductionAuthAudience = model.ProductionAuthAudience;

        /* We do not clear cache after each setting update.
         * This behavior can increase performance because cached settings will not be cleared 
         * and loaded from database after each update */
        await _settingService.SaveSettingOverridablePerStoreAsync(dinteroPaymentSettings, x => x.UseSandbox, model.UseSandbox_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(dinteroPaymentSettings, x => x.SandboxURL, model.SandboxURL_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(dinteroPaymentSettings, x => x.ProductionURL, model.ProductionURL_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(dinteroPaymentSettings, x => x.UseShippingAddressAsBilling, model.UseShippingAddressAsBilling_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(dinteroPaymentSettings, x => x.TransactMode, model.TransactModeId_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(dinteroPaymentSettings, x => x.SecretKey, model.SecretKey_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(dinteroPaymentSettings, x => x.ClientId, model.ClientId_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(dinteroPaymentSettings, x => x.AccountId, model.AccountId_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(dinteroPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(dinteroPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);

        await _settingService.SaveSettingOverridablePerStoreAsync(dinteroPaymentSettings, x => x.LogEnabled, model.LogEnabled_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(dinteroPaymentSettings, x => x.ProfileId, model.ProfileId_OverrideForStore, storeScope, false);

        await _settingService.SaveSettingOverridablePerStoreAsync(dinteroPaymentSettings, x => x.DefaultAddressId, model.DefaultAddressId_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(dinteroPaymentSettings, x => x.FallbackPhoneNo, model.FallbackPhoneNo_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(dinteroPaymentSettings, x => x.ProductionDinteroCheckoutWebSDKEndpoint, model.ProductionDinteroCheckoutWebSDKEndpoint_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(dinteroPaymentSettings, x => x.ProductionAuthEndpoint, model.ProductionAuthEndpoint_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(dinteroPaymentSettings, x => x.SandboxDinteroCheckoutWebSDKEndpoint, model.SandboxDinteroCheckoutWebSDKEndpoint_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(dinteroPaymentSettings, x => x.SandboxAuthEndpoint, model.SandboxAuthEndpoint_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(dinteroPaymentSettings, x => x.SandboxAuthAudience, model.SandboxAuthAudience_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(dinteroPaymentSettings, x => x.ProductionAuthAudience, model.ProductionAuthAudience_OverrideForStore, storeScope, false);

        //now clear settings cache
        await _settingService.ClearCacheAsync();

        if (hasToGenerateToken)
            await _dinteroHttpClient.GenerateTokenAsync("auth/token");

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }

    [HttpPost, ActionName("Configure")]
    [FormValueRequired("generate-token")]
    public virtual async Task<IActionResult> GenerateClubToken()
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
            return AccessDeniedView();

        var token = await _dinteroHttpClient.GenerateTokenAsync("auth/token");
        if (!string.IsNullOrEmpty(token))
            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugin.Payments.Dintero.Token.Generated.Successfully"));
        else
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugin.Payments.Dintero.Token.Generated.failed"));

        return RedirectToAction("Configure");
    }

    #endregion
}