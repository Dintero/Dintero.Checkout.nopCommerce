// Note: Dintero couldn't be supported order subtotal and total discount.

using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Stores;
using Nop.Core.Domain.Tax;
using Nop.Core.Http.Extensions;
using Nop.Plugin.Payments.Dintero.Domain;
using Nop.Plugin.Payments.Dintero.Infrastructure;
using Nop.Plugin.Payments.Dintero.Models;
using Nop.Plugin.Payments.Dintero.Services;
using Nop.Services.Attributes;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Services.Vendors;
using Nop.Web.Factories;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Themes;
using Nop.Web.Models.Checkout;
using Nop.Web.Models.Common;
using Nop.Web.Models.ShoppingCart;
using Nop.Web.Validators.Common;
using PTX.Plugin.Payments.Dintero.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Nop.Plugin.Payments.Dintero.Domain.DinteroOrderSessionRequest;
using static Nop.Plugin.Payments.Dintero.Domain.DinteroPreOrderSessionRequest;

namespace Nop.Plugin.Payments.Dintero.Controllers;

public class DinteroCheckoutController : BasePluginController
{
    #region Fields

    private readonly ILogger _logger;
    private readonly IDinteroHttpClient _dinteroHttpClient;
    private readonly DinteroPaymentSettings _dinteroPaymentSettings;
    private readonly IWorkContext _workContext;
    private readonly IOrderProcessingService _orderProcessingService;
    private readonly IStoreContext _storeContext;
    private readonly IPaymentService _paymentService;
    private readonly IOrderService _orderService;
    private readonly INotificationService _notificationService;
    private readonly ILocalizationService _localizationService;
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly ICustomerService _customerService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IEncryptionService _encryptionService;
    private readonly IWorkflowMessageService _workflowMessageService;
    private readonly OrderSettings _orderSettings;
    private readonly LocalizationSettings _localizationSettings;
    private readonly IPdfService _pdfService;
    private readonly IVendorService _vendorService;
    private readonly IProductService _productService;
    private readonly ICurrencyService _currencyService;
    private readonly ICustomerActivityService _customerActivityService;
    private readonly IPriceCalculationService _priceCalculationService;
    private readonly ICountryService _countryService;
    private readonly IStateProvinceService _stateProvinceService;
    private readonly AddressSettings _addressSettings;
    private readonly CustomerSettings _customerSettings;
    private readonly ICheckoutModelFactory _checkoutModelFactory;
    private readonly IAddressService _addressService;
    private readonly ShippingSettings _shippingSettings;
    private readonly IShippingService _shippingService;
    private readonly IPaymentPluginManager _paymentPluginManager;
    private readonly IShoppingCartModelFactory _shoppingCartModelFactory;
    private readonly IWebHelper _webHelper;
    private readonly IOrderModelFactory _orderModelFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IThemeContext _themeContext;
    private readonly IAttributeParser<CheckoutAttribute, CheckoutAttributeValue> _checkoutAttributeParser;
    private readonly IAttributeService<CheckoutAttribute, CheckoutAttributeValue> _checkoutAttributeService;
    private readonly ITaxService _taxService;
    private readonly MediaSettings _mediaSettings;
    private readonly IPictureService _pictureService;
    private readonly TaxSettings _taxSettings;
    private readonly IOrderTotalCalculationService _orderTotalCalculationService;
    private readonly IOverrideOrderProcessingService _overrideOrderProcessingService;
    private readonly IStaticCacheManager _staticCacheManager;
    private readonly IStoreMappingService _storeMappingService;

    #endregion

    #region Ctor

    public DinteroCheckoutController(ILogger logger,
        IDinteroHttpClient dinteroHttpClient,
        DinteroPaymentSettings dinteroPaymentSettings,
        IWorkContext workContext,
        IOrderProcessingService orderProcessingService,
        IStoreContext storeContext,
        IPaymentService paymentService,
        IOrderService orderService,
        INotificationService notificationService,
        ILocalizationService localizationService,
        IGenericAttributeService genericAttributeService,
        ICustomerService customerService,
        IShoppingCartService shoppingCartService,
        IEncryptionService encryptionService,
        IWorkflowMessageService workflowMessageService,
        OrderSettings orderSettings,
        LocalizationSettings localizationSettings,
        IPdfService pdfService,
        IVendorService vendorService,
        IProductService productService,
        ICurrencyService currencyService,
        ICustomerActivityService customerActivityService,
        IPriceCalculationService priceCalculationService,
        ICountryService countryService,
        IStateProvinceService stateProvinceService,
        AddressSettings addressSettings,
        CustomerSettings customerSettings,
        ICheckoutModelFactory checkoutModelFactory,
        IAddressService addressService,
        ShippingSettings shippingSettings,
        IShippingService shippingService,
        IPaymentPluginManager paymentPluginManager,
        IShoppingCartModelFactory shoppingCartModelFactory,
        IWebHelper webHelper,
        IOrderModelFactory orderModelFactory,
        IHttpContextAccessor httpContextAccessor,
        IThemeContext themeContext,
        IAttributeParser<CheckoutAttribute, CheckoutAttributeValue> checkoutAttributeParser,
        IAttributeService<CheckoutAttribute, CheckoutAttributeValue> checkoutAttributeService,
        ITaxService taxService,
        MediaSettings mediaSettings,
        IPictureService pictureService,
        TaxSettings taxSettings,
        IOrderTotalCalculationService orderTotalCalculationService,
        IOverrideOrderProcessingService overrideOrderProcessingService,
        IStaticCacheManager staticCacheManager,
        IStoreMappingService storeMappingService)
    {
        _logger = logger;
        _dinteroHttpClient = dinteroHttpClient;
        _dinteroPaymentSettings = dinteroPaymentSettings;
        _workContext = workContext;
        _orderProcessingService = orderProcessingService;
        _storeContext = storeContext;
        _paymentService = paymentService;
        _orderService = orderService;
        _notificationService = notificationService;
        _localizationService = localizationService;
        _genericAttributeService = genericAttributeService;
        _customerService = customerService;
        _shoppingCartService = shoppingCartService;
        _encryptionService = encryptionService;
        _workflowMessageService = workflowMessageService;
        _orderSettings = orderSettings;
        _localizationSettings = localizationSettings;
        _pdfService = pdfService;
        _vendorService = vendorService;
        _productService = productService;
        _currencyService = currencyService;
        _customerActivityService = customerActivityService;
        _priceCalculationService = priceCalculationService;
        _countryService = countryService;
        _stateProvinceService = stateProvinceService;
        _addressSettings = addressSettings;
        _customerSettings = customerSettings;
        _checkoutModelFactory = checkoutModelFactory;
        _addressService = addressService;
        _shippingSettings = shippingSettings;
        _shippingService = shippingService;
        _paymentPluginManager = paymentPluginManager;
        _shoppingCartModelFactory = shoppingCartModelFactory;
        _webHelper = webHelper;
        _orderModelFactory = orderModelFactory;
        _httpContextAccessor = httpContextAccessor;
        _themeContext = themeContext;
        _checkoutAttributeParser = checkoutAttributeParser;
        _checkoutAttributeService = checkoutAttributeService;
        _taxService = taxService;
        _mediaSettings = mediaSettings;
        _pictureService = pictureService;
        _taxSettings = taxSettings;
        _orderTotalCalculationService = orderTotalCalculationService;
        _overrideOrderProcessingService = overrideOrderProcessingService;
        _staticCacheManager = staticCacheManager;
        _storeMappingService = storeMappingService;
    }

    #endregion

    #region Utilities

    private async Task<string> GetAddressLineAsync(Address address)
    {
        var addressLine = "";
        if (address != null)
        {
            if (!string.IsNullOrEmpty(address.Address1))
                addressLine += address.Address1;

            if (!string.IsNullOrEmpty(address.City))
                addressLine += ", " + address.City;

            if (address.StateProvinceId.HasValue)
            {
                var stateProvinceName = await _stateProvinceService.GetStateProvinceByIdAsync(address.StateProvinceId.Value);
                addressLine += ", " + stateProvinceName?.Name;
            }

            if (!string.IsNullOrEmpty(address.ZipPostalCode))
                addressLine += " " + address.ZipPostalCode;

            if (address.CountryId.HasValue)
            {
                var countryName = await _countryService.GetCountryByIdAsync(address.CountryId.Value);
                addressLine += ", " + countryName?.Name;
            }
        }

        return addressLine;
    }

    protected virtual async Task<bool> IsMinimumOrderPlacementIntervalValidAsync(Core.Domain.Customers.Customer customer)
    {
        //prevent 2 orders being placed within an X seconds time frame
        if (_orderSettings.MinimumOrderPlacementInterval == 0)
            return true;

        var lastOrder = (await _orderService.SearchOrdersAsync(storeId: (await _storeContext.GetCurrentStoreAsync()).Id,
            customerId: (await _workContext.GetCurrentCustomerAsync()).Id, pageSize: 1))
            .FirstOrDefault();
        if (lastOrder == null)
            return true;

        var interval = DateTime.UtcNow - lastOrder.CreatedOnUtc;
        return interval.TotalSeconds > _orderSettings.MinimumOrderPlacementInterval;
    }

    protected async Task<string> CreateDinteroOrderSessionRequestAsync(Core.Domain.Orders.Order order, string orderSessionresponse)
    {

        var dinteroOrderSessionRequest = JsonConvert.DeserializeObject<DinteroPreOrderSessionRequest>(orderSessionresponse);

        var dinteroOrderItemsJson = await _genericAttributeService.GetAttributeAsync<string>(order, "OrderItemsWithDiscountLine");
        var dinteroOrderItems = !string.IsNullOrWhiteSpace(dinteroOrderItemsJson) ? JsonConvert.DeserializeObject<IList<DinteroOrderItem>>(dinteroOrderItemsJson) : new List<DinteroOrderItem>();

        //prepare checkout session
        var orderItems = await _orderService.GetOrderItemsAsync(order.Id);
        var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);

        var taxRate = "";
        if (!string.IsNullOrEmpty(order.TaxRates))
            taxRate = order.TaxRates.Split(":").FirstOrDefault();

