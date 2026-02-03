using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ARM.Logistics.Payments.Square.Domain;
using ARM.Logistics.Payments.Square.Models;
using ARM.Logistics.Payments.Square.Services.Messages;
using ARM.Logistics.Payments.Square.Services.SquareOrderMappings;
using Microsoft.AspNetCore.WebUtilities;
using Nop.Core;
using Nop.Core.Domain.Payments;
using Nop.Core.Infrastructure;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Helpers;
using Nop.Services.Logging;
using Nop.Services.LogisticsLeads;
using Nop.Services.LogisticsQuotes;
using Nop.Services.Orders;
using Square;
using Square.Exceptions;
using Square.Models;
using SquareSdk = Square;

namespace ARM.Logistics.Payments.Square.Services
{
    /// <summary>
    /// Represents the Square payment manager
    /// </summary>
    public class SquarePaymentManager
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly ISettingService _settingService;
        private readonly IWorkContext _workContext;
        private readonly SquareAuthorizationHttpClient _squareAuthorizationHttpClient;
        private readonly IAddressService _addressService;
        private readonly ISquareOrderMappingService _squareOrderMappingService;
        private readonly IOrderService _orderService;

        private Dictionary<int, ParentOrderFinanceDetails> _parentOrderFinanceDetails = new();

        #endregion

        #region Ctor

        public SquarePaymentManager(ILogger logger,
            ISettingService settingService,
            IWorkContext workContext,
            SquareAuthorizationHttpClient squareAuthorizationHttpClient,
            IAddressService addressService,
            ISquareOrderMappingService squareOrderMappingService,
            IOrderService orderService)
        {
            _logger = logger;
            _settingService = settingService;
            _workContext = workContext;
            _squareAuthorizationHttpClient = squareAuthorizationHttpClient;
            _addressService = addressService;
            _squareOrderMappingService = squareOrderMappingService;
            _orderService = orderService;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Creates the Square Client
        /// </summary>
        /// <returns>The asynchronous task whose result contains the Square Client</returns>
        private async Task<ISquareClient> CreateSquareClientAsync()
        {
            return await CreateSquareClientAsync(0);
        }

        /// <summary>
        /// Creates the Square Client
        /// </summary>
        /// <param name="storeId">Store identifier for which configuration should be loaded</param>
        /// <returns>The asynchronous task whose result contains the Square Client</returns>
        private async Task<ISquareClient> CreateSquareClientAsync(int storeId)
        {
            var settings = await _settingService.LoadSettingAsync<SquarePaymentSettings>(storeId);

            //validate access token
            if (settings.UseSandbox && string.IsNullOrWhiteSpace(settings.AccessToken))
                throw new NopException("Sandbox access token should not be empty");

            var client = new SquareClient.Builder()
                .AccessToken(settings.AccessToken)
                .AddAdditionalHeader("user-agent", SquarePaymentDefaults.UserAgent);

            if (settings.UseSandbox)
                client.Environment(SquareSdk.Environment.Sandbox);
            else
                client.Environment(SquareSdk.Environment.Production);

            return client.Build();
        }

        private void ThrowErrorsIfExists(IList<Error> errors)
        {
            //check whether there are errors in the service response
            if (errors?.Any() ?? false)
            {
                var errorsMessage = string.Join(";", errors.Select(error => error.Detail));
                throw new NopException($"There are errors in the service response.\n{errorsMessage}");
            }
        }

        private async Task<string> CatchExceptionAsync(Exception exception)
        {
            //log full error
            var errorMessage = exception.Message;
            await _logger.ErrorAsync($"Square payment error: {errorMessage}.", exception, await _workContext.GetCurrentCustomerAsync());

            // check Square exception
            if (exception is ApiException apiException)
            {
                //try to get error details
                if (apiException?.Errors?.Any() ?? false)
                {
                    errorMessage = string.Join(";", apiException.Errors.Select(error => error.Detail));

                    await _logger.ErrorAsync($"Square payment api exception: {errorMessage}.");
                }
            }

            return $"{errorMessage}";
        }

        #endregion

        #region Methods

        #region Common

        /// <summary>
        /// Get selected active business locations
        /// </summary>
        /// <param name="storeId">Store identifier for which locations should be loaded</param>
        /// <returns>The asynchronous task whose result contains the Location</returns>
        public async Task<Location> GetSelectedActiveLocationAsync(int storeId)
        {
            var client = await CreateSquareClientAsync(storeId);
            var settings = await _settingService.LoadSettingAsync<SquarePaymentSettings>(storeId);

            if (string.IsNullOrWhiteSpace(settings.LocationId))
                return null;

            try
            {
                var locationResponse = client.LocationsApi.RetrieveLocation(settings.LocationId);
                if (locationResponse == null)
                    throw new NopException("No service response to get selected active location.");

                ThrowErrorsIfExists(locationResponse.Errors);

                var location = locationResponse.Location;
                if (location == null
                      || location.Status != SquarePaymentDefaults.Status.LOCATION_ACTIVE
                      || (!location.Capabilities?.Contains(SquarePaymentDefaults.Status.LOCATION_PROCESSING) ?? true))
                {
                    throw new NopException("There are no selected active location for the account");
                }

                return location;
            }
            catch (Exception exception)
            {
                await CatchExceptionAsync(exception);

                return null;
            }
        }

        /// <summary>
        /// Gets active business locations
        /// </summary>
        /// <param name="storeId">Store identifier for which locations should be loaded</param>
        /// <returns>The asynchronous task whose result contains the List of location</returns>
        public async Task<IList<Location>> GetActiveLocationsAsync(int storeId)
        {
            var client = await CreateSquareClientAsync(storeId);

            try
            {
                var listLocationsResponse = client.LocationsApi.ListLocations();
                if (listLocationsResponse == null)
                    throw new NopException("No service response to get active locations.");

                ThrowErrorsIfExists(listLocationsResponse.Errors);

                //filter active locations and locations that can process credit cards
                var activeLocations = listLocationsResponse.Locations
                    ?.Where(location => location?.Status == SquarePaymentDefaults.Status.LOCATION_ACTIVE &&
                        (location.Capabilities?.Contains(SquarePaymentDefaults.Status.LOCATION_PROCESSING) ?? false))
                    .ToList();
                if (!activeLocations?.Any() ?? true)
                    throw new NopException("There are no active locations for the account");

                return activeLocations;
            }
            catch (Exception exception)
            {
                await CatchExceptionAsync(exception);

                return new List<Location>();
            }
        }

        /// <summary>
        /// Get customer by identifier
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <param name="storeId">Store identifier for which customer should be loaded</param>
        /// <returns>The asynchronous task whose result contains the Customer</returns>
        public async Task<Customer> GetCustomerAsync(string customerId, int storeId)
        {
            //whether passed customer identifier exists
            if (string.IsNullOrWhiteSpace(customerId))
                return null;

            var client = await CreateSquareClientAsync(storeId);

            try
            {
                //get customer by identifier
                var retrieveCustomerResponse = client.CustomersApi.RetrieveCustomer(customerId);
                if (retrieveCustomerResponse == null)
                    throw new NopException("No service response to get customer");

                ThrowErrorsIfExists(retrieveCustomerResponse.Errors);

                return retrieveCustomerResponse.Customer;
            }
            catch (Exception exception)
            {
                await CatchExceptionAsync(exception);

                return null;
            }
        }

        /// <summary>
        /// Search customer by email, phone and contact name
        /// </summary>
        /// <param name="email">The email</param>
        /// <param name="phone">The phone number</param>
        /// <param name="contactName">The customer name</param>
        /// <param name="client">The square client identifier</param>
        /// <returns>The asynchronous task whose result contains the Customer</returns>
        public async Task<Customer> SearchCustomerAsync(string email, string phone, string contactName, ISquareClient client = null)
        {
            //whether passed customer identifier exists
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(contactName))
                return null;

            email = email.Trim().ToLowerInvariant();
            contactName = contactName.Trim();

            client ??= await CreateSquareClientAsync();

            try
            {
                SearchCustomersRequest searchCustomerRequest = new
                (
                    query: new CustomerQuery
                    (
                        new CustomerFilter
                        (
                            emailAddress: new CustomerTextFilter(exact: email),
                            phoneNumber: new CustomerTextFilter(exact: "+1 " + PhoneNumberFormatter.FormatToUSPhoneNumber(phone.Trim()))
                        )
                    ),
                    limit: 5
                );

                //get customer by identifier
                var retrieveCustomerResponse = await client.CustomersApi.SearchCustomersAsync(searchCustomerRequest);
                if (retrieveCustomerResponse?.Customers == null)
                    return null;

                ThrowErrorsIfExists(retrieveCustomerResponse.Errors);

                var customer = retrieveCustomerResponse.Customers.FirstOrDefault(c => string.Equals($"{c.GivenName} {c.FamilyName}".Trim(), contactName, StringComparison.InvariantCultureIgnoreCase));

                if (customer == null)
                    return null;

                return customer;
            }
            catch (Exception exception)
            {
                await CatchExceptionAsync(exception);

                return null;
            }
        }

