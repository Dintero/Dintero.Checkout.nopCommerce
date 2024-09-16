// Note: Dintero couldn't be supported order subtotal and total discount.

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Vendors;
using Nop.Core.Http.Extensions;
using Nop.Plugin.Payments.Dintero.Domain;
using Nop.Plugin.Payments.Dintero.Infrastructure;
using Nop.Plugin.Payments.Dintero.Models;
using Nop.Plugin.Payments.Dintero.Services;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Services.Vendors;
using Nop.Web.Framework.Controllers;
using PTX.Plugin.Payments.Dintero.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.Dintero.Controllers;

public class DinteroHandlerController : BasePluginController
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
    private readonly ICustomerActivityService _customerActivityService;
    
    private static LockProvider<int> LockProvider = new LockProvider<int>();

    #endregion

    #region Ctor

    public DinteroHandlerController(ILogger logger,
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
        ICustomerActivityService customerActivityService)
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
        _customerActivityService = customerActivityService;
    }

    #endregion

    #region Utilities

    protected async Task<Order> DinteroOrderPlaceProcessAsync(string id = "",
        string sessionId = "",
        string merchant_reference = "",
        string transaction_id = "",
        string error = "",
        string time = "")
    {
        if (_dinteroPaymentSettings.LogEnabled)
        {
            var method = await HttpContext.Session.GetAsync<string>("HanlderMethod");
            await _logger.InsertLogAsync(LogLevel.Information, $"Start dintero order place process with {method}");
        }

        var order = await _orderService.GetOrderByGuidAsync(new Guid(merchant_reference));
        if (order == null)
            order = await _orderService.GetOrderByGuidAsync(new Guid(sessionId));

        var customer = await _customerService.GetCustomerByGuidAsync(new Guid(id));
        if (customer == null)
            customer = await _workContext.GetCurrentCustomerAsync();

        var customerLastOrderId = await _genericAttributeService.GetAttributeAsync<int?>(customer, PluginDefaults.DinteroCustomerLastOrder);

        if (customerLastOrderId.HasValue)
        {
            if (_dinteroPaymentSettings.LogEnabled)
                await _logger.InsertLogAsync(LogLevel.Information, "Dintero Order Place Process customerLastOrderId: " + customerLastOrderId);

            var lastOrder = await _orderService.GetOrderByIdAsync(customerLastOrderId.Value);
            if (lastOrder != null)
            {
                var pastTransactionId = await _genericAttributeService.GetAttributeAsync<string>(lastOrder, PluginDefaults.DinteroCustomerTranscationId);

                if (!string.IsNullOrEmpty(pastTransactionId) && pastTransactionId == transaction_id)
                {
                    //await _genericAttributeService.SaveAttributeAsync<string>(lastOrder, PluginDefaults.DinteroCustomerTranscationId, "");
                    //await _genericAttributeService.SaveAttributeAsync<int?>(customer, PluginDefaults.DinteroCustomerLastOrder, null);

                    if (_dinteroPaymentSettings.LogEnabled)
                       await _logger.InsertLogAsync(LogLevel.Information, "Same Last Order : " + lastOrder.Id);

                    return lastOrder;
                }
            }
        }

        try
        {
            var ordertransactionResponse = await _dinteroHttpClient.GetAsync(endpoint: "transactions/" + transaction_id);

            if (_dinteroPaymentSettings.LogEnabled)
               await _logger.InsertLogAsync(LogLevel.Information, "ordertransactionResponse : ", ordertransactionResponse);

            if (!string.IsNullOrEmpty(ordertransactionResponse))
            {
                var isTransactionResponsesInList = false;
                var isTransactionResponsesNotFail = true;

                try
                {
                    var transactionResponses = JsonConvert.DeserializeObject<List<DinteroTransactionResponse>>(ordertransactionResponse);
                    isTransactionResponsesInList = true;
                }
                catch (Exception ex)
                {
                    isTransactionResponsesInList = false;

                    try
                    {
                        var errorResponse = JsonConvert.DeserializeObject<TransactionErrorResponse>(ordertransactionResponse);
                        if (errorResponse != null && errorResponse.Error != null && !string.IsNullOrEmpty(errorResponse.Error.message))
                            isTransactionResponsesNotFail = false;
                    }
                    catch (Exception e)
                    {
                       await _logger.ErrorAsync("DinteroHandler error", e, customer);
                    }
                }
                if (!isTransactionResponsesInList
                    && isTransactionResponsesNotFail
                    && order != null)
                {
                    var transactionResponse = JsonConvert.DeserializeObject<DinteroTransactionResponse>(ordertransactionResponse);
                    if (transactionResponse != null && !transactionResponse.status.ToLowerInvariant().Equals("cancelled"))
                    {
                        int cardExpireMonth = 0; int cardExpireYear = 0;
                        string cardType = string.Empty; string cardName = string.Empty; string cardNumber = string.Empty;
                        if (transactionResponse.card != null)
                        {
                            if (!string.IsNullOrEmpty(transactionResponse.card.expiry_date))
                            {
                                var cardmy = transactionResponse.card.expiry_date.Split("/");
                                if (cardmy.Length >= 2) { 
                                    cardExpireMonth = Convert.ToInt32(cardmy[0]);
                                    cardExpireYear = Convert.ToInt32(cardmy[1]);
                                }
                            }
                            cardType = transactionResponse.card.brand;
                            cardName = transactionResponse.card.brand;
                            cardNumber = transactionResponse.card.masked_pan;
                        }
                        else
                            cardType = transactionResponse.payment_product_type;

                        order.CardType = !string.IsNullOrEmpty(cardType) ? _encryptionService.EncryptText(cardType) : string.Empty;
                        order.CardType = string.IsNullOrWhiteSpace(cardType) && !string.IsNullOrWhiteSpace(transactionResponse.payment_product) ? transactionResponse.payment_product : order.CardType;
                        order.CardName = !string.IsNullOrEmpty(cardName) ? _encryptionService.EncryptText(cardName) : string.Empty;
                        order.CardNumber = !string.IsNullOrEmpty(cardNumber) ? _encryptionService.EncryptText(cardNumber) : string.Empty;
                        order.MaskedCreditCardNumber = _encryptionService.EncryptText(_paymentService.GetMaskedCreditCardNumber(cardNumber));
                        order.CardExpirationMonth = cardExpireMonth > 0 ? _encryptionService.EncryptText(cardExpireMonth.ToString()) : string.Empty;
                        order.CardExpirationYear = cardExpireYear > 0 ? _encryptionService.EncryptText(cardExpireYear.ToString()) : string.Empty;
                        order.CustomerId = customer.Id;
                        order.StoreId = (await _storeContext.GetCurrentStoreAsync()).Id;

                        var customValues = _paymentService.DeserializeCustomValues(order);
                        if (customValues.Where(c => c.Key == PluginDefaults.DinteroTranscationId).Any())
                            customValues[PluginDefaults.DinteroTranscationId] = transaction_id;
                        else
                            customValues.Add(PluginDefaults.DinteroTranscationId, transaction_id);

                        var request = new ProcessPaymentRequest
                        {
                            CustomValues = customValues
                        };
                        order.CustomValuesXml = _paymentService.SerializeCustomValues(request);
                        Event transactioneventResponse = null;
                        if (transactionResponse.payment_product.ToLowerInvariant() == "collector")
                        {
                            transactioneventResponse = transactionResponse.events.Where(x => ((x.@event.ToLowerInvariant() == "initialize"
                            && x.transaction_status.ToLowerInvariant() == "on_hold")
                            || (x.@event.ToLowerInvariant() == "authorize" && x.transaction_status.ToLowerInvariant() == "authorized"))
                            && x.success).Select(x => x).LastOrDefault();
                        }
                        else
                        {
                            transactioneventResponse = transactionResponse.events.Where(x => x.@event.ToLowerInvariant() == "authorize"
                            && x.transaction_status.ToLowerInvariant() == "authorized"
                            && x.success).Select(x => x).FirstOrDefault();
                        }

                        if (transactioneventResponse != null)
                        {
                            order.PaymentStatus = PaymentStatus.Authorized;
                            order.AuthorizationTransactionCode = transactioneventResponse != null
                                ? transactioneventResponse.metadata.PayexTransactionNumber.HasValue
                                ? transactioneventResponse.metadata.PayexTransactionNumber.ToString()
                                : transactionResponse.metadata.PayexPaymentNumber.ToString()
                                : transactionResponse.metadata.PayexPaymentNumber.ToString();
                            order.AuthorizationTransactionId = transactioneventResponse != null
                                ? !string.IsNullOrEmpty(transactioneventResponse.metadata.PayexTransactionPayeeReference)
                                ? transactioneventResponse.metadata.PayexTransactionPayeeReference.ToString()
                                : !string.IsNullOrEmpty(transactionResponse.metadata.PayexPaymentPayeeInfoPayeeId)
                                ? transactionResponse.metadata.PayexPaymentPayeeInfoPayeeId.ToString()
                                : string.Empty
                                : string.Empty;
                            order.AuthorizationTransactionResult = transactioneventResponse != null
                                ? !string.IsNullOrEmpty(transactioneventResponse.metadata.PayexTransactionId)
                                ? transactioneventResponse.metadata.PayexTransactionId.ToString()
                                : !string.IsNullOrEmpty(transactionResponse.metadata.PayexPaymentId)
                                ? transactionResponse.metadata.PayexPaymentId.ToString()
                                : string.Empty
                                : string.Empty;

                            var orderCaptureTransactionEventResponse = transactionResponse.events.Where(x => x.@event == "CAPTURE" && x.success)
                                .Select(x => x).FirstOrDefault();
                            if (orderCaptureTransactionEventResponse != null)
                            {
                                order.OrderStatus = OrderStatus.Processing;
                                order.PaymentStatus = PaymentStatus.Paid;
                                order.CaptureTransactionId = orderCaptureTransactionEventResponse != null
                                    ? orderCaptureTransactionEventResponse.metadata.PayexTransactionNumber.HasValue
                                    ? orderCaptureTransactionEventResponse.metadata.PayexTransactionNumber.ToString()
                                    : transactionResponse.metadata.PayexPaymentNumber.ToString()
                                    : transactionResponse.metadata.PayexPaymentNumber.ToString();
                                order.CaptureTransactionResult = orderCaptureTransactionEventResponse != null
                                    ? !string.IsNullOrEmpty(orderCaptureTransactionEventResponse.metadata.PayexTransactionId)
                                    ? orderCaptureTransactionEventResponse.metadata.PayexTransactionId.ToString()
                                    : !string.IsNullOrEmpty(transactionResponse.metadata.PayexPaymentId)
                                    ? transactionResponse.metadata.PayexPaymentId.ToString()
                                    : string.Empty
                                    : string.Empty;
                            }
                            else
                            {
                                var errorTransactioneventResponse = transactionResponse.events.Where(x => x.@event == "CAPTURE" && !x.success)
                                .Select(x => x).FirstOrDefault();
                                if (errorTransactioneventResponse != null && errorTransactioneventResponse.error != null)
                                {
                                    var errorMessage = errorTransactioneventResponse.error.message;
                                    var errorCode = errorTransactioneventResponse.error.code;
                                    await _logger.InsertLogAsync(LogLevel.Error, "DinteroHandler capture transaction failed", $"Message : {errorMessage} || Code: {errorCode}", customer);
                                }
                            }
                        }
                        else
                        {

                            Event errorTransactioneventResponse = null;
                            if (transactionResponse.payment_product.ToLowerInvariant() == "collector")
                            {
                                errorTransactioneventResponse = transactionResponse.events.Where(x => ((x.@event.ToLowerInvariant() == "initialize"
                                && x.transaction_status.ToLowerInvariant() == "on_hold")
                                || (x.@event.ToLowerInvariant() == "authorize" && x.transaction_status.ToLowerInvariant() == "authorized"))
                                && !x.success).Select(x => x).LastOrDefault();
                            }
                            else
                            {
                                errorTransactioneventResponse = transactionResponse.events.Where(x => x.@event.ToLowerInvariant() == "authorize"
                                && x.transaction_status.ToLowerInvariant() == "authorized"
                                && !x.success).Select(x => x).LastOrDefault();
                            }


                            if (errorTransactioneventResponse != null && errorTransactioneventResponse.error != null)
                            {
                                order.PaymentStatus = PaymentStatus.Pending;
                                var errorMessage = errorTransactioneventResponse.error.message;
                                var errorCode = errorTransactioneventResponse.error.code;
                                await _logger.InsertLogAsync(LogLevel.Error, "DinteroHandler authorize transaction failed", $"Message : {errorMessage} || Code: {errorCode}", customer);
                            }
                        }

                        order.Deleted = false;
                        order.CreatedOnUtc = DateTime.UtcNow;
                        await _orderService.UpdateOrderAsync(order);

                        await _genericAttributeService.SaveAttributeAsync<string>(order, PluginDefaults.DinteroCustomerTranscationId, transaction_id);
                        await _genericAttributeService.SaveAttributeAsync<int>(customer, PluginDefaults.DinteroCustomerLastOrder, order.Id);
                        try
                        {

                            var requestMerchantReference2 = new
                            {
                                merchant_reference_2 = order.Id.ToString()
                            };
                            var requestMerchantReference2Content = JsonConvert.SerializeObject(requestMerchantReference2);

                            var requestMerchantReference2response = await _dinteroHttpClient.PutAsync(endpoint: "transactions/" + transaction_id, content: requestMerchantReference2Content, accessToken: _dinteroPaymentSettings.AccessToken);
                        }
                        catch (Exception e)
                        {
                            await _logger.ErrorAsync("DinteroHandler merchant error", e, customer);
                        }

                        await SendNotificationsAndSaveNotesAsync(order);

                        var orderItems = await _orderService.GetOrderItemsAsync(order.Id);

                        // Manage product inventory
                        await ProductAdjustInventoryAsync(order, orderItems);

                        // clear shopping cart
                        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart);
                        cart.ToList().ForEach(async sci => await _shoppingCartService.DeleteShoppingCartItemAsync(sci, false));

                        // clear discount usages
                        await _genericAttributeService.SaveAttributeAsync<string>(customer, NopCustomerDefaults.DiscountCouponCodeAttribute, null);
                        await _genericAttributeService.SaveAttributeAsync<string>(customer, NopCustomerDefaults.GiftCardCouponCodesAttribute, null);
                        
                        await _genericAttributeService.SaveAttributeAsync<string>(customer, NopCustomerDefaults.CheckoutAttributes, null);
                        await _genericAttributeService.SaveAttributeAsync<ShippingOption>(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, null);
                        await _genericAttributeService.SaveAttributeAsync<ShippingOption>(customer, NopCustomerDefaults.OfferedShippingOptionsAttribute, null);
                        await _genericAttributeService.SaveAttributeAsync<PickupPoint>(customer, NopCustomerDefaults.SelectedPickupPointAttribute, null);
                        await _genericAttributeService.SaveAttributeAsync<string>(customer, NopCustomerDefaults.SelectedPaymentMethodAttribute, null);
                        await _genericAttributeService.SaveAttributeAsync<string>(customer, NopCustomerDefaults.CheckoutAttributes, null);

                        return order;
                    }
                    else if (transactionResponse != null && transactionResponse.status.ToLowerInvariant().Equals("cancelled"))
                    {
                        await _orderProcessingService.CancelOrderAsync(order, true);
                        await LogEditOrderAsync(order.Id);
                    }
                }

            }

        }
        catch (Exception ex)
        {
            await _logger.ErrorAsync("DinteroHandler error", ex, customer);

        }
        return null;

    }

    protected virtual async Task LogEditOrderAsync(int orderId)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);

        await _customerActivityService.InsertActivityAsync("EditOrder",
            string.Format(await _localizationService.GetResourceAsync("ActivityLog.EditOrder"), order.CustomOrderNumber), order);
    }

    /// <summary>
    /// Send "order placed" notifications and save order notes
    /// </summary>
    /// <param name="order">Order</param>
    protected async Task SendNotificationsAndSaveNotesAsync(Order order)
    {
        //notes, messages
        await AddOrderNoteAsync(order, _workContext.OriginalCustomerIfImpersonated != null
            ? $"Order placed by a store owner ('{_workContext.OriginalCustomerIfImpersonated.Email}'. ID = {_workContext.OriginalCustomerIfImpersonated.Id}) impersonating the customer."
            : "Order placed");

        //send email notifications
        var orderPlacedStoreOwnerNotificationQueuedEmailIds = await _workflowMessageService.SendOrderPlacedStoreOwnerNotificationAsync(order, _localizationSettings.DefaultAdminLanguageId);
        if (orderPlacedStoreOwnerNotificationQueuedEmailIds.Any())
            await AddOrderNoteAsync(order, $"\"Order placed\" email (to store owner) has been queued. Queued email identifiers: {string.Join(", ", orderPlacedStoreOwnerNotificationQueuedEmailIds)}.");

        var orderPlacedAttachmentFilePath = _orderSettings.AttachPdfInvoiceToOrderPlacedEmail ?
           await _pdfService.SaveOrderPdfToDiskAsync(order) : null;
        var orderPlacedAttachmentFileName = _orderSettings.AttachPdfInvoiceToOrderPlacedEmail ?
            "order.pdf" : null;
        var orderPlacedCustomerNotificationQueuedEmailIds = await _workflowMessageService
            .SendOrderPlacedCustomerNotificationAsync(order, order.CustomerLanguageId, orderPlacedAttachmentFilePath, orderPlacedAttachmentFileName);
        if (orderPlacedCustomerNotificationQueuedEmailIds.Any())
            await AddOrderNoteAsync(order, $"\"Order placed\" email (to customer) has been queued. Queued email identifiers: {string.Join(", ", orderPlacedCustomerNotificationQueuedEmailIds)}.");

        var vendors = await GetVendorsInOrderAsync(order);
        foreach (var vendor in vendors)
        {
            var orderPlacedVendorNotificationQueuedEmailIds = await _workflowMessageService.SendOrderPlacedVendorNotificationAsync(order, vendor, _localizationSettings.DefaultAdminLanguageId);
            if (orderPlacedVendorNotificationQueuedEmailIds.Any())
               await AddOrderNoteAsync(order, $"\"Order placed\" email (to vendor) has been queued. Queued email identifiers: {string.Join(", ", orderPlacedVendorNotificationQueuedEmailIds)}.");
        }

        if (order.AffiliateId == 0)
            return;

        var orderPlacedAffiliateNotificationQueuedEmailIds = await _workflowMessageService.SendOrderPlacedAffiliateNotificationAsync(order, _localizationSettings.DefaultAdminLanguageId);
        if (orderPlacedAffiliateNotificationQueuedEmailIds.Any())
            await AddOrderNoteAsync(order, $"\"Order placed\" email (to affiliate) has been queued. Queued email identifiers: {string.Join(", ", orderPlacedAffiliateNotificationQueuedEmailIds)}.");
    }

    /// <summary>
    /// Add order note
    /// </summary>
    /// <param name="order">Order</param>
    /// <param name="note">Note text</param>
    protected async Task AddOrderNoteAsync(Order order, string note)
    {
        await _orderService.InsertOrderNoteAsync(new OrderNote
        {
            OrderId = order.Id,
            Note = note,
            DisplayToCustomer = false,
            CreatedOnUtc = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get a list of vendors in order (order items)
    /// </summary>
    /// <param name="order">Order</param>
    /// <returns>Vendors</returns>
    protected async Task<IList<Vendor>> GetVendorsInOrderAsync(Order order)
    {
        var pIds = (await _orderService.GetOrderItemsAsync(order.Id)).Select(x => x.ProductId).ToArray();

        return await _vendorService.GetVendorsByProductIdsAsync(pIds);
    }

    protected async Task ProductAdjustInventoryAsync(Order order, IList<OrderItem> orderItems)
    {
        for (int i = 0; i < orderItems.Count; i++)
        {
            var item = orderItems[i];
            var product = await _productService.GetProductByIdAsync(item.ProductId);
            if (product != null)
            {
                await _productService.AdjustInventoryAsync(product, -item.Quantity, item.AttributesXml,
                            string.Format(await _localizationService.GetResourceAsync("Admin.StockQuantityHistory.Messages.PlaceOrder"), order.Id));
            }
        }

    }
    #endregion

    #region Methods

    [HttpGet]
    public async Task<IActionResult> IPNHandler(string id,
        string sessionId,
        string merchant_reference,
        string transaction_id,
        string error)
    {
        // lock for currentcustomerId only
        var customer = await _customerService.GetCustomerByGuidAsync(new Guid(id));
        if (customer == null)
            customer = await _workContext.GetCurrentCustomerAsync();

        await HttpContext.Session.SetAsync<string>("HanlderMethod", "IPNHandler");
        if (_dinteroPaymentSettings.LogEnabled)
            await _logger.InsertLogAsync(LogLevel.Information, "IPNHandler Lock Customer Wait: " + customer.Id + "(" + customer.Email + ")");

        LockProvider.Wait(customer.Id);
        if (string.IsNullOrEmpty(error))
        {

            var order = await DinteroOrderPlaceProcessAsync(id: id,
                sessionId: sessionId,
                merchant_reference: merchant_reference,
                transaction_id: transaction_id,
                error: error);

            if (_dinteroPaymentSettings.LogEnabled)
            {
                if (order != null)
                   await _logger.InsertLogAsync(LogLevel.Information, "IPNHandler call with order placed successfully", "Order number : " + order.CustomOrderNumber + "Order Id: " + order.Id);
                else
                   await _logger.InsertLogAsync(LogLevel.Information, "IPNHandler call with failed to order placed");
            }

            if (order != null)
            {
                // release the lock
                LockProvider.Release(customer.Id);

                if (_dinteroPaymentSettings.LogEnabled)
                   await _logger.InsertLogAsync(LogLevel.Information, "IPNHandler Lock Customer Release: " + customer.Id + "(" + customer.Email + ")");

                await _genericAttributeService.SaveAttributeAsync<string>(customer, PluginDefaults.DinteroOrderSessionId, null, (await _storeContext.GetCurrentStoreAsync()).Id);
                await _genericAttributeService.SaveAttributeAsync<DateTime?>(customer, PluginDefaults.DinteroOrderSessionIdGeneratedTime, null, (await _storeContext.GetCurrentStoreAsync()).Id);

                return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
            }
        }

        _notificationService.ErrorNotification(error + await _localizationService.GetResourceAsync("Plugin.Payments.Dintero.IPNHandler.Error.Message"));

        try
        {
            var orderSessionresponse = await _dinteroHttpClient.GetAsync(endpoint: "transactions/" + transaction_id);
            var isTransactionResponsesInList = false;
            var isTransactionResponsesInListButAuth = false;
            var isTransactionResponsesNotFail = true;

            try
            {
                var transactionResponses = JsonConvert.DeserializeObject<List<DinteroTransactionResponse>>(orderSessionresponse);
                if (transactionResponses.Any(x => x.events.Any(y => y.@event == "AUTHORIZE" && y.success)))
                    isTransactionResponsesInListButAuth = true;
                else
                    isTransactionResponsesInListButAuth = false;

                isTransactionResponsesInList = true;
            }
            catch (Exception)
            {
                isTransactionResponsesInList = false;

                try
                {
                    var errorResponse = JsonConvert.DeserializeObject<TransactionErrorResponse>(orderSessionresponse);
                    if (errorResponse != null && errorResponse.Error != null && !string.IsNullOrEmpty(errorResponse.Error.message))
                        isTransactionResponsesNotFail = false;
                }
                catch (Exception)
                {
                }
            }

            if (isTransactionResponsesInListButAuth && isTransactionResponsesInList && !isTransactionResponsesNotFail)
            {
                var response = await _dinteroHttpClient.PostAsync(endpoint: "transactions/" + transaction_id + "/void", content: "", accessToken: _dinteroPaymentSettings.AccessToken);
            }

        }
        catch (Exception ex)
        {
            await _logger.WarningAsync(ex.Message, ex, await _workContext.GetCurrentCustomerAsync());
        }

        await HttpContext.Session.SetAsync<string>("HanlderMethod", null);
        await _genericAttributeService.SaveAttributeAsync<string>(customer, PluginDefaults.DinteroOrderSessionId, null, (await _storeContext.GetCurrentStoreAsync()).Id);
        await _genericAttributeService.SaveAttributeAsync<DateTime?>(customer, PluginDefaults.DinteroOrderSessionIdGeneratedTime, null, (await _storeContext.GetCurrentStoreAsync()).Id);
        // release the lock
        LockProvider.Release(customer.Id);

        if (_dinteroPaymentSettings.LogEnabled)
           await _logger.InsertLogAsync(LogLevel.Information, "IPNHandler Lock Customer Release: " + customer.Id + "(" + customer.Email + ")");

        return RedirectToRoute("DinteroCheckout");
    }

    [HttpGet]
    public async Task<IActionResult> CallBackHandler(string id,
        string sessionId,
        string merchant_reference,
        string transaction_id,
        string time)
    {
        var customer = await _customerService.GetCustomerByGuidAsync(new Guid(id));
        if (customer == null)
            customer = await _workContext.GetCurrentCustomerAsync();

        await HttpContext.Session.SetAsync("HanlderMethod", "CallBack");
        if (_dinteroPaymentSettings.LogEnabled)
           await _logger.InsertLogAsync(LogLevel.Information, "CallBack Handler Lock Customer Wait: " + customer.Id + "(" + customer.Email + ")");

        LockProvider.Wait(customer.Id);

        var order = await DinteroOrderPlaceProcessAsync(id: id,
            sessionId: sessionId,
            merchant_reference: merchant_reference,
            transaction_id: transaction_id,
            time: time);

        if (_dinteroPaymentSettings.LogEnabled)
        {
            if (order != null)
               await _logger.InsertLogAsync(LogLevel.Information, "Callback call with order placed successfully", "Order number : " + order.CustomOrderNumber + "Order Id: " + order.Id);
            else
               await _logger.InsertLogAsync(LogLevel.Information, "Callback call with failed to order placed");
            
        }

        await HttpContext.Session.SetAsync<string>("HanlderMethod", null);
        await _genericAttributeService.SaveAttributeAsync<string>(customer, PluginDefaults.DinteroOrderSessionId, null, (await _storeContext.GetCurrentStoreAsync()).Id);
        await _genericAttributeService.SaveAttributeAsync<DateTime?>(customer, PluginDefaults.DinteroOrderSessionIdGeneratedTime, null, (await _storeContext.GetCurrentStoreAsync()).Id);
        // release the lock
        LockProvider.Release(customer.Id);

        if (_dinteroPaymentSettings.LogEnabled)
           await _logger.InsertLogAsync(LogLevel.Information, "CallBack Handler Lock Customer release: " + customer.Id + "(" + customer.Email + ")");

        return Ok();

    }

    #endregion
}