        var shippingOption = new DinteroPreOrderSessionRequest.ShippingOption
        {
            id = dinteroOrderSessionRequest.order.shipping_option.id,
            line_id = dinteroOrderSessionRequest.order.shipping_option.line_id,
            amount = Convert.ToInt32(Math.Round(order.OrderShippingInclTax * 100)),
            @operator = dinteroOrderSessionRequest.order.shipping_option.@operator,
            title = order.ShippingMethod.Split("-").First().Trim(),
            vat = !string.IsNullOrEmpty(taxRate) ? Convert.ToInt32(Math.Round(Convert.ToDecimal(taxRate))) : 0,
            vat_amount = !string.IsNullOrEmpty(taxRate) ? Convert.ToInt32(Math.Round((order.OrderShippingInclTax - order.OrderShippingExclTax) * 100)) : 0,
            delivery_method = dinteroOrderSessionRequest.order.shipping_option.delivery_method,
            pick_up_address = new ShippingOptionPickupAddress
            {
                first_name = dinteroOrderSessionRequest.order.shipping_option.pick_up_address.first_name,
                last_name = "",
                distance = Convert.ToDecimal(dinteroOrderSessionRequest.order.shipping_option.pick_up_address.distance),
                postal_code = !string.IsNullOrWhiteSpace(dinteroOrderSessionRequest.order.shipping_option.pick_up_address.postal_code) ? dinteroOrderSessionRequest.order.shipping_option.pick_up_address.postal_code : "",
                postal_place = !string.IsNullOrWhiteSpace(dinteroOrderSessionRequest.order.shipping_option.pick_up_address.postal_place) ? dinteroOrderSessionRequest.order.shipping_option.pick_up_address.postal_place : "",
                country = !string.IsNullOrWhiteSpace(dinteroOrderSessionRequest.order.shipping_option.pick_up_address.country) ? dinteroOrderSessionRequest.order.shipping_option.pick_up_address.country : "",
                address_line = dinteroOrderSessionRequest.order.shipping_option.pick_up_address.address_line,
                business_name = ""
            }
        };

        dinteroOrderSessionRequest.order = new DinteroPreOrderSessionRequest.Order
        {
            merchant_reference = order.OrderGuid.ToString(),
            currency = order.CustomerCurrencyCode,
            shipping_option = shippingOption,
            vat_amount = order.OrderTax * 100,
        };

        var payexCreditcard = new DinteroPreOrderSessionRequest.PayexCreditcard
        {
            payment_token = "",
            recurrence_token = ""
        };

        var tokens = new DinteroPreOrderSessionRequest.Tokens
        {
            PayexCreditcard = payexCreditcard
        };

        var discountLineCount = 1;

        //order total (and applied discounts, gift cards, reward points)
        var orderTotal = order.OrderTotal;
        dinteroOrderSessionRequest.order.amount = orderTotal * 100;

        var lineCount = 1;
        var isIncludingTax = await _workContext.GetTaxDisplayTypeAsync() == TaxDisplayType.IncludingTax && !_taxSettings.ForceTaxExclusionFromOrderSubtotal;
        foreach (var item in orderItems)
        {
            var discountLineForProduct = new List<DinteroPreOrderSessionRequest.Discountlines>();

            var discountOrderItem = dinteroOrderItems.Where(x => x.ItemId == item.Id).Select(x => x).FirstOrDefault();
            if (discountOrderItem != null)
            {
                for (int i = 0; i < discountOrderItem.discount_lines.Count; i++)
                {
                    var discountLineItem = discountOrderItem.discount_lines[i];

                    var discountLine = new DinteroPreOrderSessionRequest.Discountlines
                    {
                        amount = discountLineItem.amount,
                        percentage = discountLineItem.percentage,
                        description = !string.IsNullOrWhiteSpace(discountLineItem.description) ? discountLineItem.description : "",
                        discount_type = discountLineItem.discount_type,
                        discount_id = discountLineItem.discount_id,
                        line_id = discountLineCount
                    };
                    discountLineForProduct.Add(discountLine);

                    discountLineCount++;
                }
            }

            var product = await _productService.GetProductByIdAsync(item.ProductId);
				var productPicture = await _pictureService.GetProductPictureAsync(product, string.Empty);
				dinteroOrderSessionRequest.order.items.Add(new DinteroPreOrderSessionRequest.Item()
            {
                id = discountOrderItem.id,
                line_id = lineCount.ToString(),
                description = !string.IsNullOrWhiteSpace(discountOrderItem.description) ? discountOrderItem.description : "",
                quantity = discountOrderItem.quantity,
                amount = isIncludingTax ? Math.Round(discountOrderItem.amount) : Math.Round(discountOrderItem.amount + discountOrderItem.vat_amount),
                vat = Math.Round(discountOrderItem.vat),
                vat_amount = Math.Round(discountOrderItem.vat_amount),
					thumbnail_url = productPicture != null ? await _pictureService.GetPictureUrlAsync(productPicture.Id, _mediaSettings.CartThumbPictureSize, true) : string.Empty,
					discount_lines = discountLineForProduct
            });

            lineCount++;
        }

        // Checkout attributes value pass in dintero session
        var checkoutAttributes = _checkoutAttributeParser.ParseAttributeValues(order.CheckoutAttributesXml);
        if (checkoutAttributes != null)
        {
            await foreach (var item in checkoutAttributes)
            {
                var checkoutAttributeValue = await item.values.FirstOrDefaultAsync();
                if (checkoutAttributeValue != null && checkoutAttributeValue.PriceAdjustment > 0)
                {
                    var (caExclTax, attributeTaxRate) = await _taxService.GetCheckoutAttributePriceAsync(item.attribute, checkoutAttributeValue, false, customer);
                    dinteroOrderSessionRequest.order.items.Add(new DinteroPreOrderSessionRequest.Item()
                    {
                        id = item.attribute.Id.ToString(),
                        line_id = lineCount.ToString(),
                        description = item.attribute.Name,
                        quantity = 1,
                        amount = Math.Round(checkoutAttributeValue.PriceAdjustment * 100),
                        vat = attributeTaxRate,
                        thumbnail_url = string.Empty,
                        vat_amount = Math.Round((checkoutAttributeValue.PriceAdjustment - caExclTax) * 100),
                        discount_lines = new List<DinteroPreOrderSessionRequest.Discountlines>()
                    });
                    lineCount++;
                }
            }
        }

        if (order.PaymentMethodAdditionalFeeInclTax > 0)
        {
            //payment total
            var paymentMethodAdditionaltaxRate = (await _taxService.GetPaymentMethodAdditionalFeeAsync(order.PaymentMethodAdditionalFeeInclTax, true, customer)).taxRate;

            dinteroOrderSessionRequest.order.items.Add(new DinteroPreOrderSessionRequest.Item()
            {
                id = await _localizationService.GetResourceAsync("Plugin.Payments.Dintero.PaymentAdditionalFee"),
                line_id = lineCount.ToString(),
                description = await _localizationService.GetResourceAsync("Plugin.Payments.Dintero.PaymentAdditionalFee"),
                quantity = 1,
                amount = Math.Round(order.PaymentMethodAdditionalFeeInclTax * 100),
                vat = paymentMethodAdditionaltaxRate,
                vat_amount = paymentMethodAdditionaltaxRate > 0 ? Convert.ToInt32(Math.Round((order.PaymentMethodAdditionalFeeInclTax - order.PaymentMethodAdditionalFeeExclTax) * 100)) : 0,
                thumbnail_url = string.Empty,
                discount_lines = new List<DinteroPreOrderSessionRequest.Discountlines>()
            });
            lineCount++;
        }

        var currentCustomerEmailAddress = "";
        // billing address
        var billingAddress = await _addressService.GetAddressByIdAsync(order.BillingAddressId);
        if (billingAddress != null)
        {
            //clone billing address
            var address = _addressService.CloneAddress(billingAddress);
            if (address != null)
            {
                string phone = string.Empty;
                if (!string.IsNullOrEmpty(address.PhoneNumber))
                {
                    if (!address.PhoneNumber.StartsWith("+47"))
                        phone = "+47" + address.PhoneNumber;
                }
                var dinteroBillingAddress = new DinteroPreOrderSessionRequest.OrderAddress
                {
                    first_name = !string.IsNullOrEmpty(address.FirstName) ? address.FirstName.ToString() : "",
                    last_name = !string.IsNullOrEmpty(address.LastName) ? address.LastName.ToString() : "",
                    email = !string.IsNullOrEmpty(address.Email) ? address.Email.ToString() : "",
                    country = (await _countryService.GetCountryByIdAsync(address.CountryId.HasValue ? address.CountryId.Value : 0))?.TwoLetterIsoCode ?? "",
                    address_line = !string.IsNullOrEmpty(address.Address1) ? address.Address1.ToString() : "",
                    address_line_2 = !string.IsNullOrEmpty(address.Address2) ? address.Address2.ToString() : "",
                    co_address = "",
                    business_name = "",
                    postal_code = !string.IsNullOrEmpty(address.ZipPostalCode) ? address.ZipPostalCode.ToString() : "",
                    postal_place = !string.IsNullOrEmpty(address.City) ? address.City.ToString() : "",
                    phone_number = !string.IsNullOrEmpty(phone) ? phone.Replace(" ", "").ToString() : "",
                    comment = "",
                    cost_center = "",
                    customer_reference = "",
                    organization_number = ""
                };
                dinteroOrderSessionRequest.order.billing_address = dinteroBillingAddress;

                if (await _customerService.IsGuestAsync(await _workContext.GetCurrentCustomerAsync()))
                    currentCustomerEmailAddress = !string.IsNullOrEmpty(address.Email) ? address.Email.ToString() : "";

            }
        }

        // customer
        var customerRequest = new DinteroPreOrderSessionRequest.Customer
        {
            customer_id = customer.Id.ToString(),
            email = !string.IsNullOrEmpty(customer.Email) ? customer.Email.ToString() : currentCustomerEmailAddress,
            phone_number = customer.Phone ?? "",
            tokens = tokens
        };
        dinteroOrderSessionRequest.customer = customerRequest;