        /// <summary>
        /// Create the new customer
        /// </summary>
        /// <param name="customerRequest">Request parameters to create customer</param>
        /// <param name="storeId">Store identifier for which customer should be created</param>
        /// <returns>The asynchronous task whose result contains the Customer</returns>
        public async Task<Customer> CreateCustomerAsync(CreateCustomerRequest customerRequest, int storeId)
        {
            if (customerRequest == null)
                throw new ArgumentNullException(nameof(customerRequest));

            var client = await CreateSquareClientAsync(storeId);

            try
            {
                //create the new customer
                var createCustomerResponse = client.CustomersApi.CreateCustomer(customerRequest);
                if (createCustomerResponse == null)
                    throw new NopException("No service response to create customer");

                ThrowErrorsIfExists(createCustomerResponse.Errors);

                return createCustomerResponse.Customer;
            }
            catch (Exception exception)
            {
                await CatchExceptionAsync(exception);

                return null;
            }
        }

        /// <summary>
        /// Create the new customer
        /// </summary>
        /// <param name="customerRequest">Request parameters to create customer</param>
        /// <param name="client">The square client identifier</param>
        /// <returns>The asynchronous task whose result contains the Customer and error</returns>
        public async Task<(Customer customer, string Error)> CreateCustomerAsync(CreateCustomerRequest customerRequest, ISquareClient client = null)
        {
            if (customerRequest == null)
                throw new ArgumentNullException(nameof(customerRequest));

            client ??= await CreateSquareClientAsync();

            try
            {
                //create the new customer
                var createCustomerResponse = client.CustomersApi.CreateCustomer(customerRequest);
                if (createCustomerResponse == null)
                    throw new NopException("No service response to create customer");

                ThrowErrorsIfExists(createCustomerResponse.Errors);

                return (createCustomerResponse.Customer, null);
            }
            catch (Exception exception)
            {
                return (null, await CatchExceptionAsync(exception));
            }
        }

