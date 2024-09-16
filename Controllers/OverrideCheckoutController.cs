using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Plugin.Payments.Dintero.Infrastructure;
using Nop.Services.Attributes;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Shipping;
using Nop.Services.Tax;
using Nop.Web.Controllers;
using Nop.Web.Factories;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.Dintero.Controllers;

[AutoValidateAntiforgeryToken]
public partial class OverrideCheckoutController : CheckoutController
{
    #region Ctor
    public OverrideCheckoutController(AddressSettings addressSettings,
        CaptchaSettings captchaSettings,
        CustomerSettings customerSettings,
        IAddressModelFactory addressModelFactory, 
        IAddressService addressService,
        IAttributeParser<AddressAttribute, AddressAttributeValue> addressAttributeParser,
        ICheckoutModelFactory checkoutModelFactory,
        ICountryService countryService,
        ICustomerService customerService,
        IGenericAttributeService genericAttributeService,
        ILocalizationService localizationService,
        ILogger logger,
        IOrderProcessingService orderProcessingService,
        IOrderService orderService,
        IPaymentPluginManager paymentPluginManager,
        IPaymentService paymentService,
        IProductService productService,
        IShippingService shippingService,
        IShoppingCartService shoppingCartService,
        IStoreContext storeContext,
        ITaxService taxService,
        IWebHelper webHelper,
        IWorkContext workContext,
        OrderSettings orderSettings,
        PaymentSettings paymentSettings,
        RewardPointsSettings rewardPointsSettings,
        ShippingSettings shippingSettings,
        TaxSettings taxSettings) : base(
            addressSettings, 
            captchaSettings, 
            customerSettings, 
            addressModelFactory, 
            addressService, 
            addressAttributeParser, 
            checkoutModelFactory, 
            countryService, 
            customerService, 
            genericAttributeService, 
            localizationService, 
            logger, 
            orderProcessingService, 
            orderService, 
            paymentPluginManager, 
            paymentService, 
            productService, 
            shippingService, 
            shoppingCartService, 
            storeContext, 
            taxService, 
            webHelper, 
            workContext, 
            orderSettings, 
            paymentSettings, 
            rewardPointsSettings, 
            shippingSettings, 
            taxSettings)
    {
    }
    #endregion

    #region Methods (common)

    public async Task<IActionResult> DinteroIndex()
    {
        //validation
        if (_orderSettings.CheckoutDisabled)
            return RedirectToRoute("ShoppingCart");

        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        if (!cart.Any())
            return RedirectToRoute("ShoppingCart");

        var cartProductIds = cart.Select(ci => ci.ProductId).ToArray();
        var downloadableProductsRequireRegistration =
            _customerSettings.RequireRegistrationForDownloadableProducts && await _productService.HasAnyDownloadableProductAsync(cartProductIds);

        if (await _customerService.IsGuestAsync(customer) && (!_orderSettings.AnonymousCheckoutAllowed || downloadableProductsRequireRegistration))
            return Challenge();

        //if we have only "button" payment methods available (displayed on the shopping cart page, not during checkout),
        //then we should allow standard checkout
        //all payment methods (do not filter by country here as it could be not specified yet)
        var paymentMethods = await (await _paymentPluginManager
            .LoadActivePluginsAsync(customer, store.Id))
            .WhereAwait(async pm => !await pm.HidePaymentMethodAsync(cart)).ToListAsync();
        //payment methods displayed during checkout (not with "Button" type)
        var nonButtonPaymentMethods = paymentMethods
            .Where(pm => pm.PaymentMethodType != PaymentMethodType.Button)
            .ToList();
        //"button" payment methods(*displayed on the shopping cart page)
        var buttonPaymentMethods = paymentMethods
            .Where(pm => pm.PaymentMethodType == PaymentMethodType.Button)
            .ToList();
        if (!nonButtonPaymentMethods.Any() && buttonPaymentMethods.Any())
            return RedirectToRoute("ShoppingCart");

        //reset checkout data
        await _customerService.ResetCheckoutDataAsync(customer, store.Id);

        //validation (cart)
        var checkoutAttributesXml = await _genericAttributeService.GetAttributeAsync<string>(customer,
            NopCustomerDefaults.CheckoutAttributes, store.Id);
        var scWarnings = await _shoppingCartService.GetShoppingCartWarningsAsync(cart, checkoutAttributesXml, true);
        if (scWarnings.Any())
            return RedirectToRoute("ShoppingCart");
        //validation (each shopping cart item)
        foreach (var sci in cart)
        {
            var product = await _productService.GetProductByIdAsync(sci.ProductId);

            var sciWarnings = await _shoppingCartService.GetShoppingCartItemWarningsAsync(customer,
                sci.ShoppingCartType,
                product,
                sci.StoreId,
                sci.AttributesXml,
                sci.CustomerEnteredPrice,
                sci.RentalStartDateUtc,
                sci.RentalEndDateUtc,
                sci.Quantity,
                false,
                sci.Id);
            if (sciWarnings.Any())
                return RedirectToRoute("ShoppingCart");
        }

        var paymentMethodInst = await _paymentPluginManager
                    .LoadPluginBySystemNameAsync(Defaults.SYSTEM_NAME, await _workContext.GetCurrentCustomerAsync(), (await _storeContext.GetCurrentStoreAsync()).Id);
        if (!_paymentPluginManager.IsPluginActive(paymentMethodInst))
        {
            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            return RedirectToRoute("CheckoutBillingAddress");
        }

        return RedirectToRoute("DinteroCheckout");
    }

    #endregion
}