        // shipping address
        if (order.ShippingAddressId.HasValue)
        {
            var shippingAddress = await _addressService.GetAddressByIdAsync(order.ShippingAddressId.Value);
            if (shippingAddress != null)
            {
                //clone shipping address
                var address = _addressService.CloneAddress(shippingAddress);
                if (address != null)
                {
                    string phone = string.Empty;
                    if (!string.IsNullOrEmpty(address.PhoneNumber))
                    {
                        if (!address.PhoneNumber.StartsWith("+47"))
                            phone = "+47" + address.PhoneNumber;
                    }
                    var dinteroShippingAddress = new DinteroPreOrderSessionRequest.OrderAddress
                    {
                        first_name = !string.IsNullOrEmpty(address.FirstName) ? address.FirstName.ToString() : "",
                        last_name = !string.IsNullOrEmpty(address.LastName) ? address.LastName.ToString() : "",
                        email = !string.IsNullOrEmpty(address.Email) ? address.Email.ToString() : "",
                        country = (await _countryService.GetCountryByIdAsync(address.CountryId.HasValue ? address.CountryId.Value : 0))?.TwoLetterIsoCode ?? "",
                        address_line = !string.IsNullOrEmpty(address.Address1) ? address.Address1.ToString() : "",
                        address_line_2 = !string.IsNullOrEmpty(address.Address2) ? address.Address2.ToString() : "",
                        co_address = "",
                        business_name = "",
                        postal_code = !string.IsNullOrEmpty(address.ZipPostalCode) ? address.ZipPostalCode.ToString() : "",
                        postal_place = !string.IsNullOrEmpty(address.City) ? address.City.ToString() : "",
                        phone_number = !string.IsNullOrEmpty(phone) ? phone.Replace(" ", "").ToString() : "",
                        comment = "",
                        cost_center = "",
                        customer_reference = "",
                        organization_number = ""
                    };
                    dinteroOrderSessionRequest.order.shipping_address = dinteroShippingAddress;
                }
            }
        }
        foreach (var option in dinteroOrderSessionRequest.express.shipping_options)
        {
            if (option.id == dinteroOrderSessionRequest.order.shipping_option.id && option.line_id == dinteroOrderSessionRequest.order.shipping_option.line_id)
            {
                option.amount = dinteroOrderSessionRequest.order.shipping_option.amount;
                option.@operator = dinteroOrderSessionRequest.order.shipping_option.@operator;
                option.title = dinteroOrderSessionRequest.order.shipping_option.title;
                option.vat = dinteroOrderSessionRequest.order.shipping_option.vat;
                option.vat_amount = dinteroOrderSessionRequest.order.shipping_option.vat_amount;
                option.delivery_method = dinteroOrderSessionRequest.order.shipping_option.delivery_method;
                option.pick_up_address = new ShippingOptionPickupAddress
                {
                    first_name = dinteroOrderSessionRequest.order.shipping_option.pick_up_address.first_name,
                    last_name = "",
                    distance = Convert.ToDecimal(dinteroOrderSessionRequest.order.shipping_option.pick_up_address.distance),
                    postal_code = dinteroOrderSessionRequest.order.shipping_option.pick_up_address.postal_code,
                    postal_place = !string.IsNullOrWhiteSpace(dinteroOrderSessionRequest.order.shipping_option.pick_up_address.postal_place) ? dinteroOrderSessionRequest.order.shipping_option.pick_up_address.postal_place : "",
                    country = !string.IsNullOrWhiteSpace(dinteroOrderSessionRequest.order.shipping_option.pick_up_address.country) ? dinteroOrderSessionRequest.order.shipping_option.pick_up_address.country : "",
                    address_line = dinteroOrderSessionRequest.order.shipping_option.pick_up_address.address_line,
                    business_name = ""
                };
            }
            else
            {
                option.amount = option.amount;
                option.@operator = option.@operator;
                option.title = option.title;
                option.vat = option.vat;
                option.vat_amount = option.vat_amount;
                option.delivery_method = option.delivery_method;
                option.pick_up_address = new ShippingOptionPickupAddress
                {
                    first_name = !string.IsNullOrWhiteSpace(option.pick_up_address.first_name) ? option.pick_up_address.first_name : "",
                    last_name = "",
                    distance = option.pick_up_address.distance,
                    postal_code = option.pick_up_address.postal_code,
                    postal_place = !string.IsNullOrWhiteSpace(option.pick_up_address.postal_place) ? option.pick_up_address.postal_place : "",
                    country = !string.IsNullOrWhiteSpace(option.pick_up_address.country) ? option.pick_up_address.country : "",
                    address_line = option.pick_up_address.address_line,
                    business_name = ""
                };
            }
        }
        var storeLocation = _webHelper.GetStoreLocation();
        dinteroOrderSessionRequest.url.return_url = string.Format(PluginDefaults.DINTERO_RETURN_URL, storeLocation, customer.CustomerGuid, dinteroOrderSessionRequest.order.merchant_reference);
        dinteroOrderSessionRequest.url.callback_url = string.Format(PluginDefaults.DINTERO_CALLBACK_URL, storeLocation, customer.CustomerGuid, dinteroOrderSessionRequest.order.merchant_reference);
        dinteroOrderSessionRequest.express.shipping_mode = "shipping_required";