        /// <summary>
        /// Create the new card of the customer
        /// </summary>
        /// <param name="cardRequest">Request parameters to create card of the customer</param>
        /// <param name="storeId">Store identifier for which customer card should be created</param>
        /// <returns>The asynchronous task whose result contains the Card</returns>
        public async Task<Card> CreateCustomerCardAsync(CreateCardRequest cardRequest, int storeId)
        {
            if (cardRequest == null)
                throw new ArgumentNullException(nameof(cardRequest));

            var client = await CreateSquareClientAsync(storeId);

            try
            {
                //create the new card of the customer
                var createCustomerCardResponse = client.CardsApi.CreateCard(cardRequest);
                if (createCustomerCardResponse == null)
                    throw new NopException("No service response to create customer card");

                ThrowErrorsIfExists(createCustomerCardResponse.Errors);

                return createCustomerCardResponse.Card;
            }
            catch (Exception exception)
            {
                await CatchExceptionAsync(exception);

                return null;
            }
        }

        #endregion

        #region Payment workflow

        /// <summary>
        /// Creates a payment
        /// </summary>
        /// <param name="paymentRequest">Request parameters to create payment</param>
        /// <param name="storeId">Store identifier for which payment should be created</param>
        /// <returns>The asynchronous task whose result contains the Payment and/or errors if exist</returns>
        public async Task<(Payment, string)> CreatePaymentAsync(CreatePaymentRequest paymentRequest, int storeId)
        {
            if (paymentRequest == null)
                throw new ArgumentNullException(nameof(paymentRequest));

            var client = await CreateSquareClientAsync(storeId);

            try
            {
                var paymentResponse = client.PaymentsApi.CreatePayment(paymentRequest);
                if (paymentResponse == null)
                    throw new NopException("No service response to create payment");

                ThrowErrorsIfExists(paymentResponse.Errors);

                return (paymentResponse.Payment, null);
            }
            catch (Exception exception)
            {
                return (null, await CatchExceptionAsync(exception));
            }
        }

        /// <summary>
        /// Completes a payment
        /// </summary>
        /// <param name="paymentId">Payment ID</param>
        /// <param name="completePaymentRequest">Complete payment request</param>
        /// <param name="storeId">Store identifier for which payment should be completed</param>
        /// <returns>The asynchronous task whose result contains the True if the payment successfully completed; otherwise false. And/or errors if exist</returns>
        public async Task<(bool, string)> CompletePaymentAsync(string paymentId, CompletePaymentRequest completePaymentRequest, int storeId)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
                return (false, null);

            var client = await CreateSquareClientAsync(storeId);

