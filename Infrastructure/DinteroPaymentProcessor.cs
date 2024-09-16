// Note: Dintero couldn't be supported order subtotal and total discount.
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Tax;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Plugin.Payments.Dintero.Components;
using Nop.Plugin.Payments.Dintero.Domain;
using Nop.Plugin.Payments.Dintero.Models;
using Nop.Plugin.Payments.Dintero.Services;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Web.Framework.Menu;
using PTX.Plugin.Payments.Dintero.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.Dintero.Infrastructure;

/// <summary>
/// Dintero payment processor
/// </summary>
public class DinteroPaymentProcessor : BasePlugin, IPaymentMethod, IAdminMenuPlugin
{
    #region Fields

    private readonly DinteroPaymentSettings _dinteroPaymentSettings;
    private readonly IAddressService _addressService;
    private readonly ICustomerService _customerService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger _logger;
    private readonly IPaymentService _paymentService;
    private readonly ISettingService _settingService;
    private readonly IWebHelper _webHelper;
    private readonly IDinteroHttpClient _dinteroHttpClient;
    private readonly IStoreContext _storeContext;
    private readonly IWorkContext _workContext;
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly IOrderTotalCalculationService _orderTotalCalculationService;
    private readonly ICountryService _countryService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOrderService _orderService;
    
    #endregion

    #region Ctor

    public DinteroPaymentProcessor(DinteroPaymentSettings dinteroPaymentSettings,
        IAddressService addressService,
        ICustomerService customerService,
        ILocalizationService localizationService,
        ILogger logger,
        IPaymentService paymentService,
        ISettingService settingService,
        IWebHelper webHelper,
        IDinteroHttpClient dinteroHttpClient,
        IStoreContext storeContext,
        IWorkContext workContext,
        IGenericAttributeService genericAttributeService,
        IOrderTotalCalculationService orderTotalCalculationService,
        ICountryService countryService,
        IHttpContextAccessor httpContextAccessor,
        IOrderService orderService)
    {
        _dinteroPaymentSettings = dinteroPaymentSettings;
        _addressService = addressService;
        _customerService = customerService;
        _localizationService = localizationService;
        _logger = logger;
        _paymentService = paymentService;
        _settingService = settingService;
        _webHelper = webHelper;
        _dinteroHttpClient = dinteroHttpClient;
        _storeContext = storeContext;
        _workContext = workContext;
        _genericAttributeService = genericAttributeService;
        _orderTotalCalculationService = orderTotalCalculationService;
        _countryService = countryService;
        _httpContextAccessor = httpContextAccessor;
        _orderService = orderService;
    }

    #endregion

    #region Utilities

