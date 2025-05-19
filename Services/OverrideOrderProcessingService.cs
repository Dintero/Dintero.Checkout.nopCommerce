using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Core.Events;
using Nop.Core.Infrastructure;
using Nop.Plugin.Payments.Dintero.Domain;
using Nop.Services.Affiliates;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Services.Vendors;

namespace Nop.Plugin.Payments.Dintero.Services
{
    /// <summary>
    /// Order processing service
    /// </summary>
    public partial class OverrideOrderProcessingService : OrderProcessingService, IOverrideOrderProcessingService
    {

        #region Fields

        private readonly IStoreContext _storeContext;

        #endregion

        #region Ctor
        public OverrideOrderProcessingService(CurrencySettings currencySettings,
        IAddressService addressService,
        IAffiliateService affiliateService,
        ICheckoutAttributeFormatter checkoutAttributeFormatter,
        ICountryService countryService,
        ICurrencyService currencyService,
        ICustomerActivityService customerActivityService,
        ICustomerService customerService,
        ICustomNumberFormatter customNumberFormatter,
        IDiscountService discountService,
        IEncryptionService encryptionService,
        IEventPublisher eventPublisher,
        IGenericAttributeService genericAttributeService,
        IGiftCardService giftCardService,
        ILanguageService languageService,
        ILocalizationService localizationService,
        ILogger logger,
        IOrderService orderService,
        IOrderTotalCalculationService orderTotalCalculationService,
        IPaymentPluginManager paymentPluginManager,
        IPaymentService paymentService,
        IPdfService pdfService,
        IPriceCalculationService priceCalculationService,
        IPriceFormatter priceFormatter,
        IProductAttributeFormatter productAttributeFormatter,
        IProductAttributeParser productAttributeParser,
        IProductService productService,
        IReturnRequestService returnRequestService,
        IRewardPointService rewardPointService,
        IShipmentService shipmentService,
        IShippingService shippingService,
        IShoppingCartService shoppingCartService,
        IStateProvinceService stateProvinceService,
        IStaticCacheManager staticCacheManager,
        IStoreMappingService storeMappingService,
        IStoreService storeService,
        ITaxService taxService,
        IVendorService vendorService,
        IWebHelper webHelper,
        IWorkContext workContext,
        IWorkflowMessageService workflowMessageService,
        LocalizationSettings localizationSettings,
        OrderSettings orderSettings,
        PaymentSettings paymentSettings,
        RewardPointsSettings rewardPointsSettings,
        ShippingSettings shippingSettings,
        TaxSettings taxSettings,
        IStoreContext storeContext) : base(currencySettings,
        addressService,
        affiliateService,
        checkoutAttributeFormatter,
        countryService,
        currencyService,
        customerActivityService,
        customerService,
        customNumberFormatter,
        discountService,
        encryptionService,
        eventPublisher,
        genericAttributeService,
        giftCardService,
        languageService,
        localizationService,
        logger,
        orderService,
        orderTotalCalculationService,
        paymentPluginManager,
        paymentService,
        pdfService,
        priceCalculationService,
        priceFormatter,
        productAttributeFormatter,
        productAttributeParser,
        productService,
        returnRequestService,
        rewardPointService,
        shipmentService,
        shippingService,
        shoppingCartService,
        stateProvinceService,
        staticCacheManager,
        storeMappingService,
        storeService,
        taxService,
        vendorService,
        webHelper,
        workContext,
        workflowMessageService,
        localizationSettings,
        orderSettings,
        paymentSettings,
        rewardPointsSettings,
        shippingSettings,
        taxSettings)
        {
            _storeContext = storeContext;
            _storeContext = storeContext;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Places an order
        /// </summary>
        /// <param name="processPaymentRequest">Process payment request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the place order result
        /// </returns>
        public virtual async Task<PlaceOrderResult> PlaceOrderAsync(ProcessPaymentRequest processPaymentRequest)
        {
            ArgumentNullException.ThrowIfNull(processPaymentRequest);

            if (processPaymentRequest.OrderGuid == Guid.Empty)
                throw new Exception("Order GUID is not generated");

            //prepare order details
            var details = await PreparePlaceOrderDetailsAsync(processPaymentRequest);

            async Task<PlaceOrderResult> placeOrder(PlaceOrderContainer placeOrderContainer)
            {
                var result = new PlaceOrderResult();

                try
                {
                    var processPaymentResult =
                        await GetProcessPaymentResultAsync(processPaymentRequest, placeOrderContainer)
                        ?? throw new NopException("processPaymentResult is not available");

                    if (processPaymentResult.Success)
                    {
                        var order = await SaveOrderDetailsAsync(processPaymentRequest, processPaymentResult,
                            placeOrderContainer);
                        result.PlacedOrder = order;

                        //move shopping cart items to order items
                        await MoveShoppingCartItemsToOrderItemsAsync(placeOrderContainer, order);

                        //discount usage history
                        await SaveDiscountUsageHistoryAsync(placeOrderContainer, order);

                        //gift card usage history
                        await SaveGiftCardUsageHistoryAsync(placeOrderContainer, order);

                        //recurring orders
                        if (placeOrderContainer.IsRecurringShoppingCart)
                            await CreateFirstRecurringPaymentAsync(processPaymentRequest, order);

                        //reset checkout data
                        await _customerService.ResetCheckoutDataAsync(placeOrderContainer.Customer,
                            processPaymentRequest.StoreId, clearCouponCodes: true, clearCheckoutAttributes: false, clearShippingMethod: false, clearPaymentMethod: false);
                        await _customerActivityService.InsertActivityAsync("PublicStore.PlaceOrder",
                            string.Format(await _localizationService.GetResourceAsync("ActivityLog.PublicStore.PlaceOrder"),
                                order.Id), order);

                        //raise event       
                        await _eventPublisher.PublishAsync(new OrderPlacedEvent(order));

                        //check order status
                        await CheckOrderStatusAsync(order);

                        if (order.PaymentStatus == PaymentStatus.Paid)
                            await ProcessOrderPaidAsync(order);
                    }
                    else
                        foreach (var paymentError in processPaymentResult.Errors)
                            result.AddError(string.Format(
                                await _localizationService.GetResourceAsync("Checkout.PaymentError"), paymentError));
                }
                catch (Exception exc)
                {
                    await _logger.ErrorAsync(exc.Message, exc);
                    result.AddError(exc.Message);
                }

                if (result.Success)
                    return result;

                //log errors
                var logError = result.Errors.Aggregate("Error while placing order. ",
                    (current, next) => $"{current}Error {result.Errors.IndexOf(next) + 1}: {next}. ");
                var customer = await _customerService.GetCustomerByIdAsync(processPaymentRequest.CustomerId);
                await _logger.ErrorAsync(logError, customer: customer);

                return result;
            }

            if (!_orderSettings.PlaceOrderWithLock)
                return await placeOrder(details);

            PlaceOrderResult result;
            var resource = details.Customer.Id.ToString();

            //the named mutex helps to avoid creating the same order in different threads,
            //and does not decrease performance significantly, because the code is blocked only for the specific cart.
            //you should be very careful, mutexes cannot be used in with the await operation
            //we can't use semaphore here, because it produces PlatformNotSupportedException exception on UNIX based systems
            using var mutex = new Mutex(false, resource);

            mutex.WaitOne();

            try
            {
                var cacheKey = _staticCacheManager.PrepareKey(NopOrderDefaults.OrderWithLockCacheKey, resource);
                cacheKey.CacheTime = _orderSettings.MinimumOrderPlacementInterval;

                var exist = _staticCacheManager.Get(cacheKey, () => false);

                if (exist)
                {
                    result = new PlaceOrderResult();
                    result.Errors.Add(_localizationService.GetResourceAsync("Checkout.MinOrderPlacementInterval").Result);
                }
                else
                {
                    result = placeOrder(details).Result;

                    if (result.Success)
                        _staticCacheManager.SetAsync(cacheKey, true).Wait();
                }
            }
            finally
            {
                mutex.ReleaseMutex();
            }

            return result;
        }

        /// <summary>
        /// Move shopping cart items to order items
        /// </summary>
        /// <param name="details">Place order container</param>
        /// <param name="order">Order</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        protected virtual async Task MoveShoppingCartItemsToOrderItemsAsync(PlaceOrderContainer details, Order order)
        {
            var dinteroOrderItems = new List<DinteroOrderItem>();
            var lineNumber = 1;

            var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);
            var paymentMethodofOrder = await _paymentPluginManager
                .LoadPluginBySystemNameAsync(order.PaymentMethodSystemName, customer, order.StoreId);
            var orderSettings = await EngineContext.Current.Resolve<ISettingService>().LoadSettingAsync<OrderSettings>();

            foreach (var sc in details.Cart)
            {
                var product = await _productService.GetProductByIdAsync(sc.ProductId);

                //prices
                var scUnitPrice = (await _shoppingCartService.GetUnitPriceAsync(sc, true)).unitPrice;
                var (scSubTotal, discountAmount, scDiscounts, _) = await _shoppingCartService.GetSubTotalAsync(sc, true);
                var scUnitPriceInclTax = await _taxService.GetProductPriceAsync(product, scUnitPrice, true, details.Customer);
                var scUnitPriceExclTax = await _taxService.GetProductPriceAsync(product, scUnitPrice, false, details.Customer);
                var scSubTotalInclTax = await _taxService.GetProductPriceAsync(product, scSubTotal, true, details.Customer);
                var scSubTotalExclTax = await _taxService.GetProductPriceAsync(product, scSubTotal, false, details.Customer);
                var discountAmountInclTax = await _taxService.GetProductPriceAsync(product, discountAmount, true, details.Customer);
                var discountAmountExclTax = await _taxService.GetProductPriceAsync(product, discountAmount, false, details.Customer);
                foreach (var disc in scDiscounts)
                    if (!_discountService.ContainsDiscount(details.AppliedDiscounts, disc))
                        details.AppliedDiscounts.Add(disc);

                //attributes
                var currentStore = await _storeContext.GetCurrentStoreAsync();
                //var attributeDescription = await _productAttributeFormatter.FormatAttributesAsync(product, attributesXml, customer, currentStore);
                var attributeDescription =
                    await _productAttributeFormatter.FormatAttributesAsync(product, sc.AttributesXml, details.Customer, currentStore);

                var itemWeight = await _shippingService.GetShoppingCartItemWeightAsync(sc);

                //save order item
                var orderItem = new OrderItem
                {
                    OrderItemGuid = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = product.Id,
                    UnitPriceInclTax = scUnitPriceInclTax.price,
                    UnitPriceExclTax = scUnitPriceExclTax.price,
                    PriceInclTax = scSubTotalInclTax.price,
                    PriceExclTax = scSubTotalExclTax.price,
                    OriginalProductCost = await _priceCalculationService.GetProductCostAsync(product, sc.AttributesXml),
                    AttributeDescription = attributeDescription,
                    AttributesXml = sc.AttributesXml,
                    Quantity = sc.Quantity,
                    DiscountAmountInclTax = discountAmountInclTax.price,
                    DiscountAmountExclTax = discountAmountExclTax.price,
                    DownloadCount = 0,
                    IsDownloadActivated = false,
                    LicenseDownloadId = 0,
                    ItemWeight = itemWeight,
                    RentalStartDateUtc = sc.RentalStartDateUtc,
                    RentalEndDateUtc = sc.RentalEndDateUtc,
                };

                await _orderService.InsertOrderItemAsync(orderItem);

                var discountLineForProduct = new List<OrderItemDiscountlines>();
                if (scDiscounts.Any())
                {
                    int discountLineCount = 1;
                    var discountDescription = string.Join(",", scDiscounts.Select(d => d.Name).ToList());
                    var discountIds = string.Join(",", scDiscounts.Select(d => d.Id).ToList());
                    var discountLine = new OrderItemDiscountlines
                    {
                        ItemId = orderItem.Id,
                        amount = !_taxSettings.PricesIncludeTax ? discountAmountExclTax.price * 100 : discountAmountInclTax.price * 100,
                        percentage = decimal.Zero,
                        description = discountDescription,
                        discount_type = "customer",
                        discount_id = discountIds,
                        line_id = discountLineCount,
                        discount_code = ""
                    };
                    discountLineForProduct.Add(discountLine);
                    discountLineCount++;
                }

                var vatAmount = (scSubTotalInclTax.price - scSubTotalExclTax.price);

                var item = new DinteroOrderItem
                {
                    id = product.Sku.ToString(),
                    ItemId = orderItem.Id,
                    line_id = lineNumber.ToString(),
                    description = product.Name,
                    quantity = orderItem.Quantity,
                    amount = _taxSettings.PricesIncludeTax ? scSubTotalInclTax.price * 100 : scSubTotalExclTax.price * 100,
                    vat_amount = vatAmount * 100,
                    vat = scSubTotalInclTax.taxRate,
                    discount_lines = discountLineForProduct
                };
                dinteroOrderItems.Add(item);
                lineNumber++;

                //gift cards
                await AddGiftCardsAsync(product, sc.AttributesXml, sc.Quantity, orderItem, scUnitPriceExclTax.price);

            }

            var dinteroOrderItemsJson = JsonConvert.SerializeObject(dinteroOrderItems);
            await _genericAttributeService.SaveAttributeAsync<string>(order, "OrderItemsWithDiscountLine", dinteroOrderItemsJson);

        }

        #endregion
    }
}