            try
            {
                var paymentResponse = await client.PaymentsApi.CompletePaymentAsync(paymentId, completePaymentRequest, new CancellationToken());
                if (paymentResponse == null)
                    throw new NopException("No service response to complete payment");

                ThrowErrorsIfExists(paymentResponse.Errors);

                //if there are no errors in the response, payment was successfully completed
                return (true, null);
            }
            catch (Exception exception)
            {
                return (false, await CatchExceptionAsync(exception));
            }
        }

        /// <summary>
        /// Cancels a payment
        /// </summary>
        /// <param name="paymentId">Payment ID</param>
        /// <param name="storeId">Store identifier for which payment should be canceled</param>
        /// <returns>The asynchronous task whose result contains the True if the payment successfully canceled; otherwise false. And/or errors if exist</returns>
        public async Task<(bool, string)> CancelPaymentAsync(string paymentId, int storeId)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
                return (false, null);

            var client = await CreateSquareClientAsync(storeId);

            try
            {
                var paymentResponse = client.PaymentsApi.CancelPayment(paymentId);
                if (paymentResponse == null)
                    throw new NopException("No service response to cancel payment");

                ThrowErrorsIfExists(paymentResponse.Errors);

                //if there are no errors in the response, payment was successfully canceled
                return (true, null);
            }
            catch (Exception exception)
            {
                return (false, await CatchExceptionAsync(exception));
            }
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request parameters to create refund</param>
        /// <param name="storeId">Store identifier for which payment should be refunded</param>
        /// <returns>The asynchronous task whose result contains the Payment refund and/or errors if exist</returns>
        public async Task<(PaymentRefund, string)> RefundPaymentAsync(RefundPaymentRequest refundPaymentRequest, int storeId)
        {
            if (refundPaymentRequest == null)
                throw new ArgumentNullException(nameof(refundPaymentRequest));

            var client = await CreateSquareClientAsync(storeId);

            try
            {
                var refundPaymentResponse = client.RefundsApi.RefundPayment(refundPaymentRequest);
                if (refundPaymentResponse == null)
                    throw new NopException("No service response to refund payment");

                ThrowErrorsIfExists(refundPaymentResponse.Errors);

                return (refundPaymentResponse.Refund, null);
            }
            catch (Exception exception)
            {
                return (null, await CatchExceptionAsync(exception));
            }
        }

        #endregion

        #region OAuth2 authorization

        /// <summary>
        /// Generate URL for the authorization permissions page
        /// </summary>
        /// <param name="storeId">Store identifier for which authorization url should be created</param>
        /// <returns>The asynchronous task whose result contains the URL</returns>
        public async Task<string> GenerateAuthorizeUrlAsync(int storeId)
        {
            var serviceUrl = "https://connect.squareup.com/oauth2/authorize";

            //list of all available permission scopes
            var permissionScopes = new List<string>
            {
                //GET endpoints related to a merchant's business and location entities.
                "MERCHANT_PROFILE_READ",

                //GET endpoints related to transactions and refunds.
                "PAYMENTS_READ",

                //POST, PUT, and DELETE endpoints related to transactions and refunds
                "PAYMENTS_WRITE",

                //GET endpoints related to customer management.
                "CUSTOMERS_READ",

                //POST, PUT, and DELETE endpoints related to customer management.
                "CUSTOMERS_WRITE",

                //GET endpoints related to settlements (deposits).
                "SETTLEMENTS_READ",

                //GET endpoints related to a merchant's bank accounts.
                "BANK_ACCOUNTS_READ",

                //GET endpoints related to a merchant's item library.
                "ITEMS_READ",

                //POST, PUT, and DELETE endpoints related to a merchant's item library.
                "ITEMS_WRITE",

                //GET endpoints related to a merchant's orders.
                "ORDERS_READ",

                //POST, PUT, and DELETE endpoints related to a merchant's orders.
                "ORDERS_WRITE",

                //GET endpoints related to employee management.
                "EMPLOYEES_READ",

                //POST, PUT, and DELETE endpoints related to employee management.
                "EMPLOYEES_WRITE",

                //GET endpoints related to employee timecards.
                "TIMECARDS_READ",

                //POST, PUT, and DELETE endpoints related to employee timecards.
                "TIMECARDS_WRITE",

                //POST, PUT, and DELETE endpoints related to a invoice item library.
                "INVOICES_WRITE",

                //Get endpoints related to a invoice item library.
                "INVOICES_READ",

                //POST, PUT, and DELETE endpoints related to a inventory item library.
                "INVENTORY_WRITE",

                //Get endpoints related to a inventory item library.
                "INVENTORY_READ",

                //Get endpoints related to a payment write shared online item library.
                "PAYMENTS_WRITE_SHARED_ONFILE",

                //Get endpoints related to a payment write additional recipient item library.
                "PAYMENTS_WRITE_ADDITIONAL_RECIPIENTS",

                //Get endpoints related to a payouts item library.
                "PAYOUTS_READ",

                //POST, PUT, and DELETE endpoints related to a subscriptions item library.
                "SUBSCRIPTIONS_WRITE",

                //Get endpoints related to a subscriptions item library.
                "SUBSCRIPTIONS_READ"
            };

            //request all of the permissions
            var requestingPermissions = string.Join(" ", permissionScopes);

            var settings = await _settingService.LoadSettingAsync<SquarePaymentSettings>(storeId);

            //create query parameters for the request
            var queryParameters = new Dictionary<string, string>
            {
                //Route.
                //["route"] = "oauth2/authorize",

                //The application ID.
                ["client_id"] = settings.ApplicationId,

                //Indicates whether you want to receive an authorization code ("code") or an access token ("token").
                ["response_type"] = "code",

                //A space-separated list of the permissions your application is requesting. 
                ["scope"] = requestingPermissions,

                //The locale to present the Permission Request form in. Currently supported values are en-US, en-CA, es-US, fr-CA, and ja-JP.
                ["locale"] = "en-US",

                //If "false", the Square merchant must log in to view the Permission Request form, even if they already have a valid user session.
                ["session"] = "false",

                //Include this parameter and verify its value to help protect against cross-site request forgery.
                ["state"] = settings.AccessTokenVerificationString,

                //The ID of the subscription plan to direct the merchant to sign up for, if any.
                //You can provide this parameter with no value to give a merchant the option to cancel an active subscription.
                //["plan_id"] = string.Empty,
            };

            //return generated URL
            return QueryHelpers.AddQueryString(serviceUrl, queryParameters);
        }

        /// <summary>
        /// Exchange the authorization code for an access token
        /// </summary>
        /// <param name="authorizationCode">Authorization code</param>
        /// <param name="storeId">Store identifier for which access token should be obtained</param>
        /// <returns>The asynchronous task whose result contains the Access and refresh tokens</returns>
        public async Task<(string AccessToken, string RefreshToken)> ObtainAccessTokenAsync(string authorizationCode, int storeId)
        {
            return await _squareAuthorizationHttpClient.ObtainAccessTokenAsync(authorizationCode, storeId);
        }

        /// <summary>
        /// Renew the expired access token
        /// </summary>
        /// <param name="storeId">Store identifier for which access token should be updated</param>
        /// <returns>The asynchronous task whose result contains the Access and refresh tokens</returns>
        public async Task<(string AccessToken, string RefreshToken)> RenewAccessTokenAsync(int storeId)
        {
            return await _squareAuthorizationHttpClient.RenewAccessTokenAsync(storeId);
        }

        /// <summary>
        /// Revoke all access tokens
        /// </summary>
        /// <param name="storeId">Store identifier for which access token should be revoked</param>
        /// <returns>The asynchronous task whose result contains the True if tokens were successfully revoked; otherwise false</returns>
        public async Task<bool> RevokeAccessTokensAsync(int storeId)
        {
            return await _squareAuthorizationHttpClient.RevokeAccessTokensAsync(storeId);
        }

        #endregion

        #region Order workflow

        /// <summary>
        /// Get the order
        /// </summary>
        /// <param name="squareOrderId">The square order identifier</param>
        /// <param name="client">The square client identifier</param>
        /// <returns>The asynchronous task whose result contains the order </returns>
        public async Task<Order> GetOrderAsync(string squareOrderId, ISquareClient client = null)
        {
            try
            {
                ArgumentNullException.ThrowIfNullOrEmpty(squareOrderId);

                client ??= await CreateSquareClientAsync();

                var response = await client.OrdersApi.RetrieveOrderAsync(squareOrderId);
                if (response == null)
                    throw new NopException("No service response while getting the order from square payment.");

                ThrowErrorsIfExists(response.Errors);

                return response.Order;
            }
            catch (Exception exception)
            {
                await CatchExceptionAsync(exception);

                return null;
            }
        }

        /// <summary>
        /// Creates a order
        /// </summary>
        /// <param name="order">The order</param>
        /// <param name="orderModel">The square order model</param>
        /// <param name="squareCustomerId">The square customer identifier</param>
        /// <param name="client">The square client identifier</param>
        /// <param name="locationId">The square payment location identifier</param>
        /// <returns>The asynchronous task whose result contains the order and error</returns>
        public async Task<(Order order, string Error)> CreateOrderAsync(Nop.Core.Domain.Orders.Order order,
            SquareOrderModel orderModel,
            string squareCustomerId,
            ISquareClient client = null,
            string locationId = null)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(order);
                ArgumentNullException.ThrowIfNullOrEmpty(squareCustomerId);

                if (order.PaymentStatus == PaymentStatus.Paid ||
                    order.PaymentStatus == PaymentStatus.PartiallyPaid)
                {
                    throw new NopException("Can't create an invoice for a paid and/or partially paid order.");
                }

                orderModel ??= new SquareOrderModel();

                client ??= await CreateSquareClientAsync();

                if (string.IsNullOrWhiteSpace(locationId))
                {
                    var location = await GetSelectedActiveLocationAsync(order.StoreId)
                        ?? throw new NopException("Location is a required parameter for payment requests");

                    locationId = location.Id;
                }

                List<OrderLineItem> lineItems = new();

                if (order.IsChildOrder)
                {
                    var logisticsAccessorialService = EngineContext.Current.Resolve<ILogisticsAccessorialService>();

                    var accessorials = await logisticsAccessorialService.GetAllLogisticsAccessorialsAsync(order.LeadId);

                    var subOrderAmount = accessorials?.Sum(a => a.AR) ?? decimal.Zero;

                    lineItems.Add(new OrderLineItem.Builder("1")
                        .BasePriceMoney(new Money.Builder()
                                .Amount(await SquarePaymentHelper.DollarsToCentsAsync(subOrderAmount))
                                .Currency(SquarePaymentDefaults.CurrencyCode)
                                .Build())
                        .ItemType(SquarePaymentDefaults.Order.ITEM_TYPE_ITEM)
                        .Name(SquarePaymentDefaults.Order.TotalCost_LineItem)
                        .Build());
                }
                else if (orderModel.HasFullAmount)
                {
                    orderModel.Amount = order.Price;

                    var financeDetails = await _orderService.GetParentOrderFinanceDetailsAsync(order);

                    if (financeDetails != null)
                    {
                        orderModel.Amount = financeDetails.CustomerPrice;

                        _parentOrderFinanceDetails.Add(order.Id, financeDetails);
                    }

                    lineItems.Add(new OrderLineItem.Builder("1")
                        .BasePriceMoney(new Money.Builder()
                                .Amount(await SquarePaymentHelper.DollarsToCentsAsync(orderModel.Amount))
                                .Currency(SquarePaymentDefaults.CurrencyCode)
                                .Build())
                        .ItemType(SquarePaymentDefaults.Order.ITEM_TYPE_ITEM)
                        .Name(SquarePaymentDefaults.Order.TotalCost_LineItem)
                        .Build());
                }
                else if (orderModel.HasDepositAmount)
                {
                    var price = order.Price;
                    var carrierPay = order.CarrierPay;

                    var financeDetails = await _orderService.GetParentOrderFinanceDetailsAsync(order);

                    if (financeDetails != null)
                    {
                        price = financeDetails.CustomerPrice;
                        carrierPay = financeDetails.CarrierPay;

                        _parentOrderFinanceDetails.Add(order.Id, financeDetails);
                    }

                    orderModel.Amount = price - carrierPay;

                    lineItems.Add(new OrderLineItem.Builder("1")
                        .BasePriceMoney(new Money.Builder()
                                .Amount(await SquarePaymentHelper.DollarsToCentsAsync(orderModel.Amount))
                                .Currency(SquarePaymentDefaults.CurrencyCode)
                                .Build())
                        .ItemType(SquarePaymentDefaults.Order.ITEM_TYPE_ITEM)
                        .Name(SquarePaymentDefaults.Order.Deposit_LineItem)
                        .Build());
                }

                if (orderModel.HasAdditionalAmount)
                {
                    foreach (var lineItem in orderModel.LineItems.Where(li => !string.IsNullOrWhiteSpace(li.Name)))
                    {
                        lineItems.Add(new OrderLineItem.Builder("1")
                        .BasePriceMoney(new Money.Builder()
                                .Amount(await SquarePaymentHelper.DollarsToCentsAsync(lineItem.Amount))
                                .Currency(SquarePaymentDefaults.CurrencyCode)
                                .Build())
                        .ItemType(SquarePaymentDefaults.Order.ITEM_TYPE_ITEM)
                        .Name(lineItem.Name.Trim())
                        .Build());
                    }
                }

                if (lineItems.Any(l => l.BasePriceMoney?.Amount <= 0))
                {
                    throw new NopException("Price must have a positive amount");
                }

                List<OrderServiceCharge> orderServiceCharge = new()
                {
                    new OrderServiceCharge.Builder()
                    .CalculationPhase(SquarePaymentDefaults.Order.SERVICECHARGE_CALCULATIONPHASE_SUBTOTALPHASE)
                    .Percentage("3.3")
                    .Name(SquarePaymentDefaults.Order.ProcessingFee_ServiceCharge_LineItem)
                    .Build()
                };

                var squareOrderModel = new Order(locationId: locationId)
                    .ToBuilder()
                    .CustomerId(squareCustomerId)
                    .LineItems(lineItems)
                    .ServiceCharges(orderServiceCharge)
                    .PricingOptions(new OrderPricingOptions.Builder()
                                .AutoApplyTaxes(false)
                                .AutoApplyDiscounts(false)
                                .Build())
                    .State(SquarePaymentDefaults.Order.STATE_OPEN)
                    .ReferenceId(order.CustomOrderNumber)
                    .Build();

                CreateOrderRequest createOrderRequest = new(order: squareOrderModel, idempotencyKey: Guid.NewGuid().ToString());

                var response = await client.OrdersApi.CreateOrderAsync(createOrderRequest)
                    ?? throw new NopException("No service response while creating order from square payment.");

                ThrowErrorsIfExists(response.Errors);

                return (response.Order, null);
            }
            catch (Exception exception)
            {
                return (null, await CatchExceptionAsync(exception));
            }
        }

        #endregion

        #region Invoice workflow

        /// <summary>
        /// Creates the invoice
        /// </summary>
        /// <param name="order">The order</param>
        /// <param name="squareOrderModel">The square order model</param>
        /// <returns>The asynchronous task whose result contains the invoice and/or errors if exist</returns>
        public async Task<(Invoice Invoice, string Error)> CreateInvoiceAsync(Nop.Core.Domain.Orders.Order order,
            SquareOrderModel squareOrderModel)
        {
            try
            {
                _parentOrderFinanceDetails = new();

                ArgumentNullException.ThrowIfNull(order);

                if (order.PaymentStatus == PaymentStatus.Paid ||
                    order.PaymentStatus == PaymentStatus.PartiallyPaid)
                {
                    throw new NopException("Can't create an invoice for a paid and/or partially paid order.");
                }

                squareOrderModel ??= new SquareOrderModel();

                if (string.IsNullOrWhiteSpace(squareOrderModel.InvoiceNumber))
                {
                    squareOrderModel.InvoiceNumber = await _squareOrderMappingService.GenerateInvoiceNumberAsync(order);
                }

                var pickupAddressId = order.PickupAddressId ?? 0;

                if (order.IsChildOrder)
                {
                    var parentOrderMapping = await _orderService.GetLogisticsChildOrderMappingByOrderIdAsync(order.Id);
                    var parentOrder = await _orderService.GetOrderByOrderIdAsync(parentOrderMapping?.ParentOrderId ?? 0);
                    pickupAddressId = parentOrder?.PickupAddressId ?? pickupAddressId;
                }

                var originAddress = await _addressService.GetAddressByIdAsync(pickupAddressId)
                    ?? throw new NopException("Failed to get the pickup address.");

                var clientTask = CreateSquareClientAsync();
                var locationTask = GetSelectedActiveLocationAsync(order.StoreId);

                await Task.WhenAll(clientTask, locationTask);

                var client = await clientTask;
                var location = await locationTask
                    ?? throw new NopException("Location is a required parameter for invoice requests");

                //check whether customer exists for current store
                var squareCustomer = await SearchCustomerAsync(originAddress.Email, originAddress.PhoneNumber, originAddress.ContactName, client);

                if (squareCustomer == null)
                {
                    //try to create the new one for current store, if not exists
                    var customerRequestBuilder = new CreateCustomerRequest.Builder()
                        .EmailAddress(originAddress.Email)
                        .GivenName(CommonHelper.EnsureMaximumLength(originAddress.ContactName, 300))
                        .PhoneNumber(originAddress.PhoneNumber);

                    var squareCustomerResponse = await CreateCustomerAsync(customerRequestBuilder.Build(), client);
                    if (!string.IsNullOrWhiteSpace(squareCustomerResponse.Error))
                    {
                        throw new NopException(squareCustomerResponse.Error);
                    }

                    squareCustomer = squareCustomerResponse.customer;
                }

                var squareOrderResponse = await CreateOrderAsync(order, squareOrderModel, squareCustomer.Id, client: client, locationId: location.Id);
                if (!string.IsNullOrWhiteSpace(squareOrderResponse.Error))
                {
                    throw new NopException(squareOrderResponse.Error);
                }

                var squareOrder = squareOrderResponse.order;

                var invoicePaymentRequest = new InvoicePaymentRequest.Builder()
                    .DueDate(DateTime.UtcNow.ToDateFormat(SquarePaymentDefaults.DateTimeFormat))
                    .RequestType(SquarePaymentDefaults.Invoice.REQUEST_TYPE_BALANCE)
                    .TippingEnabled(true)
                    .Build();

                var acceptedPaymentMethods = new InvoiceAcceptedPaymentMethods.Builder()
                    .BankAccount(true)
                    .Card(true)
                    .Build();

                Invoice invoice = new
                (
                    acceptedPaymentMethods: acceptedPaymentMethods,
                    locationId: location.Id,
                    orderId: squareOrder.Id,
                    paymentRequests: new List<InvoicePaymentRequest>() { invoicePaymentRequest },
                    primaryRecipient: new InvoiceRecipient(customerId: squareCustomer.Id),
                    deliveryMethod: SquarePaymentDefaults.Invoice.DELIVERY_METHOD_EMAIL,
                    invoiceNumber: squareOrderModel.InvoiceNumber,
                    storePaymentMethodEnabled: true,
                    title: SquarePaymentDefaults.Invoice.TITLE
                );

                CreateInvoiceRequest createInvoiceRequest = new(invoice: invoice, idempotencyKey: Guid.NewGuid().ToString());

                var response = await client.InvoicesApi.CreateInvoiceAsync(createInvoiceRequest)
                    ?? throw new NopException("No service response while creating the invoice from square payment.");

                ThrowErrorsIfExists(response.Errors);

                var orderSubTotalAmount = squareOrder.LineItems?
                        .Sum(sq => sq.TotalMoney?.Amount ?? decimal.Zero) ?? decimal.Zero;

                _ = Enum.TryParse<InvoiceStatus>(response.Invoice.Status, true, out var status);

                var invoiceAmountTypeId = 0;

                if (order.IsChildOrder)
                {
                    invoiceAmountTypeId = (int)InvoiceAmountType.Full;
                }
                else if (squareOrderModel.HasAdditionalAmount)
                {
                    invoiceAmountTypeId = (int)InvoiceAmountType.Custom;
                }
                else if (squareOrderModel.HasDepositAmount)
                {
                    invoiceAmountTypeId = (int)InvoiceAmountType.Deposit;
                }
                else
                {
                    invoiceAmountTypeId = (int)InvoiceAmountType.Full;
                }

                SquareOrderMapping squareOrderMapping = new()
                {
                    OrderId = order.Id,
                    SquareOrderId = squareOrder.Id,
                    SquareInvoiceId = response.Invoice.Id,
                    SquareInvoiceNumber = response.Invoice.InvoiceNumber,
                    OrderSubTotalAmount = await SquarePaymentHelper.CentsToDollarsAsync(orderSubTotalAmount),
                    InvoiceStatusId = (int)status,
                    InvoiceAmountTypeId = invoiceAmountTypeId
                };

                await _squareOrderMappingService.InsertSquareOrderMappingAsync(squareOrderMapping);

                if (squareOrder.LineItems?.Count > 0)
                {
                    foreach (var orderItem in squareOrder.LineItems)
                    {
                        var amount = orderItem.TotalMoney?.Amount ?? decimal.Zero;

                        await _squareOrderMappingService.InsertSquareOrderItemAsync(new SquareOrderItem()
                        {
                            SquareOrderMappingId = squareOrderMapping.Id,
                            Name = orderItem.Name,
                            Amount = await SquarePaymentHelper.CentsToDollarsAsync(amount)
                        });
                    }
                }

                var (_, publishInvoice, error) = await PublishInvoiceAsync(response.Invoice, client: client);
                if (!string.IsNullOrWhiteSpace(error))
                {
                    throw new NopException(error);
                }

                if (!string.IsNullOrWhiteSpace(publishInvoice?.PublicUrl) &&
                    (squareOrderModel.HasAdditionalAmount || squareOrderModel.HasFullAmount || squareOrderModel.HasDepositAmount))
                {
                    _ = Enum.TryParse<InvoiceStatus>(publishInvoice.Status, true, out status);

                    squareOrderMapping.InvoiceStatusId = (int)status;
                    squareOrderMapping.InvoiceLink = publishInvoice.PublicUrl;
                    await _squareOrderMappingService.UpdateSquareOrderMappingAsync(squareOrderMapping);

                    var squarePaymentMessageService = EngineContext.Current.Resolve<ISquarePaymentMessageService>();
                    var priceFormatter = EngineContext.Current.Resolve<IPriceFormatter>();
                    var logisticsLeadVehicleInfoService = EngineContext.Current.Resolve<ILogisticsLeadVehicleInfoService>();

                    var vehicleList = (await logisticsLeadVehicleInfoService.GetAllLogisticsLeadVehicleInfoAsync(leadId: order.LeadId))
                        .Where(v => !string.IsNullOrWhiteSpace(v.VehicleType))
                        .Select(v => v.VehicleType.ToLowerInvariant().Trim());

                    Dictionary<string, string> dictionary = new()
                    {
                        { "Email", originAddress.Email },
                        { "Name", originAddress.ContactName },
                        { "PaymentUrl", publishInvoice.PublicUrl },
                        { "VehicleTypes", string.Join(", ", vehicleList) },
                    };

                    if (squareOrderModel.HasAdditionalAmount)
                    {
                        dictionary["AdditionalAmount"] = await priceFormatter.FormatPriceAsync(squareOrderMapping.OrderSubTotalAmount, true, false);

                        _ = await squarePaymentMessageService.SendCustomPaymentDetailsEmailNotificationAsync(dictionary);
                    }
                    else if (squareOrderModel.HasDepositAmount)
                    {
                        var price = order.Price;

                        if (_parentOrderFinanceDetails.TryGetValue(order.Id, out var financeDetails))
                        {
                            price = financeDetails.CustomerPrice;
                        }

                        dictionary["Deposit"] = await priceFormatter.FormatPriceAsync(squareOrderMapping.OrderSubTotalAmount, true, false);
                        dictionary["PendingAmount"] = await priceFormatter.FormatPriceAsync(price - squareOrderMapping.OrderSubTotalAmount, true, false);

                        _ = await squarePaymentMessageService.SendDepositPaymentDetailsEmailNotificationAsync(dictionary);
                    }
                    else if (squareOrderModel.HasFullAmount)
                    {
                        dictionary["FullPayment"] = await priceFormatter.FormatPriceAsync(squareOrderMapping.OrderSubTotalAmount, true, false);

                        _ = await squarePaymentMessageService.SendFullPaymentDetailsEmailNotificationAsync(dictionary);
                    }
                }

                return (response.Invoice, null);
            }
            catch (Exception exception)
            {
                return (null, await CatchExceptionAsync(exception));
            }
        }

        /// <summary>
        /// Get the invoice
        /// </summary>
        /// <param name="invoiceId">The square invoice identifier</param>
        /// <returns>The asynchronous task whose result contains the invoice and/or errors if exist</returns>
        public async Task<(Invoice Invoice, string Error)> GetInvoiceAsync(string invoiceId)
        {
            try
            {
                ArgumentNullException.ThrowIfNullOrEmpty(invoiceId);

                var client = await CreateSquareClientAsync();

                var response = await client.InvoicesApi.GetInvoiceAsync(invoiceId);
                if (response == null)
                    throw new NopException("No service response while getting the invoice from square payment.");

                ThrowErrorsIfExists(response.Errors);

                return (response.Invoice, null);
            }
            catch (Exception exception)
            {
                return (null, await CatchExceptionAsync(exception));
            }
        }

        /// <summary>
        /// Update the invoice
        /// </summary>
        /// <param name="invoiceId">The square invoice identifier</param>
        /// <param name="order">The order</param>
        /// <param name="squareInvoice">The square invoice</param>
        /// <returns>The asynchronous task whose result contains the invoice and/or errors if exist</returns>
        public async Task<(Invoice Invoice, string Error)> UpdateInvoiceAsync(string invoiceId, Nop.Core.Domain.Orders.Order order, Invoice squareInvoice)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(order);
                ArgumentNullException.ThrowIfNull(squareInvoice);

                var client = await CreateSquareClientAsync();

                List<InvoicePaymentRequest> paymentRequests = new();

                if (squareInvoice.PaymentRequests?.Count > 0)
                {
                    foreach (var item in squareInvoice.PaymentRequests)
                    {
                        var invoicePaymentRequest = new InvoicePaymentRequest.Builder()
                        .DueDate(DateTime.UtcNow.ToDateFormat(SquarePaymentDefaults.DateTimeFormat))
                        .RequestType(SquarePaymentDefaults.Invoice.REQUEST_TYPE_BALANCE)
                        .TippingEnabled(true)
                        .Uid(item.Uid)
                        .Build();

                        paymentRequests.Add(invoicePaymentRequest);
                    }
                }

                var squareInvoiceBuilder = squareInvoice.ToBuilder();

                squareInvoiceBuilder.Title(SquarePaymentDefaults.Invoice.TITLE);
                squareInvoiceBuilder.PaymentRequests(paymentRequests);
                squareInvoiceBuilder.CustomFields(new List<InvoiceCustomField>() { });

                UpdateInvoiceRequest updateInvoiceRequest = new(invoice: squareInvoiceBuilder.Build(), idempotencyKey: Guid.NewGuid().ToString());

                var response = await client.InvoicesApi.UpdateInvoiceAsync(invoiceId: invoiceId, updateInvoiceRequest);
                if (response == null)
                    throw new NopException("No service response while updating the invoice from square payment.");

                ThrowErrorsIfExists(response.Errors);

                var squareOrderMapping = await _squareOrderMappingService.GetSquareOrderMappingBySquareInvoiceIdAsync(invoiceId);
                if (squareOrderMapping != null &&
                    Enum.TryParse<InvoiceStatus>(response.Invoice.Status, true, out var status))
                {
                    squareOrderMapping.InvoiceStatusId = (int)status;
                    await _squareOrderMappingService.UpdateSquareOrderMappingAsync(squareOrderMapping);
                }

                return (response.Invoice, null);
            }
            catch (Exception exception)
            {
                return (null, await CatchExceptionAsync(exception));
            }
        }

        /// <summary>
        /// Publish invoice
        /// </summary>
        /// <param name="invoice">The square invoice</param>
        /// <param name="client">The square client identifier</param>
        /// <returns>The asynchronous task whose result contains the success result and/or errors if exist</returns>
        public async Task<(bool Success, Invoice Invoice, string Error)> PublishInvoiceAsync(Invoice invoice, ISquareClient client = null)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(invoice);

                client ??= await CreateSquareClientAsync();

                PublishInvoiceRequest publishInvoiceRequest = new
                (
                    version: invoice.Version.HasValue ? invoice.Version.Value : 0,
                    idempotencyKey: Guid.NewGuid().ToString()
                );

                var response = await client.InvoicesApi.PublishInvoiceAsync(invoiceId: invoice.Id, publishInvoiceRequest);
                if (response == null)
                    throw new NopException("No service response while publishing the invoice from square payment.");

                ThrowErrorsIfExists(response.Errors);

                return (true, response.Invoice, null);
            }
            catch (Exception exception)
            {
                return (false, null, await CatchExceptionAsync(exception));
            }
        }

        /// <summary>
        /// Delete invoice
        /// </summary>
        /// <param name="invoiceId">The square invoice identifier</param>
        /// <param name="version">The square invoice version</param>
        /// <returns>The asynchronous task whose result contains the success result and/or errors if exist</returns>
        public async Task<(bool Success, string Error)> DeleteInvoiceAsync(string invoiceId, int version)
        {
            try
            {
                var client = await CreateSquareClientAsync();

                var response = await client.InvoicesApi.DeleteInvoiceAsync(invoiceId: invoiceId, version: version);
                if (response == null)
                    throw new NopException("No service response while deleting the invoice from square payment.");

                ThrowErrorsIfExists(response.Errors);

                var squareOrderMapping = await _squareOrderMappingService.GetSquareOrderMappingBySquareInvoiceIdAsync(invoiceId);

                if (squareOrderMapping != null)
                {
                    await _squareOrderMappingService.DeleteSquareOrderMappingAsync(squareOrderMapping);
                }

                return (true, null);
            }
            catch (Exception exception)
            {
                return (false, await CatchExceptionAsync(exception));
            }
        }

        /// <summary>
        /// Cancel invoice
        /// </summary>
        /// <param name="invoiceId">The square invoice identifier</param>
        /// <param name="version">The square invoice version</param>
        /// <returns>The asynchronous task whose result contains the success result and/or errors if exist</returns>
        public async Task<(bool Success, string Error)> CancelInvoiceAsync(string invoiceId, int version)
        {
            try
            {
                var client = await CreateSquareClientAsync();

                CancelInvoiceRequest cancelInvoiceRequest = new(version: version);

                var response = await client.InvoicesApi.CancelInvoiceAsync(invoiceId: invoiceId, cancelInvoiceRequest);
                if (response == null)
                    throw new NopException("No service response while canceling the invoice from square payment.");

                ThrowErrorsIfExists(response.Errors);

                var squareOrderMapping = await _squareOrderMappingService.GetSquareOrderMappingBySquareInvoiceIdAsync(invoiceId);
                if (squareOrderMapping != null &&
                    Enum.TryParse<InvoiceStatus>(response.Invoice.Status, true, out var status))
                {
                    squareOrderMapping.InvoiceStatusId = (int)status;
                    await _squareOrderMappingService.UpdateSquareOrderMappingAsync(squareOrderMapping);
                }

                return (true, null);
            }
            catch (Exception exception)
            {
                return (false, await CatchExceptionAsync(exception));
            }
        }

        #endregion

        #endregion
    }
}