    protected async Task<bool> GetAcessTokenAsync()
    {
        //get auth token if not exist or expired
        if (string.IsNullOrEmpty(_dinteroPaymentSettings.AccessToken)
            || _dinteroPaymentSettings.TokenExpiresInDatetimeUTC == null
            || _dinteroPaymentSettings.TokenExpiresInDatetimeUTC < DateTime.UtcNow)
        {
            var baseURL = _dinteroPaymentSettings.UseSandbox ? _dinteroPaymentSettings.SandboxAuthEndpoint ?? string.Empty : _dinteroPaymentSettings.ProductionAuthEndpoint ?? string.Empty;
            //get auth token
            var dinteroPaymentRequest = new DinteroPaymentRequest
            {
                grant_type = "client_credentials",
                audience = $"https://{baseURL.TrimEnd('/')}/accounts/{_dinteroPaymentSettings.AccountId}"
            };
            var httpContent = JsonConvert.SerializeObject(dinteroPaymentRequest);

            if (_dinteroPaymentSettings.LogEnabled)
                await _logger.InsertLogAsync(LogLevel.Information, "Dintero request body for /auth/token", JsonConvert.SerializeObject(dinteroPaymentRequest));

            try
            {
                var tokenResponse = await _dinteroHttpClient.PostAsync("accounts/" + _dinteroPaymentSettings.AccountId + "/auth/token", httpContent);

                if ((tokenResponse != null || tokenResponse.Content != null) && tokenResponse.IsSuccessStatusCode)
                {
                    var dinteroPaymentResponse = JsonConvert.DeserializeObject<DinteroPaymentResponse>(await tokenResponse.Content.ReadAsStringAsync());

                    if (_dinteroPaymentSettings.LogEnabled)
                        await _logger.InsertLogAsync(LogLevel.Information, "Dintero response body for /auth/token ", JsonConvert.SerializeObject(dinteroPaymentResponse));

                    //save auth token
                    _dinteroPaymentSettings.AccessToken = dinteroPaymentResponse.access_token;
                    _dinteroPaymentSettings.TokenExpiresIn = dinteroPaymentResponse.expires_in;
                    _dinteroPaymentSettings.TokenExpiresInDatetimeUTC = DateTime.UtcNow.AddSeconds(dinteroPaymentResponse.expires_in);

                    await _settingService.SaveSettingAsync(_dinteroPaymentSettings, x => x.AccessToken, clearCache: false);
                    await _settingService.SaveSettingAsync(_dinteroPaymentSettings, x => x.TokenExpiresIn, clearCache: false);
                    await _settingService.SaveSettingAsync(_dinteroPaymentSettings, x => x.TokenExpiresInDatetimeUTC, clearCache: false);
                    await _settingService.ClearCacheAsync();
                    return true;
                }
                else
                   await _logger.InsertLogAsync(LogLevel.Error, "Dintero response body to access token", await tokenResponse.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
               await _logger.ErrorAsync("Dintero access token error.", ex);
            }
        }
        return false;
    }

    protected async Task<string> CreateDinteroOrderSessionRequestAsync(Order order)
    {

        var dinteroOrderItemsJson = await _genericAttributeService.GetAttributeAsync<string>(order, "OrderItemsWithDiscountLine");
        var dinteroOrderItems = JsonConvert.DeserializeObject<IList<DinteroOrderItem>>(dinteroOrderItemsJson);

        //prepare checkout session
        var storeLocation = _webHelper.GetStoreLocation();
        var orderItems = await _orderService.GetOrderItemsAsync(order.Id);
        var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);

        var creditcard = new DinteroOrderSessionRequest.Creditcard
        {
            enabled = true
        };
        var payex = new DinteroOrderSessionRequest.Payex
        {
            creditcard = creditcard,
        };
        var vipps = new DinteroOrderSessionRequest.Vipps
        {
            enabled = true,
        };
        var invoice = new DinteroOrderSessionRequest.Invoice
        {
            enabled = true,
            type = "payment_product_type"
        };
        var collector = new DinteroOrderSessionRequest.Collector
        {
            type = "payment_type",
            invoice = invoice,
        };
        var configuration = new DinteroOrderSessionRequest.Configuration
        {
            auto_capture = Convert.ToInt32(_dinteroPaymentSettings.TransactMode) == (int)TransactMode.AuthorizeAndCapture,
            default_payment_type = "payex.creditcard",
            payex = payex,
            vipps = vipps,
            collector = collector,
        };

        var taxRate = "";
        if (!string.IsNullOrEmpty(order.TaxRates))
            taxRate = order.TaxRates.Split(":").FirstOrDefault();

        var shippingOption = new DinteroOrderSessionRequest.ShippingOption
        {
            id = Guid.NewGuid().ToString(),
            line_id = Guid.NewGuid().ToString(),
            amount = order.OrderShippingExclTax > decimal.Zero
            ? order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax
            ? Convert.ToInt32(Math.Round(order.OrderShippingInclTax * 100))
            : Convert.ToInt32(Math.Round(order.OrderShippingExclTax * 100))
            : 0,
            @operator = order.ShippingRateComputationMethodSystemName,
            title = order.ShippingMethod,
            vat = !string.IsNullOrEmpty(taxRate) && order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax
            ? Convert.ToInt32(Math.Round(Convert.ToDecimal(taxRate)))
            : 0,
            vat_amount = !string.IsNullOrEmpty(taxRate) && order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax
            ? Convert.ToInt32(Math.Round((order.OrderShippingInclTax - order.OrderShippingExclTax) * 100)) : 0,
        };

        var orderRequest = new DinteroOrderSessionRequest.Order
        {
            merchant_reference = order.OrderGuid.ToString(),
            currency = order.CustomerCurrencyCode,
            shipping_option = shippingOption,
        };
        var url = new DinteroOrderSessionRequest.Url
        {
            return_url = string.Format(PluginDefaults.DINTERO_RETURN_URL, storeLocation, customer.CustomerGuid, orderRequest.merchant_reference),
            callback_url = string.Format(PluginDefaults.DINTERO_CALLBACK_URL, "https://gant.nopadvance.team/", customer.CustomerGuid, orderRequest.merchant_reference)
        };

        var payexCreditcard = new DinteroOrderSessionRequest.PayexCreditcard
        {
            payment_token = "",
            recurrence_token = ""
        };

        var tokens = new DinteroOrderSessionRequest.Tokens
        {
            PayexCreditcard = payexCreditcard
        };

        var dinteroOrderSessionRequest = new DinteroOrderSessionRequest
        {
            //configuration = configuration,
            order = orderRequest,
            url = url,
            profile_id = _dinteroPaymentSettings.ProfileId,
        };

        var discountLineCount = 1;

        //order total (and applied discounts, gift cards, reward points)
        var orderTotal = order.OrderTotal;
        dinteroOrderSessionRequest.order.amount = orderTotal * 100;

        var lineCount = 1;

        foreach (var item in orderItems)
        {
            var discountLineForProduct = new List<DinteroOrderSessionRequest.Discountlines>();

            var discountOrderItem = dinteroOrderItems.Where(x => x.ItemId == item.Id).Select(x => x).FirstOrDefault();
            for (int i = 0; i < discountOrderItem.discount_lines.Count; i++)
            {
                var discountLineItem = discountOrderItem.discount_lines[i];

                var discountLine = new DinteroOrderSessionRequest.Discountlines
                {
                    amount = discountLineItem.amount,
                    percentage = discountLineItem.percentage,
                    description = discountLineItem.description,
                    discount_type = discountLineItem.discount_type,
                    discount_id = discountLineItem.discount_id,
                    line_id = discountLineCount
                };
                discountLineForProduct.Add(discountLine);

                discountLineCount++;
            }

            dinteroOrderSessionRequest.order.items.Add(new DinteroOrderSessionRequest.Item()
            {
                id = discountOrderItem.id,
                line_id = lineCount.ToString(),
                description = discountOrderItem.description,
                quantity = discountOrderItem.quantity,
                amount = Math.Round(discountOrderItem.amount),
                vat = Math.Round(discountOrderItem.vat),
                vat_amount = Math.Round(discountOrderItem.vat_amount),
                discount_lines = discountLineForProduct
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
                var dinteroBillingAddress = new DinteroOrderSessionRequest.OrderAddress
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
        var customerRequest = new DinteroOrderSessionRequest.Customer
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
                    var dinteroShippingAddress = new DinteroOrderSessionRequest.OrderAddress
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

        var orderSessionContent = JsonConvert.SerializeObject(dinteroOrderSessionRequest);
        if (_dinteroPaymentSettings.LogEnabled)
            await _logger.InsertLogAsync(LogLevel.Information, "Dintero request body for checkout sessions",
                JsonConvert.SerializeObject(dinteroOrderSessionRequest) + " Token : " + _dinteroPaymentSettings.AccessToken);

        return orderSessionContent;

    }

    #endregion

    #region Methods

    /// <summary>
    /// Process a payment
    /// </summary>
    /// <param name="processPaymentRequest">Payment info required for an order processing</param>
    /// <returns>Process payment result</returns>
    public Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
    {
        return Task.FromResult(new ProcessPaymentResult());
    }

    /// <summary>
    /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
    /// </summary>
    /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
    public async Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
    {
        //get auth token if not exist or expired
        if (string.IsNullOrEmpty(_dinteroPaymentSettings.AccessToken)
            || _dinteroPaymentSettings.TokenExpiresInDatetimeUTC == null
            || _dinteroPaymentSettings.TokenExpiresInDatetimeUTC < DateTime.UtcNow)
            await _dinteroHttpClient.GenerateTokenAsync("auth/token");

        var content = await CreateDinteroOrderSessionRequestAsync(postProcessPaymentRequest.Order);

        //order session response
        try
        {
            var orderSessionresponse = await _dinteroHttpClient.PostAsync(endpoint: "sessions-profile", content: content, accessToken: _dinteroPaymentSettings.AccessToken);

            if (orderSessionresponse == null || orderSessionresponse.Content == null)
                _httpContextAccessor.HttpContext.Response.Redirect("");

            if (orderSessionresponse.IsSuccessStatusCode)
            {
                var dinteroOrderSessionResponse = JsonConvert.DeserializeObject<DinteroOrderSessionResponse>(await orderSessionresponse.Content.ReadAsStringAsync());
                if (_dinteroPaymentSettings.LogEnabled)
                   await _logger.InsertLogAsync(LogLevel.Information, "Dintero response body for checkout sessions", content + " Token : " + _dinteroPaymentSettings.AccessToken);

                if (dinteroOrderSessionResponse != null && !string.IsNullOrEmpty(dinteroOrderSessionResponse.url))
                    _httpContextAccessor.HttpContext.Response.Redirect(dinteroOrderSessionResponse.url);
            }
            else
               await _logger.InsertLogAsync(LogLevel.Error, "Dintero response body for checkout sessions", await orderSessionresponse.Content.ReadAsStringAsync());
        }
        catch (Exception ex)
        {
            await _logger.ErrorAsync("Dintero checkout sessions error.", ex);
        }
    }

    /// <summary>
    /// Gets additional handling fee
    /// </summary>
    /// <param name="cart">Shopping cart</param>
    /// <returns>Additional handling fee</returns>
    public async Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
    {
        var setting = await _settingService.LoadSettingAsync<DinteroPaymentSettings>();
        var result = await _orderTotalCalculationService.CalculatePaymentAdditionalFeeAsync(cart,
            setting.AdditionalFee, setting.AdditionalFeePercentage);

        return result;
    }

    /// <summary>
    /// Returns a value indicating whether payment method should be hidden during checkout
    /// </summary>
    /// <param name="cart">Shopping cart</param>
    /// <returns>true - hide; false - display.</returns>
    public Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
    {
        //you can put any logic here
        //for example, hide this payment method if all products in the cart are downloadable
        //or hide this payment method if current customer is from certain country

        return Task.FromResult(false);
    }

    public Task<string> GetPaymentMethodDescriptionAsync()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Captures payment
    /// </summary>
    /// <param name="capturePaymentRequest">Capture payment request</param>
    /// <returns>Capture payment result</returns>
    public async Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
    {
        var result = new CapturePaymentResult();

        var customValues = _paymentService.DeserializeCustomValues(capturePaymentRequest.Order);
        var paymenId = string.Empty;
        if (customValues.ContainsKey(PluginDefaults.DinteroTranscationId))
            paymenId = customValues[PluginDefaults.DinteroTranscationId].ToString();

        if (string.IsNullOrEmpty(paymenId))
            return null;
        try
        {
            var captureAmount = new
            {
                amount = capturePaymentRequest.Order.OrderTotal * 100
            };
            var body = JsonConvert.SerializeObject(captureAmount);

        captureRequest:
            var response = await _dinteroHttpClient.PostAsync(endpoint: "transactions/" + paymenId + "/capture", content: body, accessToken: _dinteroPaymentSettings.AccessToken);

            if ((response != null || response.Content != null) && response.IsSuccessStatusCode)
                result.NewPaymentStatus = PaymentStatus.Paid;
            else
            {
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    if (await GetAcessTokenAsync())
                        goto captureRequest;
                }

                result.NewPaymentStatus = capturePaymentRequest.Order.PaymentStatus;
                result.AddError("Capture Failed: " + errorResponse.message);
            }
        }
        catch (Exception ex)
        {
            await _logger.ErrorAsync("Capture Failed", ex, await _workContext.GetCurrentCustomerAsync());
        }
        return result;
    }

    /// <summary>
    /// Refunds a payment
    /// </summary>
    /// <param name="refundPaymentRequest">Request</param>
    /// <returns>Result</returns>
    public async Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
    {
        var result = new RefundPaymentResult();
        var customValues = _paymentService.DeserializeCustomValues(refundPaymentRequest.Order);
        var paymenId = string.Empty;
        if (customValues.ContainsKey(PluginDefaults.DinteroTranscationId))
            paymenId = customValues[PluginDefaults.DinteroTranscationId].ToString();

        if (string.IsNullOrEmpty(paymenId))
            return result;

        try
        {
            var refundAmount = new
            {
                amount = refundPaymentRequest.AmountToRefund * 100
            };
            var body = JsonConvert.SerializeObject(refundAmount);

        refundRequest:
            var response = await _dinteroHttpClient.PostAsync(endpoint: "transactions/" + paymenId + "/refund", content: body, accessToken: _dinteroPaymentSettings.AccessToken);
            if ((response != null || response.Content != null) && response.IsSuccessStatusCode)
            {
                var totalAmountRefunded = refundPaymentRequest.Order.RefundedAmount + refundPaymentRequest.AmountToRefund;
                result.NewPaymentStatus = refundPaymentRequest.Order.OrderTotal == totalAmountRefunded ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
            }
            else
            {
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    if (await GetAcessTokenAsync())
                        goto refundRequest;
                }
                result.NewPaymentStatus = refundPaymentRequest.Order.PaymentStatus;
                result.AddError("Refund Failed : " + errorResponse.message);
            }

        }
        catch (Exception ex)
        {
            var totalAmountRefunded = refundPaymentRequest.Order.RefundedAmount + refundPaymentRequest.AmountToRefund;
            await _logger.ErrorAsync(refundPaymentRequest.Order.OrderTotal == totalAmountRefunded ? "Refund Failed" : "Partial refund Failed", ex, await _workContext.GetCurrentCustomerAsync());
        }
        return result;
    }

    /// <summary>
    /// Voids a payment
    /// </summary>
    /// <param name="voidPaymentRequest">Request</param>
    /// <returns>Result</returns>
    public async Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
    {
        var result = new VoidPaymentResult();
        var customValues = _paymentService.DeserializeCustomValues(voidPaymentRequest.Order);
        var paymenId = string.Empty;
        if (customValues.ContainsKey(PluginDefaults.DinteroTranscationId))
            paymenId = customValues[PluginDefaults.DinteroTranscationId].ToString();

        if (string.IsNullOrEmpty(paymenId))
            return null;
        try
        {
        voidRequest:
            var response = await _dinteroHttpClient.PostAsync(endpoint: "transactions/" + paymenId + "/void", content: "", accessToken: _dinteroPaymentSettings.AccessToken);

            if ((response != null || response.Content != null) && response.IsSuccessStatusCode)
                result.NewPaymentStatus = PaymentStatus.Voided;
            else
            {
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    if (await GetAcessTokenAsync())
                        goto voidRequest;
                }
                result.NewPaymentStatus = voidPaymentRequest.Order.PaymentStatus;
                result.AddError("Void Failed : " + errorResponse.message);
            }
        }
        catch (Exception ex)
        {
            await _logger.ErrorAsync("Void Failed", ex, await _workContext.GetCurrentCustomerAsync());
        }
        return result;
    }