        var orderSessionContent = JsonConvert.SerializeObject(dinteroOrderSessionRequest);
        if (_dinteroPaymentSettings.LogEnabled)
           await _logger.InsertLogAsync(LogLevel.Information, "Dintero request body for checkout sessions",
                JsonConvert.SerializeObject(dinteroOrderSessionRequest) + " Token : " + _dinteroPaymentSettings.AccessToken);
        dinteroOrderSessionRequest.express.customer_types = new List<string>() { "b2c" };
        return orderSessionContent;

    }

    #endregion

    #region Methods

    public async Task<IActionResult> DinteroCheckout()
    {
        //get auth token if not exist or expired
        if (string.IsNullOrEmpty(_dinteroPaymentSettings.AccessToken)
            || _dinteroPaymentSettings.TokenExpiresInDatetimeUTC == null
            || _dinteroPaymentSettings.TokenExpiresInDatetimeUTC < DateTime.UtcNow)
            await _dinteroHttpClient.GenerateTokenAsync("auth/token");

        var customer = await _workContext.GetCurrentCustomerAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, (await _storeContext.GetCurrentStoreAsync()).Id);

        if (cart.Count <= 0)
            return RedirectToRoute("ShoppingCart");

        var model = await _shoppingCartModelFactory.PrepareShoppingCartModelAsync(new ShoppingCartModel(), cart,
            isEditable: false,
            prepareAndDisplayOrderReviewData: false);

        var dinteroSessionId = await _genericAttributeService.GetAttributeAsync<string>(customer, PluginDefaults.DinteroOrderSessionId, (await _storeContext.GetCurrentStoreAsync()).Id);

        DinteroPreOrderSessionRequest dinteroPreOrderSessionRequest = null;
        if (!string.IsNullOrWhiteSpace(dinteroSessionId))
        {
            var geneatedDate = await _genericAttributeService.GetAttributeAsync<DateTime?>(customer, PluginDefaults.DinteroOrderSessionIdGeneratedTime, (await _storeContext.GetCurrentStoreAsync()).Id);
            if (geneatedDate.HasValue && (DateTime.UtcNow - geneatedDate.Value).TotalMinutes <= PluginDefaults.DinteroOrderSessionExiredTime)
            {
                var orderSessionresponseObj = await _dinteroHttpClient.GetAsync(endpoint: "sessions/" + dinteroSessionId);
                if (!string.IsNullOrWhiteSpace(orderSessionresponseObj))
                {
                    dinteroPreOrderSessionRequest = JsonConvert.DeserializeObject<DinteroPreOrderSessionRequest>(orderSessionresponseObj);
                    dinteroSessionId = !string.IsNullOrEmpty(dinteroPreOrderSessionRequest.transaction_id) ? string.Empty : dinteroSessionId;
                    dinteroPreOrderSessionRequest = !string.IsNullOrEmpty(dinteroPreOrderSessionRequest.transaction_id) ? null : dinteroPreOrderSessionRequest;
                }
            }
            else
                dinteroSessionId = string.Empty;
        }
        //prepare checkout session

        var storeLocation = _webHelper.GetStoreLocation();
        
        var orderRequest = new DinteroPreOrderSessionRequest.Order
        {
            merchant_reference = Guid.NewGuid().ToString(),
            currency = "NOK",
            //     shipping_option = shippingOption,
        };

        var url = new DinteroPreOrderSessionRequest.Url
        {
            return_url = string.Format(PluginDefaults.DINTERO_RETURN_URL, storeLocation, customer.CustomerGuid, orderRequest.merchant_reference),
            callback_url = string.Format(PluginDefaults.DINTERO_CALLBACK_URL, storeLocation, customer.CustomerGuid, orderRequest.merchant_reference)
        };

        var creditcard = new DinteroPreOrderSessionRequest.Creditcard
        {
            enabled = true
        };
        var payex = new DinteroPreOrderSessionRequest.Payex
        {
            creditcard = creditcard,
        };
        var vipps = new DinteroPreOrderSessionRequest.Vipps
        {
            enabled = true,
        };
        var invoice = new DinteroPreOrderSessionRequest.Invoice
        {
            enabled = true,
            type = "payment_product_type"
        };
        var collector = new DinteroPreOrderSessionRequest.Collector
        {
            type = "payment_type",
            invoice = invoice,
        };
        var configuration = new DinteroPreOrderSessionRequest.Configuration
        {
            auto_capture = Convert.ToInt32(_dinteroPaymentSettings.TransactMode) == (int)TransactMode.AuthorizeAndCapture,
            default_payment_type = "payex.creditcard",
            payex = payex,
            vipps = vipps,
            collector = collector,
        };

        var dinteroOrderSessionRequest = new DinteroPreOrderSessionRequest
        {
            configuration = configuration,
            order = dinteroPreOrderSessionRequest == null ? orderRequest : dinteroPreOrderSessionRequest.order,
            url = dinteroPreOrderSessionRequest == null ? url : dinteroPreOrderSessionRequest.url,
            profile_id = _dinteroPaymentSettings.ProfileId,
        };

        var payexCreditcard = new DinteroPreOrderSessionRequest.PayexCreditcard
        {
            payment_token = "",
            recurrence_token = ""
        };

        var tokens = new DinteroPreOrderSessionRequest.Tokens
        {
            PayexCreditcard = payexCreditcard
        };

        // customer
        var customerRequest = new DinteroPreOrderSessionRequest.Customer
        {
            customer_id = customer.Id.ToString(),
            email = !string.IsNullOrEmpty(customer.Email) ? customer.Email.ToString() : "",
            phone_number = customer.Phone ?? "",
            tokens = tokens
        };
        dinteroOrderSessionRequest.customer = customerRequest;

        var shippingAddress = await _customerService.GetCustomerShippingAddressAsync(customer);
        if (shippingAddress == null)
            shippingAddress = (await _customerService.GetAddressesByCustomerIdAsync(customer.Id)).FirstOrDefault();
        if (shippingAddress == null)
            shippingAddress = await _addressService.GetAddressByIdAsync(_dinteroPaymentSettings.DefaultAddressId);

        if (shippingAddress != null)
        {
            var shippingMethodModel = await _checkoutModelFactory.PrepareShippingMethodModelAsync(cart, shippingAddress);

            if (shippingMethodModel.ShippingMethods.Count > 0)
            {
                var shippingOption = shippingMethodModel.ShippingMethods.FirstOrDefault().ShippingOption;

                //save
                await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, shippingOption, (await _storeContext.GetCurrentStoreAsync()).Id);
            }

            foreach (var item in shippingMethodModel.ShippingMethods)
            {
                var pickupPointsResponse = await _shippingService.GetPickupPointsAsync(cart, shippingAddress,
                        await _workContext.GetCurrentCustomerAsync(), storeId: (await _storeContext.GetCurrentStoreAsync()).Id, providerSystemName: item.ShippingRateComputationMethodSystemName);
                var pickupPoints = new PickUpAddress();
                if (pickupPointsResponse.Success)
                {
                    foreach (var point in pickupPointsResponse.PickupPoints)
                    {
                        var country = await _countryService.GetCountryByTwoLetterIsoCodeAsync(point.CountryCode);
                        var state = await _stateProvinceService.GetStateProvinceByAbbreviationAsync(point.StateAbbreviation, country?.Id);

                        var pickupPointModel = new Nop.Plugin.Payments.Dintero.Domain.DinteroPreOrderSessionRequest.PickupPoint()
                        {
                            id = int.Parse(point.Id),
                            name = point.Name,
                            address = point.Address,
                            city = point.City,
                            countryCode = country != null ? await _localizationService.GetLocalizedAsync(country, x => x.Name, (await _workContext.GetWorkingLanguageAsync()).Id) : string.Empty,
                            postalCode = point.ZipPostalCode,
                            latitude = point.Latitude.HasValue ? point.Latitude.Value : decimal.Zero,
                            longitude = point.Longitude.HasValue ? point.Latitude.Value : decimal.Zero,
                        };
                        pickupPoints.PickupPoint.Add(pickupPointModel);
                    }
                }

                // Calculate shipping tax
                var (shippingAmountInclTax,_) = await _taxService.GetShippingPriceAsync(item.Rate, true, customer);
                var (shippingAmountExclTax, taxRateInclTax) = await _taxService.GetShippingPriceAsync(item.Rate, false, customer);
                var shippingVatAmount = Math.Round((shippingAmountInclTax - shippingAmountExclTax) * 100);
                if (pickupPoints.PickupPoint.Count > 0 && item.ShippingOption.IsPickupInStore)
                {
                    var pickupLineId = 1;
                    foreach (var picupPoint in pickupPoints.PickupPoint)
                    {
                        dinteroOrderSessionRequest.express.shipping_options.Add(new DinteroPreOrderSessionRequest.ShippingOption
                        {
                            id = item.ShippingRateComputationMethodSystemName,
                            line_id = item.ShippingRateComputationMethodSystemName + "_" + picupPoint.id.ToString(),
                            vat = Convert.ToInt32(taxRateInclTax),
                            vat_amount = Convert.ToInt32(shippingVatAmount),
                            amount = Convert.ToInt32(item.Rate * 100),
                            title = item.Name,
                            @operator = "Posten",
                            delivery_method = "pick_up",
                            pick_up_address = new ShippingOptionPickupAddress
                            {
                                first_name = picupPoint.name,
                                last_name = "",
                                distance = Convert.ToDecimal(picupPoint.distanceInKm.Replace('.', ',')),
                                postal_code = picupPoint.postalCode,
                                postal_place = !string.IsNullOrWhiteSpace(picupPoint.city) ? picupPoint.city : "",
                                country = !string.IsNullOrWhiteSpace(picupPoint.countryCode) ? picupPoint.countryCode : "",
                                address_line = picupPoint.address + ", " + picupPoint.postalCode + " " + picupPoint.city,
                                business_name = ""
                            }
                        });
                        pickupLineId++;
                    }
                }
                else if (item.ShippingRateComputationMethodSystemName == "Shipping.ClickAndCollect")
                {

                    var warehouses = await _shippingService.GetAllWarehousesAsync();

                    var nearLocatedList = new List<DinteroPreOrderSessionRequest.ShippingOption>();
                    var farLocatedList = new List<DinteroPreOrderSessionRequest.ShippingOption>();

                    var pickupLineId = 1;
                    foreach (var warehouse in warehouses)
                    {
                        //Get address
                        var warehouseAddress = await _addressService.GetAddressByIdAsync(warehouse.AddressId);
                        string addressLine = await GetAddressLineAsync(warehouseAddress);

                        var thisShippingOption = new DinteroPreOrderSessionRequest.ShippingOption
                        {
                            id = item.ShippingRateComputationMethodSystemName,
                            line_id = item.ShippingRateComputationMethodSystemName + "_" + warehouse.Id.ToString(),
                            vat = Convert.ToInt32(taxRateInclTax),
                            vat_amount = Convert.ToInt32(shippingVatAmount),
                            amount = Convert.ToInt32(item.Rate * 100),
                            title = item.Name + " - " +  await _localizationService.GetResourceAsync("Plugins.Payments.Dintero.ClickAndCollect.EstimatedShipment"),
                            @operator = item.Name,
                            delivery_method = "pick_up",
                            pick_up_address = new ShippingOptionPickupAddress
                            {
                                first_name = warehouse.Name,
                                last_name = "",
                                distance = decimal.Zero,
                                postal_code = warehouseAddress.ZipPostalCode,
                                postal_place = !string.IsNullOrWhiteSpace(warehouseAddress.City) ? warehouseAddress.City : "",
                                country = "",
                                address_line = addressLine,
                                business_name = ""
                            }
                        };

                        if (shippingAddress.ZipPostalCode == warehouseAddress.ZipPostalCode)
                        {
                            nearLocatedList.Add(thisShippingOption);
                        }
                        else
                        {
                            farLocatedList.Add(thisShippingOption);
                        }

                        pickupLineId++;
                    }
                    if (nearLocatedList.Count() > 0)
                    {
                        var sortedNearest = nearLocatedList.OrderBy(x => x.pick_up_address.distance);

                        foreach (var near in sortedNearest)
                        {
                            dinteroOrderSessionRequest.express.shipping_options.Add(near);
                        }
                    }
                    foreach (var far in farLocatedList)
                    {
                        dinteroOrderSessionRequest.express.shipping_options.Add(far);
                    }
                }
                else if (pickupPoints.DropPoints.Count > 0 && item.ShippingOption.IsPickupInStore && item.ShippingRateComputationMethodSystemName.Contains("Shipping.ShipAdvisor"))
                {
                    var pickupLineId = 1;
                    foreach (var picupPoint in pickupPoints.DropPoints)
                    {
                        dinteroOrderSessionRequest.express.shipping_options.Add(new DinteroPreOrderSessionRequest.ShippingOption
                        {
                            id = item.ShippingRateComputationMethodSystemName,
                            line_id = item.ShippingRateComputationMethodSystemName + "_" + picupPoint.OriginalID.ToString(),
                            vat = 0,
                            vat_amount = 0,
                            amount = Convert.ToInt32(item.Rate * 100),
                            title = item.Name,
                            @operator = "Posten",
                            delivery_method = "pick_up",
                            pick_up_address = new ShippingOptionPickupAddress
                            {
                                first_name = picupPoint.Name1,
                                last_name = "",
                                distance = Convert.ToDecimal(picupPoint.Distance.ToString().Replace('.', ',')),
                                postal_code = picupPoint.PostCode,
                                postal_place = !string.IsNullOrWhiteSpace(picupPoint.City) ? picupPoint.City : "",
                                country = !string.IsNullOrWhiteSpace(picupPoint.CountryCode) ? picupPoint.CountryCode : "",
                                address_line = picupPoint.Street1 + ", " + picupPoint.Street2 + ", " + picupPoint.PostCode + " " + picupPoint.City,
                                business_name = ""
                            }
                        });
                        pickupLineId++;
                    }
                }
                else
                {

                    dinteroOrderSessionRequest.express.shipping_options.Add(new DinteroPreOrderSessionRequest.ShippingOption
                    {
                        id = item.ShippingRateComputationMethodSystemName,
                        line_id = item.Name,
                        vat = Convert.ToInt32(taxRateInclTax),
                        vat_amount = Convert.ToInt32(shippingVatAmount),
                        amount = Convert.ToInt32(item.Rate * 100),
                        title = item.Name,
                        @operator = "Posten",
                        delivery_method = "delivery",
                        pick_up_address = new ShippingOptionPickupAddress
                        {
                            first_name = "",
                            last_name = "",
                            distance = decimal.Zero,
                            postal_code = "",
                            postal_place = "",
                            country = "",
                            address_line = "",
                            business_name = ""
                        }
                    });
                }
            }

            dinteroOrderSessionRequest.express.shipping_mode = "shipping_required";
            dinteroOrderSessionRequest.express.customer_types = new List<string>() { "b2c" };
            string phone = string.Empty;
            if (!string.IsNullOrEmpty(shippingAddress.PhoneNumber) && shippingAddress.Id != _dinteroPaymentSettings.DefaultAddressId)
            {
                if (!shippingAddress.PhoneNumber.StartsWith("+47"))
                    phone = "+47" + shippingAddress.PhoneNumber;
            }

            var dinteroBillingAddress = new DinteroPreOrderSessionRequest.OrderAddress
            {
                first_name = !string.IsNullOrEmpty(shippingAddress.FirstName) && shippingAddress.Id != _dinteroPaymentSettings.DefaultAddressId ? shippingAddress.FirstName.ToString() : "",
                last_name = !string.IsNullOrEmpty(shippingAddress.FirstName) && shippingAddress.Id != _dinteroPaymentSettings.DefaultAddressId ? shippingAddress.LastName.ToString() : "",
                email = !string.IsNullOrEmpty(shippingAddress.Email) && shippingAddress.Id != _dinteroPaymentSettings.DefaultAddressId ? shippingAddress.Email.ToString() : "",
                country = "",
                address_line = !string.IsNullOrEmpty(shippingAddress.Address1) && shippingAddress.Id != _dinteroPaymentSettings.DefaultAddressId ? shippingAddress.Address1.ToString() : "",
                address_line_2 = !string.IsNullOrEmpty(shippingAddress.Address2) && shippingAddress.Id != _dinteroPaymentSettings.DefaultAddressId ? shippingAddress.Address2.ToString() : "",
                co_address = "",
                business_name = "",
                postal_code = !string.IsNullOrEmpty(shippingAddress.ZipPostalCode) && shippingAddress.Id != _dinteroPaymentSettings.DefaultAddressId ? shippingAddress.ZipPostalCode.ToString() : "",
                postal_place = !string.IsNullOrEmpty(shippingAddress.City) && shippingAddress.Id != _dinteroPaymentSettings.DefaultAddressId ? shippingAddress.City.ToString() : "",
                phone_number = !string.IsNullOrEmpty(phone) && shippingAddress.Id != _dinteroPaymentSettings.DefaultAddressId ? phone.Replace(" ", "").ToString() : _dinteroPaymentSettings.FallbackPhoneNo,
                comment = "",
                cost_center = "",
                customer_reference = "",
                organization_number = ""
            };
            dinteroOrderSessionRequest.order.billing_address = dinteroBillingAddress;
            dinteroOrderSessionRequest.order.shipping_address = dinteroBillingAddress;

            dinteroOrderSessionRequest.order.items.Clear();
            var cartTotal = await _shoppingCartModelFactory.PrepareOrderTotalsModelAsync(cart, false);

            var lineCount = 1;
            var isIncludingTax = await _workContext.GetTaxDisplayTypeAsync() == TaxDisplayType.IncludingTax && !_taxSettings.ForceTaxExclusionFromOrderSubtotal;
            foreach (var discountOrderItem in model.Items)
            {
                //Add club discount in shopping cart itme
                int discountLineCount = 1;
                var discountLineForProduct = new List<DinteroPreOrderSessionRequest.Discountlines>();

                var product = await _productService.GetProductByIdAsync(discountOrderItem.ProductId);
                var sci = cart.Where(x => x.Id == discountOrderItem.Id).FirstOrDefault();
                var vat = decimal.Zero;
                var vatAmount = decimal.Zero;
                var unitPrice = product.Price;
                
                if (sci != null)
                {
                    // Get shopping cart item discounts
                    var (scSubTotal, discountAmount, scDiscounts,_) = await _shoppingCartService.GetSubTotalAsync(sci, true);

                    var (discountAmountInclTax,_) =
                await _taxService.GetProductPriceAsync(product, discountAmount, true, customer);

                    var (scSubTotalInclTax, taxRateInclTax) =
                await _taxService.GetProductPriceAsync(product, scSubTotal, true, customer);
                    var (scSubTotalExclTax,_) =
                        await _taxService.GetProductPriceAsync(product, scSubTotal, false, customer);

                    vat = taxRateInclTax;
                    vatAmount = Math.Round((scSubTotalInclTax - scSubTotalExclTax) * 100);
                    if (scDiscounts.Any())
                    {
                        var discountDescription = string.Join(",", scDiscounts.Select(d => d.Name).ToList());
                        var discountIds = string.Join(",", scDiscounts.Select(d => d.Id).ToList());
                        var discountLine = new DinteroPreOrderSessionRequest.Discountlines
                        {
                            amount = Math.Round(discountAmountInclTax * 100),
                            percentage = decimal.Zero,
                            description = discountDescription,
                            discount_type = "customer",
                            discount_id = discountIds,
                            line_id = discountLineCount,
                        };
                        discountLineForProduct.Add(discountLine);
                        discountLineCount++;
                    }

                    var productPicture = await _shoppingCartModelFactory.PrepareCartItemPictureModelAsync(sci, _mediaSettings.CartThumbPictureSize, true, product.Name);

						dinteroOrderSessionRequest.order.items.Add(new DinteroPreOrderSessionRequest.Item()
                    {
                        id = discountOrderItem.Sku,
                        line_id = lineCount.ToString(),
                        description = !string.IsNullOrWhiteSpace(product.Name) ? product.Name : "",
                        quantity = discountOrderItem.Quantity,
                        amount = isIncludingTax ? Math.Round(scSubTotal * 100) : Math.Round(scSubTotal * 100) + vatAmount,
                        vat = vat,
                        vat_amount = vatAmount,
							thumbnail_url = productPicture != null && !string.IsNullOrWhiteSpace(productPicture.ImageUrl) ? productPicture.ImageUrl : string.Empty,
                        discount_lines = discountLineForProduct
                    });
                    lineCount++;
                }

            }

            // Checkout attributes value pass in dintero session
            var checkoutAttributesXml = await _genericAttributeService.GetAttributeAsync<string>(customer,
            NopCustomerDefaults.CheckoutAttributes, (await _storeContext.GetCurrentStoreAsync()).Id);
            var checkoutAttributes = _checkoutAttributeParser.ParseAttributeValues(checkoutAttributesXml);
            if (checkoutAttributes != null)
            {
                await foreach (var item in checkoutAttributes)
                {
                    var checkoutAttributeValue = await item.values.FirstOrDefaultAsync();
                    if (checkoutAttributeValue != null && checkoutAttributeValue.PriceAdjustment > 0)
                    {
                        var (caExclTax, attributeTaxRate) = await _taxService.GetCheckoutAttributePriceAsync(item.attribute, checkoutAttributeValue, false, customer);
                        dinteroOrderSessionRequest.order.items.Add(new DinteroPreOrderSessionRequest.Item()
                        {
                            id = item.attribute.Id.ToString(),
                            line_id = lineCount.ToString(),
                            description = item.attribute.Name,
                            quantity = 1,
                            amount = Math.Round(checkoutAttributeValue.PriceAdjustment * 100),
                            vat = attributeTaxRate,
                            vat_amount = Math.Round((checkoutAttributeValue.PriceAdjustment - caExclTax) * 100),
								thumbnail_url = string.Empty,
								discount_lines = new List<DinteroPreOrderSessionRequest.Discountlines>()
                        });
                        lineCount++;
                    }
                }
            }

            dinteroOrderSessionRequest.order.shipping_option = dinteroOrderSessionRequest.express.shipping_options.FirstOrDefault();
            var paymentAdditionalFee = decimal.Zero;
            if (_dinteroPaymentSettings.AdditionalFee > 0)
            {
                paymentAdditionalFee = await _orderTotalCalculationService.CalculatePaymentAdditionalFeeAsync(cart,
                       _dinteroPaymentSettings.AdditionalFee, _dinteroPaymentSettings.AdditionalFeePercentage);


                dinteroOrderSessionRequest.order.items.Add(new DinteroPreOrderSessionRequest.Item()
                {
                    id = await _localizationService.GetResourceAsync("Plugin.Payments.Dintero.PaymentAdditionalFee"),
                    line_id = lineCount.ToString(),
                    description = await _localizationService.GetResourceAsync("Plugin.Payments.Dintero.PaymentAdditionalFee"),
                    quantity = 1,
                    amount = Math.Round(paymentAdditionalFee * 100),
                    vat = 0,
                    vat_amount = 0,
                    thumbnail_url = string.Empty,
                    discount_lines = new List<DinteroPreOrderSessionRequest.Discountlines>()
                });
                lineCount++;
            }


            var subTotalIncludingTax = await _workContext.GetTaxDisplayTypeAsync() == TaxDisplayType.IncludingTax && !_taxSettings.ForceTaxExclusionFromOrderSubtotal;
            var (orderSubTotalDiscountAmountBaseInclTax, _, subTotalWithoutDiscountBaseInclTax, _, _) = await _orderTotalCalculationService.GetShoppingCartSubTotalAsync(cart, true);
            var subtotalBase = subTotalWithoutDiscountBaseInclTax;
            var currentCurrency = await _workContext.GetWorkingCurrencyAsync();
            var subtotal = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(subtotalBase, currentCurrency);

            dinteroOrderSessionRequest.order.amount = (subtotal * 100) + Math.Round(paymentAdditionalFee * 100) + (dinteroOrderSessionRequest.order.shipping_option != null ? dinteroOrderSessionRequest.order.shipping_option.amount : decimal.Zero);

            dinteroPreOrderSessionRequest = dinteroOrderSessionRequest;

            var orderSessionContent = JsonConvert.SerializeObject(dinteroPreOrderSessionRequest);

            if (_dinteroPaymentSettings.LogEnabled)
                await _logger.InsertLogAsync(LogLevel.Information, "DinteroCheckout > Dintero request body for checkout sessions",
                    JsonConvert.SerializeObject(orderSessionContent) + " Token : " + _dinteroPaymentSettings.AccessToken);
            //save
            await _genericAttributeService.SaveAttributeAsync(await _workContext.GetCurrentCustomerAsync(),
                NopCustomerDefaults.SelectedPaymentMethodAttribute, Defaults.SYSTEM_NAME, (await _storeContext.GetCurrentStoreAsync()).Id);

            var orderSessionresponse = !string.IsNullOrWhiteSpace(dinteroSessionId) ? await _dinteroHttpClient.PutAsync(endpoint: "sessions/" + dinteroSessionId + "?update_without_lock=true", content: orderSessionContent, accessToken: _dinteroPaymentSettings.AccessToken)
            : await _dinteroHttpClient.PostAsync(endpoint: "sessions-profile", content: orderSessionContent, accessToken: _dinteroPaymentSettings.AccessToken);
            if (orderSessionresponse.IsSuccessStatusCode)
            {
                if (!string.IsNullOrWhiteSpace(dinteroSessionId))
                    ViewBag.DinteroOrderSessionResponseId = dinteroSessionId;
                else
                {
                    var dinteroOrderSessionResponse = JsonConvert.DeserializeObject<DinteroOrderSessionResponse>(await orderSessionresponse.Content.ReadAsStringAsync());
                    if (dinteroOrderSessionResponse != null && !string.IsNullOrEmpty(dinteroOrderSessionResponse.id))
                    {
                        ViewBag.DinteroOrderSessionResponseId = dinteroOrderSessionResponse.id;
                        await _genericAttributeService.SaveAttributeAsync<string>(customer, PluginDefaults.DinteroOrderSessionId, dinteroOrderSessionResponse.id, (await _storeContext.GetCurrentStoreAsync()).Id);
                        DateTime? generatedDateTime = DateTime.UtcNow;
                        await _genericAttributeService.SaveAttributeAsync(customer, PluginDefaults.DinteroOrderSessionIdGeneratedTime, generatedDateTime, (await _storeContext.GetCurrentStoreAsync()).Id);
                    }
                }
            }
            else
            {
                await _genericAttributeService.SaveAttributeAsync<string>(customer, PluginDefaults.DinteroOrderSessionId, null, (await _storeContext.GetCurrentStoreAsync()).Id);
                await _genericAttributeService.SaveAttributeAsync<DateTime?>(customer, PluginDefaults.DinteroOrderSessionIdGeneratedTime, null, (await _storeContext.GetCurrentStoreAsync()).Id);
                _notificationService.ErrorNotification("Something went wrong, Please try again.");
                await _logger.InsertLogAsync(LogLevel.Error, "DinteroCheckout > Dintero response body for checkout sessions", await orderSessionresponse.Content.ReadAsStringAsync());
            }
        }
        else
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Payments.Dintero.AddressNotFound"));

        return View("/Plugins/Payments.Dintero/Themes/" + await _themeContext.GetWorkingThemeNameAsync() + "/Views/DinteroCheckout/DinteroCheckout.cshtml", model);
    }

    [IgnoreAntiforgeryToken]
    public virtual async Task<IActionResult> SaveAddressesShippingAddress(string dinteroSessionId)
    {
        try
        {
            var orderSession = await _dinteroHttpClient.GetAsync(endpoint: "sessions/" + dinteroSessionId);
            if (!string.IsNullOrWhiteSpace(orderSession))
            {
                var dinteroSessionRequest = JsonConvert.DeserializeObject<DinteroPreOrderSessionRequest>(orderSession);

                //validation
                if (_orderSettings.CheckoutDisabled)
                    throw new Exception(await _localizationService.GetResourceAsync("Checkout.Disabled"));

                var cart = await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), ShoppingCartType.ShoppingCart,(await _storeContext.GetCurrentStoreAsync()).Id);
                if (!cart.Any())
                    throw new Exception("Your cart is empty");

                if (await _customerService.IsGuestAsync(await _workContext.GetCurrentCustomerAsync()) && !_orderSettings.AnonymousCheckoutAllowed)
                    throw new Exception("Anonymous checkout is not allowed");

                //new address
                var newAddress = new AddressModel
                {
                    FirstName = dinteroSessionRequest.order.shipping_address.first_name,
                    LastName = dinteroSessionRequest.order.shipping_address.last_name,
                    Email = dinteroSessionRequest.order.shipping_address.email,
                    CountryId = (await _countryService.GetCountryByTwoLetterIsoCodeAsync(dinteroSessionRequest.order.shipping_address.country)).Id,
                    City = dinteroSessionRequest.order.shipping_address.postal_place,
                    Address1 = dinteroSessionRequest.order.shipping_address.address_line,
                    Address2 = dinteroSessionRequest.order.shipping_address.address_line_2,
                    ZipPostalCode = dinteroSessionRequest.order.shipping_address.postal_code,
                    PhoneNumber = dinteroSessionRequest.order.shipping_address.phone_number.StartsWith("+47") ? dinteroSessionRequest.order.shipping_address.phone_number.Substring(3) : dinteroSessionRequest.order.shipping_address.phone_number,
                };
                newAddress.StateProvinceId = (await _stateProvinceService.GetStateProvincesByCountryIdAsync(newAddress.CountryId.Value)).Select(s => s.Id).FirstOrDefault();
                //Set default country to 1st country i.e Norway
                if ((newAddress.CountryId ?? 0) == 0)
                    newAddress.CountryId = (await _countryService.GetAllCountriesAsync()).FirstOrDefault()?.Id;

                var validator = new AddressValidator(_localizationService, _stateProvinceService, _addressSettings,
                    _customerSettings);

                var validationResult = validator.Validate(newAddress);

                //validate model
                if (!validationResult.IsValid)
                {
                    return Json(new { error = validationResult.Errors.Count(), message = validationResult.Errors.Select(e => e.ErrorMessage).ToList() });
                }

                //try to find an address with the same values (don't duplicate records)
                var address = _addressService.FindAddress((await _customerService.GetAddressesByCustomerIdAsync((await _workContext.GetCurrentCustomerAsync()).Id)).ToList(),
                    newAddress.FirstName, newAddress.LastName, newAddress.PhoneNumber,
                    newAddress.Email, newAddress.FaxNumber, newAddress.Company,
                    newAddress.Address1, newAddress.Address2, newAddress.City,
                    newAddress.County, newAddress.StateProvinceId, newAddress.ZipPostalCode,
                    newAddress.CountryId, "");

                if (address == null)
                {
                    //address is not found. let's create a new one
                    address = newAddress.ToEntity();
                    address.CustomAttributes = string.Empty;

                    await _addressService.InsertAddressAsync(address);

                    await _customerService.InsertCustomerAddressAsync(await _workContext.GetCurrentCustomerAsync(), address);
                }

                // Set shipping and billing address id in customer and update customer.
                (await _workContext.GetCurrentCustomerAsync()).BillingAddressId = address.Id;
                (await _workContext.GetCurrentCustomerAsync()).ShippingAddressId = address.Id;
                await _customerService.UpdateCustomerAsync(await _workContext.GetCurrentCustomerAsync());

                if (await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart))
                {
                    //shipping is required
                    var billingAddress = await _customerService.GetCustomerBillingAddressAsync(await _workContext.GetCurrentCustomerAsync());

                    //by default Shipping is available if the country is not specified
                    var shippingAllowed = !_addressSettings.CountryEnabled || ((await _countryService.GetCountryByAddressAsync(billingAddress))?.AllowsShipping ?? false);
                    if (_shippingSettings.ShipToSameAddress && shippingAllowed)
                    {
                        //ship to the same address
                        (await _workContext.GetCurrentCustomerAsync()).ShippingAddressId = billingAddress.Id;
                        await _customerService.UpdateCustomerAsync(await _workContext.GetCurrentCustomerAsync());
                        //reset selected shipping method (in case if "pick up in store" was selected)
                        await _genericAttributeService.SaveAttributeAsync<Core.Domain.Shipping.ShippingOption>(await _workContext.GetCurrentCustomerAsync(), NopCustomerDefaults.SelectedShippingOptionAttribute, null, (await _storeContext.GetCurrentStoreAsync()).Id);
                        await _genericAttributeService.SaveAttributeAsync<Core.Domain.Shipping.PickupPoint>(await _workContext.GetCurrentCustomerAsync(), NopCustomerDefaults.SelectedPickupPointAttribute, null, (await _storeContext.GetCurrentStoreAsync()).Id);
                        //limitation - "Ship to the same address" doesn't properly work in "pick up in store only" case (when no shipping plugins are available) 
                    }

                }

                // Prepare shipping method model
                var shippingMethodModel = await _checkoutModelFactory.PrepareShippingMethodModelAsync(cart,await _customerService.GetCustomerShippingAddressAsync(await _workContext.GetCurrentCustomerAsync()));
                dinteroSessionRequest.express.shipping_options = new List<Nop.Plugin.Payments.Dintero.Domain.DinteroPreOrderSessionRequest.ShippingOption>();
                foreach (var item in shippingMethodModel.ShippingMethods)
                {
                    var pickupPointsResponse = await _shippingService.GetPickupPointsAsync(cart, address,
                        await _workContext.GetCurrentCustomerAsync(), storeId: (await _storeContext.GetCurrentStoreAsync()).Id, providerSystemName: item.ShippingRateComputationMethodSystemName);
                    var pickupPoints = new PickUpAddress();
                    if (pickupPointsResponse.Success)
                    {
                        foreach (var point in pickupPointsResponse.PickupPoints)
                        {
                            var country = await _countryService.GetCountryByTwoLetterIsoCodeAsync(point.CountryCode);
                            var state = await _stateProvinceService.GetStateProvinceByAbbreviationAsync(point.StateAbbreviation, country?.Id);

                            var pickupPointModel = new Nop.Plugin.Payments.Dintero.Domain.DinteroPreOrderSessionRequest.PickupPoint()
                            {
                                id = int.Parse(point.Id),
                                name = point.Name,
                                address = point.Address,
                                city = point.City,
                                countryCode = country != null ? await _localizationService.GetLocalizedAsync(country, x => x.Name, (await _workContext.GetWorkingLanguageAsync()).Id) : string.Empty,
                                postalCode = point.ZipPostalCode,
                                latitude = point.Latitude.HasValue ? point.Latitude.Value : decimal.Zero,
                                longitude = point.Longitude.HasValue ? point.Latitude.Value : decimal.Zero,
                            };
                            pickupPoints.PickupPoint.Add(pickupPointModel);
                        }
                    }
                    if (pickupPoints.PickupPoint.Count > 0 && item.ShippingOption.IsPickupInStore)
                    {
                        var pickupLineId = 1;
                        foreach (var picupPoint in pickupPoints.PickupPoint)
                        {
                            dinteroSessionRequest.express.shipping_options.Add(new DinteroPreOrderSessionRequest.ShippingOption
                            {
                                id = item.ShippingRateComputationMethodSystemName,
                                line_id = item.ShippingRateComputationMethodSystemName + "_" + picupPoint.id.ToString(),
                                vat = 0,
                                vat_amount = 0,
                                amount = Convert.ToInt32(item.Rate * 100),
                                title = item.Name,
                                @operator = "Posten",
                                delivery_method = "pick_up",
                                pick_up_address = new ShippingOptionPickupAddress
                                {
                                    first_name = picupPoint.name,
                                    last_name = "",
                                    distance = Convert.ToDecimal(picupPoint.distanceInKm.Replace('.', ',')),
                                    postal_code = picupPoint.postalCode,
                                    postal_place = !string.IsNullOrWhiteSpace(picupPoint.city) ? picupPoint.city : "",
                                    country = !string.IsNullOrWhiteSpace(picupPoint.countryCode) ? picupPoint.countryCode : "",
                                    address_line = picupPoint.address + ", " + picupPoint.postalCode + " " + picupPoint.city,
                                    business_name = ""
                                }
                            });
                            pickupLineId++;
                        }
                    }
                    else if (item.ShippingRateComputationMethodSystemName == "Shipping.ClickAndCollect")
                    {
                        // Set warehouses in shipping_options if shipping method is Shipping.ClickAndCollect
                        var warehouses = await _shippingService.GetAllWarehousesAsync();

                        var nearLocatedList = new List<DinteroPreOrderSessionRequest.ShippingOption>();
                        var farLocatedList = new List<DinteroPreOrderSessionRequest.ShippingOption>();

                        var pickupLineId = 1;
                        foreach (var warehouse in warehouses)
                        {
                            //Get address
                            var warehouseAddress = await _addressService.GetAddressByIdAsync(warehouse.AddressId);
                            string addressLine = await GetAddressLineAsync(warehouseAddress);

                            var thisShippingOption = new DinteroPreOrderSessionRequest.ShippingOption
                            {
                                id = item.ShippingRateComputationMethodSystemName,
                                line_id = item.ShippingRateComputationMethodSystemName + "_" + warehouse.Id.ToString(),
                                vat = 0,
                                vat_amount = 0,
                                amount = Convert.ToInt32(item.Rate * 100),
                                title = item.Name + " - " + await _localizationService.GetResourceAsync("Plugins.Payments.Dintero.ClickAndCollect.EstimatedShipment"),
                                @operator = item.Name,
                                delivery_method = "pick_up",
                                pick_up_address = new ShippingOptionPickupAddress
                                {
                                    first_name = warehouse.Name,
                                    last_name = "",
                                    distance = decimal.Zero,
                                    postal_code = warehouseAddress.ZipPostalCode,
                                    postal_place = !string.IsNullOrWhiteSpace(warehouseAddress.City) ? warehouseAddress.City : "",
                                    country = "",
                                    address_line = addressLine,
                                    business_name = ""
                                }
                            };

                            if (address.ZipPostalCode == warehouseAddress.ZipPostalCode)
                            {
                                nearLocatedList.Add(thisShippingOption);
                            }
                            else
                            {
                                farLocatedList.Add(thisShippingOption);
                            }

                            pickupLineId++;
                        }
                        if (nearLocatedList.Count() > 0)
                        {
                            var sortedNearest = nearLocatedList.OrderBy(x => x.pick_up_address.distance);

                            foreach (var near in sortedNearest)
                            {
                                dinteroSessionRequest.express.shipping_options.Add(near);
                            }
                        }
                        foreach (var far in farLocatedList)
                        {
                            dinteroSessionRequest.express.shipping_options.Add(far);
                        }
                    }
                    else if (pickupPoints.DropPoints.Count > 0 && item.ShippingOption.IsPickupInStore && item.ShippingRateComputationMethodSystemName.Contains("Shipping.ShipAdvisor"))
                    {
                        var pickupLineId = 1;
                        foreach (var picupPoint in pickupPoints.DropPoints)
                        {
                            dinteroSessionRequest.express.shipping_options.Add(new DinteroPreOrderSessionRequest.ShippingOption
                            {
                                id = item.ShippingRateComputationMethodSystemName,
                                line_id = item.ShippingRateComputationMethodSystemName + "_" + picupPoint.OriginalID.ToString(),
                                vat = 0,
                                vat_amount = 0,
                                amount = Convert.ToInt32(item.Rate * 100),
                                title = item.Name,
                                @operator = "Posten",
                                delivery_method = "pick_up",
                                pick_up_address = new ShippingOptionPickupAddress
                                {
                                    first_name = picupPoint.Name1,
                                    last_name = "",
                                    distance = Convert.ToDecimal(picupPoint.Distance.ToString().Replace('.', ',')),
                                    postal_code = picupPoint.PostCode,
                                    postal_place = !string.IsNullOrWhiteSpace(picupPoint.City) ? picupPoint.City : "",
                                    country = !string.IsNullOrWhiteSpace(picupPoint.CountryCode) ? picupPoint.CountryCode : "",
                                    address_line = picupPoint.Street1 + ", " + picupPoint.Street2 + ", " + picupPoint.PostCode + " " + picupPoint.City,
                                    business_name = ""
                                }
                            });
                            pickupLineId++;
                        }
                    }
                    else
                    {
                        // Set shipping option with delivery_method metho "delivery" if pickup is not allowed.
                        dinteroSessionRequest.express.shipping_options.Add(new DinteroPreOrderSessionRequest.ShippingOption
                        {
                            id = item.ShippingRateComputationMethodSystemName,
                            line_id = item.Name,
                            vat = 0,
                            vat_amount = 0,
                            amount = Convert.ToInt32(item.Rate * 100),
                            title = item.Name,
                            @operator = "Posten",
                            delivery_method = "delivery",
                            pick_up_address = new ShippingOptionPickupAddress
                            {
                                first_name = "",
                                last_name = "",
                                distance = decimal.Zero,
                                postal_code = "",
                                postal_place = "",
                                country = "",
                                address_line = "",
                                business_name = ""
                            }
                        });
                    }
                }

                dinteroSessionRequest.order.billing_address.address_line_2 = !string.IsNullOrEmpty(dinteroSessionRequest.order.billing_address.address_line_2) ? dinteroSessionRequest.order.billing_address.address_line_2 : "";
                dinteroSessionRequest.order.billing_address.business_name = !string.IsNullOrEmpty(dinteroSessionRequest.order.billing_address.business_name) ? dinteroSessionRequest.order.billing_address.business_name : "";
                dinteroSessionRequest.order.billing_address.co_address = !string.IsNullOrEmpty(dinteroSessionRequest.order.billing_address.co_address) ? dinteroSessionRequest.order.billing_address.co_address : "";
                dinteroSessionRequest.order.billing_address.comment = !string.IsNullOrEmpty(dinteroSessionRequest.order.billing_address.comment) ? dinteroSessionRequest.order.billing_address.comment : "";
                dinteroSessionRequest.order.billing_address.organization_number = !string.IsNullOrEmpty(dinteroSessionRequest.order.billing_address.organization_number) ? dinteroSessionRequest.order.billing_address.organization_number : "";
                dinteroSessionRequest.order.billing_address.customer_reference = !string.IsNullOrEmpty(dinteroSessionRequest.order.billing_address.customer_reference) ? dinteroSessionRequest.order.billing_address.customer_reference : "";
                dinteroSessionRequest.order.billing_address.cost_center = !string.IsNullOrEmpty(dinteroSessionRequest.order.billing_address.cost_center) ? dinteroSessionRequest.order.billing_address.cost_center : "";

                dinteroSessionRequest.order.billing_address.first_name = !string.IsNullOrEmpty(dinteroSessionRequest.order.billing_address.first_name) ? dinteroSessionRequest.order.billing_address.first_name : "";
                dinteroSessionRequest.order.billing_address.last_name = !string.IsNullOrEmpty(dinteroSessionRequest.order.billing_address.last_name) ? dinteroSessionRequest.order.billing_address.last_name : "";

                dinteroSessionRequest.order.shipping_address = dinteroSessionRequest.order.billing_address;

                // Save shipping method
                //validation
                if (_orderSettings.CheckoutDisabled)
                    throw new Exception(await _localizationService.GetResourceAsync("Checkout.Disabled"));

                if (!await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart))
                    throw new Exception("Shipping is not required");

                //pickup point
                if (_shippingSettings.AllowPickupInStore && _orderSettings.DisplayPickupInStoreOnShippingMethodPage)
                {
                    if (dinteroSessionRequest.order.shipping_option.delivery_method == "pick_up")
                    {
                        var pickupOption = new Core.Domain.Shipping.PickupPoint
                        {
                            Id = dinteroSessionRequest.order.shipping_option.line_id.Split("_").Last(),
                            Name = dinteroSessionRequest.order.shipping_option.title,
                            ProviderSystemName = dinteroSessionRequest.order.shipping_option.id,
                            Address = dinteroSessionRequest.order.shipping_option.pick_up_address.address_line,
                            City = dinteroSessionRequest.order.shipping_option.pick_up_address.postal_place,
                            CountryCode = dinteroSessionRequest.order.shipping_option.pick_up_address.country,
                            ZipPostalCode = dinteroSessionRequest.order.shipping_option.pick_up_address.postal_code,
                            //FirstName = dinteroSessionRequest.order.shipping_option.pick_up_address.first_name,
                            //LastName = dinteroSessionRequest.order.shipping_option.pick_up_address.last_name,
                        };
                        var pickUpInStoreShippingOption = new Core.Domain.Shipping.ShippingOption
                        {
                            Name = string.Format(await _localizationService.GetResourceAsync("Checkout.PickupPoints.Name"), pickupOption.Name),
                            Rate = pickupOption.PickupFee,
                            Description = !string.IsNullOrWhiteSpace(pickupOption.Description) ? pickupOption.Description : "",
                            ShippingRateComputationMethodSystemName = pickupOption.ProviderSystemName,
                            IsPickupInStore = true
                        };
                        await _genericAttributeService.SaveAttributeAsync(await _workContext.GetCurrentCustomerAsync(), NopCustomerDefaults.SelectedShippingOptionAttribute, pickUpInStoreShippingOption, (await _storeContext.GetCurrentStoreAsync()).Id);
                        await _genericAttributeService.SaveAttributeAsync(await _workContext.GetCurrentCustomerAsync(), NopCustomerDefaults.SelectedPickupPointAttribute, pickupOption, (await _storeContext.GetCurrentStoreAsync()).Id);

                    }
                    else
                        // set value indicating that "pick up in store" option has not been chosen
                        await _genericAttributeService.SaveAttributeAsync<Core.Domain.Shipping.PickupPoint>(await _workContext.GetCurrentCustomerAsync(), NopCustomerDefaults.SelectedPickupPointAttribute, null, (await _storeContext.GetCurrentStoreAsync()).Id);
                }
                var selectedSippingOption = dinteroSessionRequest.express.shipping_options.Where(s => s.id == dinteroSessionRequest.order.shipping_option.id && s.line_id == dinteroSessionRequest.order.shipping_option.line_id).FirstOrDefault();
                dinteroSessionRequest.order.shipping_option = selectedSippingOption != null ? selectedSippingOption : dinteroSessionRequest.express.shipping_options.FirstOrDefault();
                //dinteroSessionRequest.order.shipping_option = dinteroSessionRequest.express.shipping_options.FirstOrDefault();

                //parse selected method 
                if (string.IsNullOrEmpty(dinteroSessionRequest.order.shipping_option.title))
                    throw new Exception("Selected shipping method can't be parsed");

                var selectedName = dinteroSessionRequest.order.shipping_option.title;
                var shippingRateComputationMethodSystemName = dinteroSessionRequest.order.shipping_option.id;

                //find it
                //performance optimization. try cache first
                var shippingOptions = await _genericAttributeService.GetAttributeAsync<List<Core.Domain.Shipping.ShippingOption>>(await _workContext.GetCurrentCustomerAsync(),
                    NopCustomerDefaults.OfferedShippingOptionsAttribute, (await _storeContext.GetCurrentStoreAsync()).Id);
                if (shippingOptions == null || !shippingOptions.Any())
                {
                    //not found? let's load them using shipping service
                    shippingOptions = (await _shippingService.GetShippingOptionsAsync(cart, await _customerService.GetCustomerShippingAddressAsync(await _workContext.GetCurrentCustomerAsync()),
                        await _workContext.GetCurrentCustomerAsync(), shippingRateComputationMethodSystemName, (await _storeContext.GetCurrentStoreAsync()).Id)).ShippingOptions.ToList();
                }
                else
                {
                    //loaded cached results. let's filter result by a chosen shipping rate computation method
                    shippingOptions = shippingOptions.Where(so => so.ShippingRateComputationMethodSystemName.Equals(shippingRateComputationMethodSystemName, StringComparison.InvariantCultureIgnoreCase))
                        .ToList();
                }

                //var shippingOption = shippingOptions
                //    .Find(so => !string.IsNullOrEmpty(so.Name) && so.Name.Equals(selectedName, StringComparison.InvariantCultureIgnoreCase));
                var shippingOption = shippingOptions
                    .Find(so => !string.IsNullOrEmpty(so.Name) && selectedName.StartsWith(so.Name, StringComparison.InvariantCultureIgnoreCase));
                if (shippingOption == null)
                    throw new Exception("Selected shipping method can't be loaded");

                //save
                await _genericAttributeService.SaveAttributeAsync(await _workContext.GetCurrentCustomerAsync(), NopCustomerDefaults.SelectedShippingOptionAttribute, shippingOption, (await _storeContext.GetCurrentStoreAsync()).Id);

                dinteroSessionRequest.order.items.Select(c => { c.description = string.IsNullOrWhiteSpace(c.description) ? "" : c.description; return c; }).ToList();

                var cartTotal = await _shoppingCartModelFactory.PrepareOrderTotalsModelAsync(cart, false);

                var paymentAdditionalFee = decimal.Zero;
                if (_dinteroPaymentSettings.AdditionalFee > 0)
                {
                    paymentAdditionalFee = await _orderTotalCalculationService.CalculatePaymentAdditionalFeeAsync(cart,
                           _dinteroPaymentSettings.AdditionalFee, _dinteroPaymentSettings.AdditionalFeePercentage);
                    var additionalFeeItemName = await _localizationService.GetResourceAsync("Plugin.Payments.Dintero.PaymentAdditionalFee");
                    dinteroSessionRequest.order.items.Where(c => c.id == additionalFeeItemName).Select(c => { c.amount = Math.Round(paymentAdditionalFee * 100); return c; }).ToList();                      
                }

                var (orderSubTotalDiscountAmountBaseInclTax, _, subTotalWithoutDiscountBaseInclTax, _, _) = await _orderTotalCalculationService.GetShoppingCartSubTotalAsync(cart, true);
                var subtotalBase = subTotalWithoutDiscountBaseInclTax;
                var currentCurrency = await _workContext.GetWorkingCurrencyAsync();
                var subtotal = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(subtotalBase, currentCurrency);

                dinteroSessionRequest.order.amount = (subtotal * 100) + Math.Round(paymentAdditionalFee * 100) + dinteroSessionRequest.order.shipping_option.amount;
                
                var orderSessionContent = JsonConvert.SerializeObject(dinteroSessionRequest);

                if (_dinteroPaymentSettings.LogEnabled)
                    await _logger.InsertLogAsync(LogLevel.Information, "SaveAddressesShippingAddress > Dintero request body for update checkout sessions",
                        JsonConvert.SerializeObject(orderSessionContent) + " Token : " + _dinteroPaymentSettings.AccessToken);

                var orderSessionresponse = await _dinteroHttpClient.PutAsync(endpoint: "sessions/" + dinteroSessionRequest.id + "?update_without_lock=true", content: orderSessionContent, accessToken: _dinteroPaymentSettings.AccessToken);
                if (orderSessionresponse.IsSuccessStatusCode)
                {
                    var updatedCart = await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), ShoppingCartType.ShoppingCart, (await _storeContext.GetCurrentStoreAsync()).Id);
                    _httpContextAccessor.HttpContext.Items.Remove("nop.TaxTotal");
                    var model = await _shoppingCartModelFactory.PrepareShoppingCartModelAsync(new ShoppingCartModel(), updatedCart,
                        isEditable: false,
                        prepareAndDisplayOrderReviewData: false);

                    return Json(new
                    {
                        Success = true,
                        orderSummary = await RenderPartialViewToStringAsync("/Plugins/" + Defaults.SYSTEM_NAME + "/Themes/" + await _themeContext.GetWorkingThemeNameAsync() + "/Views/DinteroCheckout/_OrderSummary.cshtml", model),
                    });
                }
                else
                    await _logger.InsertLogAsync(LogLevel.Error, "SaveAddressesShippingAddress > Dintero response body for update checkout sessions", await orderSessionresponse.Content.ReadAsStringAsync());
            }

            return Json(new { error = 1, message = "Something went wrong, Please try again." });
        }
        catch (Exception exc)
        {
            await _logger.WarningAsync(exc.Message, exc, await _workContext.GetCurrentCustomerAsync());
            return Json(new { error = 1, message = exc.Message });
        }
    }

    [IgnoreAntiforgeryToken]
    public virtual async Task<IActionResult> SavePaymentMethod(string dinteroSessionId)
    {
        try
        {
            var orderSessionresponse = await _dinteroHttpClient.GetAsync(endpoint: "sessions/" + dinteroSessionId);
            if (!string.IsNullOrWhiteSpace(orderSessionresponse))
            {
                var dinteroPreOrderSessionRequest = JsonConvert.DeserializeObject<DinteroPreOrderSessionRequest>(orderSessionresponse);

                //validation
                if (_orderSettings.CheckoutDisabled)
                    throw new Exception(await _localizationService.GetResourceAsync("Checkout.Disabled"));

                var cart = await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), ShoppingCartType.ShoppingCart, (await _storeContext.GetCurrentStoreAsync()).Id);

                if (!cart.Any())
                    throw new Exception("Your cart is empty");

                if (await _customerService.IsGuestAsync(await _workContext.GetCurrentCustomerAsync()) && !_orderSettings.AnonymousCheckoutAllowed)
                    throw new Exception("Anonymous checkout is not allowed");

                var paymentMethodInst = await _paymentPluginManager
                    .LoadPluginBySystemNameAsync(Defaults.SYSTEM_NAME, await _workContext.GetCurrentCustomerAsync(), (await _storeContext.GetCurrentStoreAsync()).Id);
                if (!_paymentPluginManager.IsPluginActive(paymentMethodInst))
                    throw new Exception("Selected payment method can't be parsed");

                //save
                await _genericAttributeService.SaveAttributeAsync(await _workContext.GetCurrentCustomerAsync(),
                    NopCustomerDefaults.SelectedPaymentMethodAttribute, Defaults.SYSTEM_NAME, (await _storeContext.GetCurrentStoreAsync()).Id);

                //get payment info
                var processPaymentRequest = new ProcessPaymentRequest();

                //set previous order GUID (if exists)
                await _paymentService.GenerateOrderGuidAsync(processPaymentRequest);

                //prevent 2 orders being placed within an X seconds time frame
                if (!await IsMinimumOrderPlacementIntervalValidAsync(await _workContext.GetCurrentCustomerAsync()))
                    throw new Exception(await _localizationService.GetResourceAsync("Checkout.MinOrderPlacementInterval"));
                var paymentMethodAdditionalFeeInclTax = await _orderTotalCalculationService.CalculatePaymentAdditionalFeeAsync(cart,
                    _dinteroPaymentSettings.AdditionalFee, _dinteroPaymentSettings.AdditionalFeePercentage);

                //place order
                processPaymentRequest.StoreId = (await _storeContext.GetCurrentStoreAsync()).Id;
                processPaymentRequest.CustomerId = (await _workContext.GetCurrentCustomerAsync()).Id;
                processPaymentRequest.PaymentMethodSystemName = await _genericAttributeService.GetAttributeAsync<string>(await _workContext.GetCurrentCustomerAsync(),
                    NopCustomerDefaults.SelectedPaymentMethodAttribute, (await _storeContext.GetCurrentStoreAsync()).Id);
                processPaymentRequest.CustomValues.Add(PluginDefaults.DinteroSessionId, dinteroPreOrderSessionRequest.id);
                processPaymentRequest.InitialOrder = new Core.Domain.Orders.Order();
                processPaymentRequest.InitialOrder.PaymentMethodAdditionalFeeInclTax = paymentMethodAdditionalFeeInclTax;
                await HttpContext.Session.SetAsync<ProcessPaymentRequest>("OrderPaymentInfo", processPaymentRequest);

                if (dinteroPreOrderSessionRequest.order.shipping_option.title.ToLower().Contains("click"))
                {
                    //Save attribute
                    await _genericAttributeService.SaveAttributeAsync(await _workContext.GetCurrentCustomerAsync(), "warehouseId", dinteroPreOrderSessionRequest.order.shipping_option.line_id.Split("_").Last(), (await _storeContext.GetCurrentStoreAsync()).Id);
                }

                //Store pickup point in checkout attribute.
                //Get all existing attributes
                //var checkoutAttributes = await _checkoutAttributeService.GetAllCheckoutAttributesAsync((await _storeContext.GetCurrentStoreAsync()).Id, false);
                var checkoutAttributes = await _checkoutAttributeService.GetAllAttributesAsync(_staticCacheManager, _storeMappingService, (await _storeContext.GetCurrentStoreAsync()).Id);

                //Get current attribute values
                var checkoutAttributesXml =
                   await _genericAttributeService.GetAttributeAsync<string>(await _workContext.GetCurrentCustomerAsync(), NopCustomerDefaults.CheckoutAttributes,
                        (await _storeContext.GetCurrentStoreAsync()).Id);

                foreach (var attribute in checkoutAttributes)
                {
                    if (attribute.Name == "PickupPointId")
                    {
                        //Remove and potentially re-add
                        checkoutAttributesXml = _checkoutAttributeParser.RemoveAttribute(checkoutAttributesXml, attribute.Id);
                        if ((dinteroPreOrderSessionRequest.order.shipping_option.line_id.Contains("BringApi") || dinteroPreOrderSessionRequest.order.shipping_option.line_id.Contains("ShipAdvisor")) && !string.IsNullOrEmpty(dinteroPreOrderSessionRequest.order.shipping_option.line_id.Split("_").LastOrDefault()))
                        {
                            checkoutAttributesXml = _checkoutAttributeParser.AddAttribute(checkoutAttributesXml, attribute, dinteroPreOrderSessionRequest.order.shipping_option.line_id.Split("_").LastOrDefault());

                            var pickupOption = new Core.Domain.Shipping.PickupPoint
                            {
                                Id = dinteroPreOrderSessionRequest.order.shipping_option.line_id.Split("_").Last(),
                                Name = dinteroPreOrderSessionRequest.order.shipping_option.title,
                                ProviderSystemName = dinteroPreOrderSessionRequest.order.shipping_option.id,
                                Address = dinteroPreOrderSessionRequest.order.shipping_option.pick_up_address.address_line,
                                City = dinteroPreOrderSessionRequest.order.shipping_option.pick_up_address.postal_place,
                                CountryCode = dinteroPreOrderSessionRequest.order.shipping_option.pick_up_address.country,
                                ZipPostalCode = dinteroPreOrderSessionRequest.order.shipping_option.pick_up_address.postal_code,
                                //FirstName = dinteroPreOrderSessionRequest.order.shipping_option.pick_up_address.first_name,
                                //LastName = dinteroPreOrderSessionRequest.order.shipping_option.pick_up_address.last_name,
                            };

                            await _genericAttributeService.SaveAttributeAsync(await _workContext.GetCurrentCustomerAsync(), NopCustomerDefaults.SelectedPickupPointAttribute, pickupOption, (await _storeContext.GetCurrentStoreAsync()).Id);
                        }
                    }
                    else if (attribute.Name == "PickupPointName")
                    {
                        //Remove and potentially re-add
                        checkoutAttributesXml = _checkoutAttributeParser.RemoveAttribute(checkoutAttributesXml, attribute.Id);
                        if ((dinteroPreOrderSessionRequest.order.shipping_option.line_id.Contains("BringApi") || dinteroPreOrderSessionRequest.order.shipping_option.line_id.Contains("ShipAdvisor")) && !string.IsNullOrEmpty(dinteroPreOrderSessionRequest.order.shipping_option.pick_up_address.first_name))
                        {
                            checkoutAttributesXml = _checkoutAttributeParser.AddAttribute(checkoutAttributesXml, attribute, dinteroPreOrderSessionRequest.order.shipping_option.pick_up_address.first_name);
                        }
                    }
                }

                //Save potential changes
                await _genericAttributeService.SaveAttributeAsync(await _workContext.GetCurrentCustomerAsync(), NopCustomerDefaults.CheckoutAttributes, checkoutAttributesXml, (await _storeContext.GetCurrentStoreAsync()).Id);
                
                var placeOrderResult = await _overrideOrderProcessingService.PlaceOrderAsync(processPaymentRequest);
                if (placeOrderResult.Success)
                {
                    await HttpContext.Session.SetAsync<ProcessPaymentRequest>("OrderPaymentInfo", null);

                    //get the order
                    var order = placeOrderResult.PlacedOrder;
                    order.Deleted = true;

                    await _orderService.UpdateOrderAsync(order);

                    var orderSessionContent = await CreateDinteroOrderSessionRequestAsync(order, orderSessionresponse);

                    if (_dinteroPaymentSettings.LogEnabled)
                       await _logger.InsertLogAsync(LogLevel.Information, "SavePaymentMethod > Dintero request body for update checkout sessions",
                            JsonConvert.SerializeObject(orderSessionContent) + " Token : " + _dinteroPaymentSettings.AccessToken);

                    var updateOrderSessionresponse = await _dinteroHttpClient.PutAsync(endpoint: "sessions/" + dinteroPreOrderSessionRequest.id + "?update_without_lock=true", content: orderSessionContent, accessToken: _dinteroPaymentSettings.AccessToken);
                    if (updateOrderSessionresponse.IsSuccessStatusCode)
                    {
                        cart = await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), ShoppingCartType.ShoppingCart, (await _storeContext.GetCurrentStoreAsync()).Id);

                        _httpContextAccessor.HttpContext.Items.Remove("nop.TaxTotal");

                        var model = await _shoppingCartModelFactory.PrepareShoppingCartModelAsync(new ShoppingCartModel(), cart,
                            isEditable: false,
                            prepareAndDisplayOrderReviewData: false);

                        return Json(new
                        {
                            Success = true,
                            orderSummary = await RenderPartialViewToStringAsync("/Plugins/"+ Defaults.SYSTEM_NAME +"/Themes/"+ await _themeContext.GetWorkingThemeNameAsync() + "/Views/DinteroCheckout/_OrderSummary.cshtml", model),
                        });
                    }
                    else
                        await _logger.InsertLogAsync(LogLevel.Error, "SavePaymentMethod > Dintero response body for update checkout sessions", await updateOrderSessionresponse.Content.ReadAsStringAsync());
                }

                //error
                var errors = new List<string>();
                foreach (var error in placeOrderResult.Errors)
                    errors.Add(error);

                if (errors.Count() > 0)
                    return Json(new { error = errors.Count(), message = errors });
                else
                    return Json(new { error = 1, message = "Something went wrong, Please try again." });
            }

            return Json(new { error = 1, message = "Something went wrong, Please try again." });
        }
        catch (Exception exc)
        {
            await _logger.WarningAsync(exc.Message, exc, await _workContext.GetCurrentCustomerAsync());
            return Json(new { error = 1, message = exc.Message });
        }
    }

    #endregion
}
