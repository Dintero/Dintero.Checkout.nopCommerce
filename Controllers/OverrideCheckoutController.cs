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
public class OverrideCheckoutController: BasePublicController
{
    protected readonly AddressSettings _addressSettings;
    protected readonly CaptchaSettings _captchaSettings;
    protected readonly CustomerSettings _customerSettings;
    protected readonly IAddressModelFactory _addressModelFactory;
    protected readonly IAddressService _addressService;
    protected readonly IAttributeParser<AddressAttribute, AddressAttributeValue> _addressAttributeParser;
    protected readonly ICheckoutModelFactory _checkoutModelFactory;
    protected readonly ICountryService _countryService;
    protected readonly ICustomerService _customerService;
    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly ILocalizationService _localizationService;
    protected readonly ILogger _logger;
    protected readonly IOrderProcessingService _orderProcessingService;
    protected readonly IOrderService _orderService;
    protected readonly IPaymentPluginManager _paymentPluginManager;
    protected readonly IPaymentService _paymentService;
    protected readonly IProductService _productService;
    protected readonly IShippingService _shippingService;
    protected readonly IShoppingCartService _shoppingCartService;
    protected readonly IStoreContext _storeContext;
    protected readonly ITaxService _taxService;
    protected readonly IWebHelper _webHelper;
    protected readonly IWorkContext _workContext;
    protected readonly OrderSettings _orderSettings;
    protected readonly PaymentSettings _paymentSettings;
    protected readonly RewardPointsSettings _rewardPointsSettings;
    protected readonly ShippingSettings _shippingSettings;
    protected readonly TaxSettings _taxSettings;

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
        TaxSettings taxSettings)
    {
        _addressSettings = addressSettings;
        _captchaSettings = captchaSettings;
        _customerSettings = customerSettings;
        _addressModelFactory = addressModelFactory;
        _addressService = addressService;
        _addressAttributeParser = addressAttributeParser;
        _checkoutModelFactory = checkoutModelFactory;
        _countryService = countryService;
        _customerService = customerService;
        _genericAttributeService = genericAttributeService;
        _localizationService = localizationService;
        _logger = logger;
        _orderProcessingService = orderProcessingService;
        _orderService = orderService;
        _paymentPluginManager = paymentPluginManager;
        _paymentService = paymentService;
        _productService = productService;
        _shippingService = shippingService;
        _shoppingCartService = shoppingCartService;
        _storeContext = storeContext;
        _taxService = taxService;
        _webHelper = webHelper;
        _workContext = workContext;
        _orderSettings = orderSettings;
        _paymentSettings = paymentSettings;
        _rewardPointsSettings = rewardPointsSettings;
        _shippingSettings = shippingSettings;
        _taxSettings = taxSettings;
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