    /// <summary>
    /// Process recurring payment
    /// </summary>
    /// <param name="processPaymentRequest">Payment info required for an order processing</param>
    /// <returns>Process payment result</returns>
    public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
    {
        return Task.FromResult(new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } });
    }

    /// <summary>
    /// Cancels a recurring payment
    /// </summary>
    /// <param name="cancelPaymentRequest">Request</param>
    /// <returns>Result</returns>
    public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
    {
        return Task.FromResult(new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } });
    }


    /// <summary>
    /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
    /// </summary>
    /// <param name="order">Order</param>
    /// <returns>Result</returns>
    public Task<bool> CanRePostProcessPaymentAsync(Order order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        //it's not a redirection payment method. So we always return false
        return Task.FromResult(false);
    }

    /// <summary>
    /// Validate payment form
    /// </summary>
    /// <param name="form">The parsed form values</param>
    /// <returns>List of validating errors</returns>
    public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
    {
        return Task.FromResult<IList<string>>(new List<string>());
    }

    /// <summary>
    /// Get payment information
    /// </summary>
    /// <param name="form">The parsed form values</param>
    /// <returns>Payment info holder</returns>
    public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
    {
        return Task.FromResult(new ProcessPaymentRequest());
    }

    /// <summary>
    /// Gets a view component for displaying plugin in public store ("payment info" checkout step)
    /// </summary>
    public Type GetPublicViewComponent()
    {
        return typeof(PaymentInfoViewComponent);
    }

    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/Dintero/Configure";
    }

    public async Task ManageSiteMapAsync(SiteMapNode rootNode)
    {
        var DigitrollMenuItem = new SiteMapNode()
        {
            SystemName = "Digitroll.MainmenuItem",
            Title = "Digitroll",
            ControllerName = "",
            ActionName = "",
            Visible = true,
            RouteValues = new RouteValueDictionary() { { "area", "admin" } },
            IconClass = "icon-digitroll"
        };
        var menuItem = new SiteMapNode()
        {
            SystemName = "Payments.Dintero",
            Title = "Dintero Checkout",
            ControllerName = "Dintero",
            ActionName = "Configure",
            Visible = true,
            RouteValues = new RouteValueDictionary() { { "area", "admin" } },
            IconClass = "far fa-dot-circle"
        };
        var mainMenuNode = rootNode.ChildNodes.FirstOrDefault(x => x.SystemName == "Digitroll.MainmenuItem");
        if (mainMenuNode == null)
        {
            rootNode.ChildNodes.Add(DigitrollMenuItem);
            mainMenuNode = DigitrollMenuItem;
        }
        mainMenuNode.ChildNodes.Add(menuItem);
    }

    /// <summary>
    /// Install plugin
    /// </summary>
    public override async Task InstallAsync()
    {
        //settings
        var settings = new DinteroPaymentSettings
        {
            UseSandbox = true,
            SandboxURL = "checkout.dintero.com/v1",
            SandboxAuthEndpoint = "test.dintero.com/v1",
            SandboxDinteroCheckoutWebSDKEndpoint = "https://checkout.test.dintero.com",
            TransactMode = TransactMode.Authorize,
            SecretKey = string.Empty,
            ClientId = string.Empty,
            DefaultAddressId = 1,
            FallbackPhoneNo = "9999999"
        };
        await _settingService.SaveSettingAsync(settings);

        //locales
        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Payments.Dintero.Notes"] = "If you're using this gateway, ensure that your primary store currency is supported by Dintero.",
            ["Plugins.Payments.Dintero.Fields.SandboxURL"] = "Sandbox Url",
            ["Plugins.Payments.Dintero.Fields.SandboxURL.Hint"] = "Where to send sandbox transactions. Please enter the URL without 'https://' for example 'checkout.dintero.com'.",
            ["Plugins.Payments.Dintero.Fields.ProductionURL"] = "Production Url",
            ["Plugins.Payments.Dintero.Fields.ProductionURL.Hint"] = "Where to send real transactions. Please enter the URL without 'https://' for example 'checkout.dintero.com'.",
            ["Plugins.Payments.Dintero.Fields.UseSandbox"] = "Use Sandbox",
            ["Plugins.Payments.Dintero.Fields.UseSandbox.Hint"] = "Check to enable Sandbox (testing environment).",
            ["Plugins.Payments.Dintero.Fields.UseShippingAddressAsBilling"] = "Use shipping address.",
            ["Plugins.Payments.Dintero.Fields.UseShippingAddressAsBilling.Hint"] = "Check if you want to use the shipping address as a billing address.",
            ["Plugins.Payments.Dintero.Fields.TransactModeValues"] = "Transaction mode",
            ["Plugins.Payments.Dintero.Fields.TransactModeValues.Hint"] = "Choose transaction mode.",
            ["Plugins.Payments.Dintero.Fields.SecretKey"] = "Secret key",
            ["Plugins.Payments.Dintero.Fields.SecretKey.Hint"] = "Specify Secret key.",
            ["Plugins.Payments.Dintero.Fields.ClientId"] = "Client Id",
            ["Plugins.Payments.Dintero.Fields.ClientId.Hint"] = "Specify client identifier.",
            ["Plugins.Payments.Dintero.Fields.AccountId"] = "Account Id",
            ["Plugins.Payments.Dintero.Fields.AccountId.Hint"] = "Specify Account identifier.",
            ["Plugins.Payments.Dintero.Fields.AdditionalFee"] = "Additional fee",
            ["Plugins.Payments.Dintero.Fields.AdditionalFee.Hint"] = "Enter additional fee to charge your customers.",
            ["Plugins.Payments.Dintero.Fields.AdditionalFeePercentage"] = "Additional fee. Use percentage",
            ["Plugins.Payments.Dintero.Fields.AdditionalFeePercentage.Hint"] = "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.",
            ["Plugins.Payments.Dintero.PaymentMethodDescription"] = "Pay by credit / debit card",
            ["Plugins.Payments.Dintero.Fields.AccessToken"] = "Access token",
            ["Plugins.Payments.Dintero.Fields.AccessToken.Hint"] = "Access token.",
            ["Plugins.Payments.Dintero.Fields.TokenExpiresIn"] = "Token expires in",
            ["Plugins.Payments.Dintero.Fields.TokenExpiresIn.Hint"] = "Token expires in (total second).",
            ["Plugins.Payments.Dintero.Fields.TokenExpiresInDatetimeUTC"] = "Token expire time",
            ["Plugins.Payments.Dintero.Fields.TokenExpiresInDatetimeUTC.Hint"] = "Token expire time.",
            ["Plugins.Payments.Dintero.Fields.LogEnabled"] = "Log enabled",
            ["Plugins.Payments.Dintero.Fields.LogEnabled.Hint"] = "Log enabled/disabled.",
            ["Plugins.Payments.Dintero.Redirect.Payment.Link.Message"] = "For payment you will be redirected to Dintero.",
            ["Plugins.Payments.Dintero.Redirect.Payment.Button"] = "Proceed to payment",
            ["Plugins.Payments.Dintero.Fields.ProfileId"] = "Profile Id",
            ["Plugins.Payments.Dintero.Fields.ProfileId.Hint"] = "Specify profile identifier",
            ["Plugin.Payments.Dintero.IPNHandler.Error.Message"] = "Something went wrong, Please try again your payment process.",
            ["Plugins.Payments.Dintero.Fields.DefaultAddressId"] = "Default address id",
            ["Plugins.Payments.Dintero.Fields.DefaultAddressId.Hint"] = "If cutomer not have shipping address and address then this address id will be use for address.",
            ["Plugins.Payments.Dintero.Fields.FallbackPhoneNo"] = "Fallback phone no",
            ["Plugins.Payments.Dintero.Fields.FallbackPhoneNo.Hint"] = "If cutomer not have phone number in shipping address then this phone number will be use for shipping address object.",
            ["Plugins.Payments.Dintero.DinteroCheckout"] = "Dintero checkout",
            ["Plugins.Payments.Dintero.TryAgain"] = "Try again",
            ["Plugins.Payments.Dintero.AddressNotFound"] = "Address not found",
            ["Plugins.Payments.Dintero.Fields.SandboxDinteroCheckoutWebSDKEndpoint"] = "Sandbox dintero checkout web SDK endpoint",
            ["Plugins.Payments.Dintero.Fields.SandboxDinteroCheckoutWebSDKEndpoint.Hint"] = "Where to send sandbox transactions. for example 'https://checkout.test.dintero.com'.",
            ["Plugins.Payments.Dintero.Fields.SandboxAuthEndpoint"] = "Sandbox auth endpoint",
            ["Plugins.Payments.Dintero.Fields.SandboxAuthEndpoint.Hint"] = "Where to send sandbox transactions. Please enter the URL without 'https://' for example 'checkout.dintero.com'.",
            ["Plugins.Payments.Dintero.Fields.SandboxAuthAudience"] = "Sandbox auth audience",
            ["Plugins.Payments.Dintero.Fields.SandboxAuthAudience.Hint"] = "Where to send sandbox transactions. Please enter the URL without 'https://' for example 'checkout.dintero.com'.",
            ["Plugins.Payments.Dintero.Fields.ProductionDinteroCheckoutWebSDKEndpoint"] = "Production dintero checkout web SDK endpoint",
            ["Plugins.Payments.Dintero.Fields.ProductionDinteroCheckoutWebSDKEndpoint.Hint"] = "Where to send sandbox transactions. for example 'https://checkout.test.dintero.com'",
            ["Plugins.Payments.Dintero.Fields.ProductionAuthEndpoint"] = "Production auth endpoint",
            ["Plugins.Payments.Dintero.Fields.ProductionAuthEndpoint.Hint"] = "Where to send real transactions. Please enter the URL without 'https://' for example 'checkout.dintero.com'.",
            ["Plugins.Payments.Dintero.Fields.ProductionAuthAudience"] = "Production auth audience",
            ["Plugins.Payments.Dintero.Fields.ProductionAuthAudience.Hint"] = "Where to send real transactions. Please enter the URL without 'https://' for example 'checkout.dintero.com'.",
            ["plugin.payments.dintero.token.generate.button"] = "Generate token",
            ["Plugin.Payments.Dintero.Token.Generated.Successfully"] = "Dintero token generated successfully.",
            ["Plugin.Payments.Dintero.Token.Generated.failed"] = "Dintero token generated Failed.",
            ["Plugin.Payments.Dintero.See.Order.Summary"] = "Bag",
            ["Plugin.Payments.Dintero.YourOrder"] = "Your Order",
            ["Plugin.Payments.Dintero.EditCart"] = "Edit Cart",
            ["Plugin.Payments.Dintero.PaymentAdditionalFee"] = "Payment method additional fee",

        });

        await base.InstallAsync();
    }

    /// <summary>
    /// Uninstall plugin
    /// </summary>
    public override async Task UninstallAsync()
    {
        await base.UninstallAsync();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets a value indicating whether capture is supported
    /// </summary>
    public bool SupportCapture => true;

    /// <summary>
    /// Gets a value indicating whether partial refund is supported
    /// </summary>
    public bool SupportPartiallyRefund => true;

    /// <summary>
    /// Gets a value indicating whether refund is supported
    /// </summary>
    public bool SupportRefund => true;

    /// <summary>
    /// Gets a value indicating whether void is supported
    /// </summary>
    public bool SupportVoid => true;

    /// <summary>
    /// Gets a recurring payment type of payment method
    /// </summary>
    public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.Automatic;

    /// <summary>
    /// Gets a payment method type
    /// </summary>
    public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

    /// <summary>
    /// Gets a value indicating whether we should display a payment information page for this plugin
    /// </summary>
    public bool SkipPaymentInfo => false;

    /// <summary>
    /// Gets a payment method description that will be displayed on checkout pages in the public store
    /// </summary>
    public string PaymentMethodDescription => _localizationService.GetResourceAsync("Plugins.Payments.Dintero.PaymentMethodDescription").Result;

    #endregion

}
