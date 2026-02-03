using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ARM.Logistics.Payments.Square.Domain;
using ARM.Logistics.Payments.Square.Factories;
using ARM.Logistics.Payments.Square.Models;
using ARM.Logistics.Payments.Square.Models.Invoices;
using ARM.Logistics.Payments.Square.Security;
using ARM.Logistics.Payments.Square.Services;
using ARM.Logistics.Payments.Square.Services.Messages;
using ARM.Logistics.Payments.Square.Services.SquareOrderMappings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json.Linq;
using Nop.Core;
using Nop.Core.Domain.LogisticsFieldsHistory;
using Nop.Core.Domain.Payments;
using Nop.Core.Infrastructure;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.LogisticsFieldsHistory;
using Nop.Services.LogisticsLeads;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Square.Models;
using SquareModel = Square.Models;

namespace ARM.Logistics.Payments.Square.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class PaymentSquareController : BasePaymentController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly SquarePaymentManager _squarePaymentManager;
        private readonly IOrderService _orderService;
        private readonly ISquareOrderMappingService _squareOrderMappingService;
        private readonly IInvoiceModelFactory _invoiceModelFactory;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public PaymentSquareController(ILocalizationService localizationService,
            INotificationService notificationService,
            IPermissionService permissionService,
            ISettingService settingService,
            IStoreContext storeContext,
            SquarePaymentManager squarePaymentManager,
            IOrderService orderService,
            ISquareOrderMappingService squareOrderMappingService,
            IInvoiceModelFactory invoiceModelFactory,
            IWorkContext workContext)
        {
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _settingService = settingService;
            _storeContext = storeContext;
            _squarePaymentManager = squarePaymentManager;
            _orderService = orderService;
            _squareOrderMappingService = squareOrderMappingService;
            _invoiceModelFactory = invoiceModelFactory;
            _workContext = workContext;
        }

        #endregion

        #region Utilities

        #region Order

        private async Task SetOrderHistoryAsync(string message,
            int entityId,
            string oldValue = null,
            string newValue = null,
            int customerId = 0)
        {
            var logisticsFieldHistoryHistoryService = EngineContext.Current.Resolve<ILogisticsFieldHistoryHistoryService>();

            var currentDateTime = DateTime.UtcNow;

            LogisticsFieldHistory logisticsFieldHistory = new()
            {
                Message = message,
                OriginalValue = oldValue,
                ChangeValue = newValue,
                EntityId = entityId,
                EntityType = nameof(FieldHistory.Order),
                Type = (int)FieldHistory.Order,
                CreatedBy = customerId,
                UpdatedBy = customerId,
                CreatedOnUtc = currentDateTime,
                UpdatedOnUtc = currentDateTime
            };

            await logisticsFieldHistoryHistoryService.InsertLogisticsFieldHistoryAsync(logisticsFieldHistory);
        }

        #endregion

        #region Invoice

        /// <summary>
        /// Allow to cancel invoice
        /// </summary>
        /// <param name="invoice">The invoice</param>
        /// <returns>A value indicating whether to cancel invoice</returns>
        private bool AllowToCanceledInvoice(SquareModel.Invoice invoice)
        {
            if (string.IsNullOrWhiteSpace(invoice?.Status))
            {
                return false;
            }

            var comparer = StringComparer.InvariantCultureIgnoreCase;

            if (comparer.Equals(invoice.Status, SquarePaymentDefaults.Invoice.STATUS_SCHEDULED))
            {
                return true;
            }

            if (comparer.Equals(invoice.Status, SquarePaymentDefaults.Invoice.STATUS_UNPAID))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Allow to delete invoice
        /// </summary>
        /// <param name="invoice">The invoice</param>
        /// <returns>A value indicating whether to delete invoice</returns>
        private bool AllowToDeleteInvoice(SquareModel.Invoice invoice)
        {
            return invoice?.Status?.Equals(SquarePaymentDefaults.Invoice.STATUS_DRAFT, StringComparison.InvariantCultureIgnoreCase) ?? false;
        }

        /// <summary>
        /// Delete or cancel invoice asynchronously
        /// </summary>
        /// <param name="invoice">The square invoice</param>
        /// <param name="order">The order</param>
        /// <returns>
        /// A task contains the asynchronous operation
        /// A task contains the result for the cancel or deleted invoice
        /// </returns>
        protected virtual async Task<bool> DeleteOrCancelInvoiceAsync(SquareModel.Invoice invoice, Nop.Core.Domain.Orders.Order order)
        {
            if (string.IsNullOrWhiteSpace(invoice?.Id))
            {
                _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.InvoiceId.NotFound"));

                return false;
            }

            if (order == null)
            {
                _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Order.NotFound"));

                return false;
            }

            var customer = await _workContext.GetCurrentCustomerAsync();
            var version = invoice.Version.HasValue ? invoice.Version.Value : 0;
            var response = (Success: true, Error: string.Empty);

            if (AllowToDeleteInvoice(invoice))
            {
                response = await _squarePaymentManager.DeleteInvoiceAsync(invoice.Id, version);

                if (response.Success)
                {
                    await SetOrderHistoryAsync("Square invoice has been deleted by the system",
                    order.Id,
                    $"Invoice Number - {invoice.InvoiceNumber}",
                    null,
                    customer.Id);
                }
            }

            if (AllowToCanceledInvoice(invoice))
            {
                response = await _squarePaymentManager.CancelInvoiceAsync(invoice.Id, version);

                if (response.Success)
                {
                    await SetOrderHistoryAsync("Square invoice has been canceled by the system",
                    order.Id,
                    $"Invoice Number - {invoice.InvoiceNumber}",
                    null,
                    customer.Id);
                }
            }

            if (!string.IsNullOrWhiteSpace(response.Error))
            {
                _notificationService.ErrorNotification(response.Error);

                return false;
            }

            return response.Success;
        }

        /// <summary>
        /// Redirect to the order details page asynchronously
        /// </summary>
        /// <param name="id">The order identifier</param>
        /// <param name="error">The error</param>
        /// <param name="customMessage">The custom message</param>
        /// <returns>
        /// A task contains the asynchronous operation
        /// A task contains the result for the redirect to the order details page
        /// </returns>
        protected virtual async Task<IActionResult> RedirectToActionAsync(Nop.Core.Domain.Orders.Order order,
            string error,
            string customMessage = null,
            bool isError = false,
            Invoice invoice = null)
        {
            if (order == null || order.Deleted)
            {
                return RedirectToAction("List", "Order");
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                _notificationService.ErrorNotification(error);

                return RedirectToAction("Details", "Order", new { id = order.LeadId });
            }

            customMessage ??= await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Invoice.Create.Success");

            if (isError)
            {
                _notificationService.ErrorNotification(customMessage);
                return RedirectToAction("Details", "Order", new { id = order.LeadId });
            }

            if (!string.IsNullOrWhiteSpace(invoice?.InvoiceNumber))
            {
                var customer = await _workContext.GetCurrentCustomerAsync();

                var message = !string.IsNullOrWhiteSpace(customMessage)
                    ? "Square invoice has been resend by manually."
                    : "Square invoice has been created by manually.";

                await SetOrderHistoryAsync(message,
                    order.Id,
                    null,
                    $"Invoice Number - {invoice.InvoiceNumber}",
                    customer.Id);
            }

            _notificationService.SuccessNotification(customMessage);

            return RedirectToAction("Details", "Order", new { id = order.LeadId });
        }

        /// <summary>
        /// Check if the line item has been updated
        /// </summary>
        /// <param name="invoiceAmount">The invoice amount</param>
        /// <param name="invoiceLineItemNames">The invoice line item names</param>
        /// <param name="squareOrderModel">The square order</param>
        /// <returns>
        /// A task contains the asynchronous operation
        /// A task contains the result for the changes in the line item
        /// </returns>
        protected virtual bool HasUpdatedLineItem(decimal invoiceAmount,
            string invoiceLineItemNames,
            SquareOrderModel squareOrderModel)
        {
            if (squareOrderModel?.LineItems == null)
            {
                return false;
            }

            return invoiceAmount == squareOrderModel.LineItems.Sum(li => li.Amount)
                        && invoiceLineItemNames?.Trim() == string.Join(", ", squareOrderModel.LineItems.Select(li => li.Name?.Trim()));
        }

        #endregion

        #region Webhook

        /// <summary>
        /// Allow to update invoice
        /// </summary>
        /// <param name="eventType">The event type</param>
        /// <returns>A value indicating the webhook event type</returns>
        protected string CheckAndGetWebhookEvent(string eventType)
        {
            if (string.IsNullOrWhiteSpace(eventType))
            {
                return string.Empty;
            }

            var comparer = StringComparer.InvariantCultureIgnoreCase;

            List<string> events = new()
            {
                SquarePaymentDefaults.WebhookEvent.PAYMENT_UPDATED,
                SquarePaymentDefaults.WebhookEvent.REFUND_UPDATED,
                SquarePaymentDefaults.WebhookEvent.INVOICE_CANCELED,
            };

            return events.FirstOrDefault(e => comparer.Equals(e, eventType));
        }

        /// <summary>
        /// Payment updated webhook event
        /// </summary>
        /// <param name="payment">The payment</param>
        /// <returns>A task contains the asynchronous operation</returns>
        protected async Task<IActionResult> PaymentUpdatedWebhookEventAsync(JToken payment)
        {
            if (payment == null)
            {
                return Ok(new { status = "Webhook received successfully" });
            }

            var squareLocationId = (await _squarePaymentManager.GetSelectedActiveLocationAsync(0))?.Id ?? string.Empty;
            var locationId = payment.Value<string>("location_id");
            if (string.IsNullOrWhiteSpace(locationId) || !locationId.Equals(squareLocationId))
            {
                return Ok(new { status = "Webhook received successfully" });
            }

            var paymentId = payment.Value<string>("id");
            if (string.IsNullOrWhiteSpace(paymentId))
            {
                return Ok(new { status = "Webhook received successfully" });
            }

            var comparer = StringComparer.InvariantCultureIgnoreCase;

            var status = payment.Value<string>("status");
            if (!comparer.Equals(status, SquarePaymentDefaults.Status.PAYMENT_COMPLETED))
            {
                return Ok(new { status = "Webhook received successfully" });
            }

            var squareOrderId = payment.Value<string>("order_id");

            var squareOrderMapping = await _squareOrderMappingService.GetSquareOrderMappingBySquareOrderIdAsync(squareOrderId);

            var order = await _orderService.GetOrderByOrderIdAsync(squareOrderMapping?.OrderId ?? 0);
            if (order == null ||
                order.Deleted)
            {
                return Ok(new { status = "Webhook received successfully" });
            }

            var currentDateTime = DateTime.UtcNow;

            squareOrderMapping.InvoiceStatusId = (int)InvoiceStatus.Paid;
            squareOrderMapping.PaidDateOnUtc = currentDateTime;
            await _squareOrderMappingService.UpdateSquareOrderMappingAsync(squareOrderMapping);

            var squareOrder = await _squarePaymentManager.GetOrderAsync(squareOrderId);

            var transactionId = squareOrder?.Tenders?.FirstOrDefault(sq => sq.PaymentId.Equals(paymentId))?.TransactionId ?? null;

            var squareTransactionOrderMapping = await _squareOrderMappingService.GetSquareTransactionOrderMappingByPaymentIdAsync(paymentId);
            if (squareTransactionOrderMapping == null)
            {
                await _squareOrderMappingService.InsertSquareTransactionOrderMappingAsync(new SquareTransactionOrderMapping()
                {
                    OrderId = squareOrderMapping.OrderId,
                    SquareOrderMappingId = squareOrderMapping.Id,
                    PaymentId = paymentId,
                    TransactionId = transactionId,
                    PaidDateTimeOnUtc = !string.IsNullOrWhiteSpace(transactionId) ? currentDateTime : null,
                });
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(transactionId))
                {
                    squareTransactionOrderMapping.TransactionId = transactionId;
                    squareTransactionOrderMapping.PaidDateTimeOnUtc = currentDateTime;

                    await _squareOrderMappingService.UpdateSquareTransactionOrderMappingAsync(squareTransactionOrderMapping);
                }
            }

            if (!string.IsNullOrWhiteSpace(transactionId))
            {
                var paymentStatus = order.PaymentStatus;

                if (order.PaymentStatus != PaymentStatus.Paid && !order.IsChildOrder)
                {
                    order.PaymentStatusId = (int)PaymentStatus.Paid;
                }

                if (order.CODPaymentStatus != PaymentStatus.Paid && order.IsChildOrder)
                {
                    order.CODPaymentStatusId = (int)PaymentStatus.Paid;
                }

                order.PaidDateUtc = currentDateTime;
                await _orderService.UpdateOrderAsync(order);

                await SetOrderHistoryAsync("Payment Status Change By Square App", order.Id,
                    await _localizationService.GetLocalizedEnumAsync(paymentStatus),
                    await _localizationService.GetLocalizedEnumAsync(order.PaymentStatus));

                if (!order.IsChildOrder)
                {
                    var childOrders = await _orderService.GetAllLogisticsChildOrderMappingsAsync(order.Id);
                    if (childOrders?.Count > 0)
                    {
                        var orders = await _orderService.GetOrdersByIdsAsync(childOrders.Select(o => o.ChildOrderId).ToArray());

                        List<Task> orderUpdateTasks = new();
                        List<Task> orderStatusUpdateTasks = new();

                        foreach (var childOrder in childOrders)
                        {
                            var childOrderEntity = orders.FirstOrDefault(o => o.Id == childOrder.ChildOrderId);
                            if (childOrderEntity == null || childOrderEntity.Deleted)
                                continue;
                            var fromChildOrderPaymentStatus = childOrderEntity.PaymentStatus;

                            childOrderEntity.PaymentStatusId = order.PaymentStatusId;

                            if (_orderService.HasCODOrder(childOrderEntity))
                            {
                                childOrderEntity.CODPaymentStatusId = order.PaymentStatusId;
                            }

                            childOrderEntity.PaidDateUtc = order.PaymentStatusId == (int)PaymentStatus.Paid
                                                                ? order.PaidDateUtc
                                                                : null;

                            orderUpdateTasks.Add(_orderService.UpdateOrderAsync(childOrderEntity));

                            if (fromChildOrderPaymentStatus != childOrderEntity.PaymentStatus)
                            {
                                orderStatusUpdateTasks.Add(SetOrderHistoryAsync("Payment Status Change By Square App", childOrder.Id,
                                    await _localizationService.GetLocalizedEnumAsync(fromChildOrderPaymentStatus),
                                    await _localizationService.GetLocalizedEnumAsync(childOrderEntity.PaymentStatus)));
                            }
                        }

                        await Task.WhenAll(orderUpdateTasks);
                        await Task.WhenAll(orderStatusUpdateTasks);
                    }
                }
            }

            return Ok(new { status = "Webhook received successfully" });
        }

        /// <summary>
        /// Refund updated webhook event
        /// </summary>
        /// <param name="refund">The refund</param>
        /// <returns>A task contains the asynchronous operation</returns>
        protected async Task<IActionResult> RefundUpdatedWebhookEventAsync(JToken refund)
        {
            if (refund == null)
            {
                return Ok(new { status = "Webhook received successfully" });
            }

            var squareLocationId = (await _squarePaymentManager.GetSelectedActiveLocationAsync(0))?.Id ?? string.Empty;
            var locationId = refund.Value<string>("location_id");
            if (string.IsNullOrWhiteSpace(locationId) || !locationId.Equals(squareLocationId))
            {
                return Ok(new { status = "Webhook received successfully" });
            }

            var refundId = refund.Value<string>("id");
            if (string.IsNullOrWhiteSpace(refundId))
            {
                return Ok(new { status = "Webhook received successfully" });
            }

            var comparer = StringComparer.InvariantCultureIgnoreCase;

            var status = refund.Value<string>("status");
            if (!comparer.Equals(status, SquarePaymentDefaults.Status.REFUND_COMPLETED))
            {
                return Ok(new { status = "Webhook received successfully" });
            }

            var squareOrderId = refund.Value<string>("order_id");

            var squareOrderMapping = await _squareOrderMappingService.GetSquareOrderMappingBySquareOrderIdAsync(squareOrderId);
            var order = await _orderService.GetOrderByOrderIdAsync(squareOrderMapping?.OrderId ?? 0);
            if (order == null ||
                order.Deleted)
            {
                return Ok(new { status = "Webhook received successfully" });
            }

            var currentDateTime = DateTime.UtcNow;

            squareOrderMapping.InvoiceStatusId = (int)InvoiceStatus.Refunded;
            squareOrderMapping.RefundDateOnUtc = currentDateTime;
            await _squareOrderMappingService.UpdateSquareOrderMappingAsync(squareOrderMapping);

            var squareOrder = await _squarePaymentManager.GetOrderAsync(squareOrderId);

            var paymentId = refund.Value<string>("payment_id");
            var transactionId = squareOrder?.Tenders?.FirstOrDefault(sq => sq.PaymentId.Equals(paymentId))?.TransactionId ?? null;

            var squareTransactionOrderMapping = await _squareOrderMappingService.GetSquareTransactionOrderMappingByPaymentIdAsync(paymentId);
            if (squareTransactionOrderMapping == null)
            {
                await _squareOrderMappingService.InsertSquareTransactionOrderMappingAsync(new SquareTransactionOrderMapping()
                {
                    OrderId = squareOrderMapping.OrderId,
                    SquareOrderMappingId = squareOrderMapping.Id,
                    PaymentId = paymentId,
                    TransactionId = transactionId,
                    RefundDateOnUtc = !string.IsNullOrWhiteSpace(transactionId) ? currentDateTime : null,
                });
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(transactionId))
                {
                    squareTransactionOrderMapping.TransactionId = transactionId;
                    squareTransactionOrderMapping.RefundDateOnUtc = currentDateTime;

                    await _squareOrderMappingService.UpdateSquareTransactionOrderMappingAsync(squareTransactionOrderMapping);
                }
            }

            if (!string.IsNullOrWhiteSpace(transactionId) && order.PaymentStatus != PaymentStatus.Paid)
            {
                var paymentStatus = order.PaymentStatus;

                if (order.PaymentStatus != PaymentStatus.Paid && !order.IsChildOrder)
                {
                    order.PaymentStatusId = (int)PaymentStatus.Pending;
                }

                if (order.CODPaymentStatus != PaymentStatus.Paid && order.IsChildOrder)
                {
                    order.CODPaymentStatusId = (int)PaymentStatus.Pending;
                }

                order.PaidDateUtc = null;
                await _orderService.UpdateOrderAsync(order);

                await SetOrderHistoryAsync("Payment Status Change By Square App", order.Id,
                   await _localizationService.GetLocalizedEnumAsync(paymentStatus),
                   await _localizationService.GetLocalizedEnumAsync(order.PaymentStatus));

                if (!order.IsChildOrder)
                {
                    var childOrders = await _orderService.GetAllLogisticsChildOrderMappingsAsync(order.Id);
                    if (childOrders?.Count > 0)
                    {
                        var orders = await _orderService.GetOrdersByIdsAsync(childOrders.Select(o => o.ChildOrderId).ToArray());

                        List<Task> orderUpdateTasks = new();
                        List<Task> orderStatusUpdateTasks = new();

                        foreach (var childOrder in childOrders)
                        {
                            var childOrderEntity = orders.FirstOrDefault(o => o.Id == childOrder.ChildOrderId);
                            if (childOrderEntity == null || childOrderEntity.Deleted)
                                continue;
                            var fromChildOrderPaymentStatus = childOrderEntity.PaymentStatus;

                            childOrderEntity.PaymentStatusId = order.PaymentStatusId;

                            if (_orderService.HasCODOrder(childOrderEntity))
                            {
                                childOrderEntity.CODPaymentStatusId = order.PaymentStatusId;
                            }

                            childOrderEntity.PaidDateUtc = order.PaidDateUtc;

                            orderUpdateTasks.Add(_orderService.UpdateOrderAsync(childOrderEntity));

                            if (fromChildOrderPaymentStatus != childOrderEntity.PaymentStatus)
                            {
                                orderStatusUpdateTasks.Add(SetOrderHistoryAsync("Payment Status Change By Square App", childOrder.Id,
                                    await _localizationService.GetLocalizedEnumAsync(fromChildOrderPaymentStatus),
                                    await _localizationService.GetLocalizedEnumAsync(childOrderEntity.PaymentStatus)));
                            }
                        }

                        await Task.WhenAll(orderUpdateTasks);
                        await Task.WhenAll(orderStatusUpdateTasks);
                    }
                }
            }

            return Ok(new { status = "Webhook received successfully" });
        }

        /// <summary>
        /// Invoice canceled webhook event
        /// </summary>
        /// <param name="invoice">The invoice</param>
        /// <returns>A task contains the asynchronous operation</returns>
        protected async Task<IActionResult> InvoiceCanceledWebhookEventAsync(JToken invoice)
        {
            if (invoice == null)
            {
                return Ok(new { status = "Webhook received successfully" });
            }

            var squareLocationId = (await _squarePaymentManager.GetSelectedActiveLocationAsync(0))?.Id ?? string.Empty;
            var locationId = invoice.Value<string>("location_id");
            if (string.IsNullOrWhiteSpace(locationId) || !locationId.Equals(squareLocationId))
            {
                return Ok(new { status = "Webhook received successfully" });
            }

            var invoiceId = invoice.Value<string>("id");
            if (string.IsNullOrWhiteSpace(invoiceId))
            {
                return Ok(new { status = "Webhook received successfully" });
            }

            var status = invoice.Value<string>("status");

            var squareOrderMapping = await _squareOrderMappingService.GetSquareOrderMappingBySquareInvoiceIdAsync(invoiceId);

            if (squareOrderMapping == null ||
                !Enum.TryParse<InvoiceStatus>(status, true, out var invoiceStatus))
            {
                return Ok(new { status = "Webhook received successfully" });
            }

            squareOrderMapping.InvoiceStatusId = (int)invoiceStatus;
            await _squareOrderMappingService.UpdateSquareOrderMappingAsync(squareOrderMapping);

            return Ok(new { status = "Webhook received successfully" });
        }

        #endregion

        #endregion

        #region Methods

        #region Admin actions

        #region Admin plugin configuration

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var settings = await _settingService.LoadSettingAsync<SquarePaymentSettings>(storeScope);

            //prepare model
            var model = new ConfigurationModel
            {
                ApplicationSecret = settings.ApplicationSecret,
                UseSandbox = settings.UseSandbox,
                Use3ds = settings.Use3ds,
                TransactionModeId = (int)settings.TransactionMode,
                LocationId = settings.LocationId,
                AdditionalFee = settings.AdditionalFee,
                AdditionalFeePercentage = settings.AdditionalFeePercentage,
                AutomaticSentInvoice = settings.AutomaticSentInvoice,
                ActiveStoreScopeConfiguration = storeScope
            };
            if (model.UseSandbox)
            {
                model.SandboxApplicationId = settings.ApplicationId;
                model.SandboxAccessToken = settings.AccessToken;
            }
            else
            {
                model.ApplicationId = settings.ApplicationId;
                model.AccessToken = settings.AccessToken;
            }

            if (storeScope > 0)
            {
                model.UseSandbox_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.UseSandbox, storeScope);
                model.Use3ds_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.Use3ds, storeScope);
                model.TransactionModeId_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.TransactionMode, storeScope);
                model.LocationId_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.LocationId, storeScope);
                model.AdditionalFee_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.AdditionalFee, storeScope);
                model.AutomaticSentInvoice_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.AutomaticSentInvoice, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.AdditionalFeePercentage, storeScope);
            }

            //prepare business locations, every payment a merchant processes is associated with one of these locations
            if (!string.IsNullOrWhiteSpace(settings.AccessToken))
            {
                model.Locations = (await _squarePaymentManager.GetActiveLocationsAsync(storeScope)).Select(location =>
                {
                    var name = location.BusinessName;
                    if (!location.Name.Equals(location.BusinessName))
                        name = $"{name} ({location.Name})";
                    return new SelectListItem { Text = name, Value = location.Id };
                }).ToList();
                if (model.Locations.Any())
                {
                    var selectLocationText = await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Fields.Location.Select");
                    model.Locations.Insert(0, new SelectListItem { Text = selectLocationText, Value = "0" });
                }
            }

            //add the special item for 'there are no location' with value 0
            if (!model.Locations.Any())
            {
                var noLocationText = await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Fields.Location.NotExist");
                model.Locations.Add(new SelectListItem { Text = noLocationText, Value = "0" });
            }

            //warn admin that the location is a required parameter
            if (string.IsNullOrWhiteSpace(settings.LocationId) || settings.LocationId.Equals("0"))
                _notificationService.WarningNotification(await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Fields.Location.Hint"));

            //migrate to using refresh tokens
            if (!settings.UseSandbox && settings.RefreshToken == Guid.Empty.ToString())
            {
                var migrateMessage = $"Your access token is deprecated.<br /> " +
                    $"1. In the <a href=\"https://squareup.com\" target=\"_blank\">" +
                    $"Square Developer Portal" +
                    $"</a> make sure your application is on Connect API version 2019-03-13 or later.<br /> " +
                    $"2. On this page click 'Obtain access token' below.<br />";
                _notificationService.ErrorNotification(migrateMessage, encode: false);
            }

            return View("~/Plugins/Logistics.Payments.Square/Views/Configure.cshtml", model);
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("save")]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return RedirectToAction(nameof(this.Configure));

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var settings = await _settingService.LoadSettingAsync<SquarePaymentSettings>(storeScope);

            //save settings
            if (model.UseSandbox)
            {
                settings.ApplicationId = model.SandboxApplicationId;
                settings.ApplicationSecret = string.Empty;
                settings.AccessToken = model.SandboxAccessToken?.Trim() ?? string.Empty;
            }
            else
            {
                settings.ApplicationId = model.ApplicationId;
                settings.ApplicationSecret = model.ApplicationSecret?.Trim() ?? string.Empty;

                if (settings.UseSandbox)
                    settings.AccessToken = string.Empty;
            }

            settings.LocationId = model.UseSandbox == settings.UseSandbox ? model.LocationId : string.Empty;
            settings.UseSandbox = model.UseSandbox;
            settings.Use3ds = model.Use3ds;
            settings.TransactionMode = (TransactionMode)model.TransactionModeId;
            settings.AdditionalFee = model.AdditionalFee;
            settings.AutomaticSentInvoice = model.AutomaticSentInvoice;
            settings.AdditionalFeePercentage = model.AdditionalFeePercentage;

            await _settingService.SaveSettingAsync(settings, x => x.ApplicationId, storeScope, false);
            await _settingService.SaveSettingAsync(settings, x => x.ApplicationSecret, storeScope, false);
            await _settingService.SaveSettingAsync(settings, x => x.AccessToken, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.UseSandbox, model.UseSandbox_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.Use3ds, model.Use3ds_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.TransactionMode, model.TransactionModeId_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.LocationId, model.LocationId_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.AutomaticSentInvoice, model.AutomaticSentInvoice_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);

            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return RedirectToAction(nameof(this.Configure));
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("obtainAccessToken")]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> ObtainAccessToken(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var settings = await _settingService.LoadSettingAsync<SquarePaymentSettings>(storeScope);

            //create new verification string
            settings.AccessTokenVerificationString = Guid.NewGuid().ToString();
            await _settingService.SaveSettingAsync(settings, x => settings.AccessTokenVerificationString, storeScope);

            //get the URL to directs a Square merchant's web browser
            var redirectUrl = await _squarePaymentManager.GenerateAuthorizeUrlAsync(storeScope);

            return Redirect(redirectUrl);
        }

        public async Task<IActionResult> AccessTokenCallback()
        {
            //load settings for a current store
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var settings = await _settingService.LoadSettingAsync<SquarePaymentSettings>(storeScope);

            //handle access token callback
            try
            {
                if (string.IsNullOrWhiteSpace(settings.ApplicationId) || string.IsNullOrWhiteSpace(settings.ApplicationSecret))
                    throw new NopException("Plugin is not configured");

                //check whether there are errors in the request
                if (Request.Query.TryGetValue("error", out var error) && Request.Query.TryGetValue("error_description", out var errorDescription))
                    throw new NopException($"{error} - {errorDescription}");

                //validate verification string
                if (!Request.Query.TryGetValue("state", out var verificationString) || !verificationString.Equals(settings.AccessTokenVerificationString))
                    throw new NopException("The verification string did not pass the validation");

                //check whether there is an authorization code in the request
                if (!Request.Query.TryGetValue("code", out var authorizationCode))
                    throw new NopException("No service response");

                //exchange the authorization code for an access token
                var (accessToken, refreshToken) = await _squarePaymentManager.ObtainAccessTokenAsync(authorizationCode, storeScope);
                if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
                    throw new NopException("No service response");

                //if access token successfully received, save it for the further usage
                settings.AccessToken = accessToken;
                settings.RefreshToken = refreshToken;

                await _settingService.SaveSettingAsync(settings, x => x.AccessToken, storeScope, false);
                await _settingService.SaveSettingAsync(settings, x => x.RefreshToken, storeScope, false);

                await _settingService.ClearCacheAsync();

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.ObtainAccessToken.Success"));
            }
            catch (Exception exception)
            {
                //display errors
                _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.ObtainAccessToken.Error"));
                if (!string.IsNullOrWhiteSpace(exception.Message))
                    _notificationService.ErrorNotification(exception.Message);
            }

            return RedirectToAction("Configure", "PaymentSquare", new { area = AreaNames.Admin });
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("revokeAccessTokens")]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> RevokeAccessTokens(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var settings = await _settingService.LoadSettingAsync<SquarePaymentSettings>(storeScope);

            try
            {
                //try to revoke all access tokens
                var successfullyRevoked = await _squarePaymentManager.RevokeAccessTokensAsync(storeScope);
                if (!successfullyRevoked)
                    throw new NopException("Tokens were not revoked");

                //if access token successfully revoked, delete it from the settings
                settings.AccessToken = string.Empty;
                await _settingService.SaveSettingAsync(settings, x => x.AccessToken, storeScope);

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.RevokeAccessTokens.Success"));
            }
            catch (Exception exception)
            {
                var error = await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.RevokeAccessTokens.Error");

                if (!string.IsNullOrWhiteSpace(exception.Message))
                {
                    error = $"{error} - {exception.Message}";
                }

                _notificationService.ErrorNotification(error);
            }

            return await Configure();
        }

        #endregion

        #region Order invoice

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> InvoiceHistory(InvoiceSearchModel searchModel)
        {
            var model = await _invoiceModelFactory.PrepareInvoiceListModelAsync(searchModel);

            return Json(model);
        }

        #endregion

        #endregion

        #region Invoice workflow

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> CreateDepositInvoice(int id)
        {
            if (!await _permissionService.AuthorizeAsync(SquarePermissionProvider.ManageSquareInvoice))
            {
                return AccessDeniedView();
            }

            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return RedirectToAction("List", "Order");
            }

            SquareOrderModel squareOrderModel = new()
            {
                HasDepositAmount = !order.IsChildOrder,
                HasFullAmount = order.IsChildOrder,
            };

            var response = await _squarePaymentManager.CreateInvoiceAsync(order, squareOrderModel);

            return await RedirectToActionAsync(order, response.Error, invoice: response.Invoice);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> SendInvoice(int id, SquareOrderModel squareOrderModel)
        {
            if (!await _permissionService.AuthorizeAsync(SquarePermissionProvider.ManageSquareInvoice))
            {
                return AccessDeniedView();
            }

            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null || order.Deleted)
            {
                return RedirectToAction("List", "Order");
            }

            var squareOrderMapping = await _squareOrderMappingService.GetUnPaidInvoiceByOrderIdAsync(order.Id);

            if (string.IsNullOrWhiteSpace(squareOrderMapping?.SquareInvoiceId))
            {
                if (squareOrderModel.HasAdditionalAmount)
                {
                    squareOrderModel.HasDepositAmount = false;
                    squareOrderModel.HasFullAmount = false;

                    var response = await _squarePaymentManager.CreateInvoiceAsync(order, squareOrderModel);

                    return await RedirectToActionAsync(order, response.Error, invoice: response.Invoice);
                }

                if (squareOrderModel.HasFullAmount)
                {
                    squareOrderModel.HasDepositAmount = false;
                    squareOrderModel.LineItems = null;

                    var response = await _squarePaymentManager.CreateInvoiceAsync(order, squareOrderModel);

                    return await RedirectToActionAsync(order, response.Error, invoice: response.Invoice);
                }

                if (!order.IsChildOrder)
                {
                    return await RedirectToActionAsync(order, await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Common.Fail"), isError: true);
                }
            }

            var hasAnyInvoicePaid = await _squareOrderMappingService.HasPaidSquareOrderMappingByOrderIdAsync(order.Id);
            var invoiceAmount = decimal.Zero;
            var invoiceLineItemNames = string.Empty;
            var squareOrder = await _squareOrderMappingService.GetAllSquareOrderItemsAsync(squareOrderMapping.Id);

            if (squareOrder?.Count > 0)
            {
                var orderLineItems = !hasAnyInvoicePaid
                                    ? squareOrder.Skip(1)
                                    : squareOrder;

                invoiceAmount = orderLineItems.Sum(sq => sq.Amount);
                invoiceLineItemNames = string.Join(", ", orderLineItems.Select(li => li.Name));
            }

            var invoiceResponse = await _squarePaymentManager.GetInvoiceAsync(squareOrderMapping.SquareInvoiceId);
            if (!string.IsNullOrWhiteSpace(invoiceResponse.Error))
            {
                return await RedirectToActionAsync(order, invoiceResponse.Error);
            }

            if (string.IsNullOrWhiteSpace(invoiceResponse.Invoice.Status))
            {
                return await RedirectToActionAsync(order, await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Invoice.Status.NotFound"), isError: true);
            }

            if (squareOrderMapping.InvoiceAmountType == InvoiceAmountType.Deposit)
            {
                if (squareOrderModel.HasFullAmount)
                {
                    if (await DeleteOrCancelInvoiceAsync(invoiceResponse.Invoice, order))
                    {
                        squareOrderModel.HasDepositAmount = false;
                        squareOrderModel.LineItems = null;

                        var response = await _squarePaymentManager.CreateInvoiceAsync(order, squareOrderModel);

                        return await RedirectToActionAsync(order, response.Error, invoice: response.Invoice);
                    }
                }
                else if (squareOrderModel.HasAdditionalAmount)
                {
                    if (await DeleteOrCancelInvoiceAsync(invoiceResponse.Invoice, order))
                    {
                        squareOrderModel.HasDepositAmount = true;

                        var response = await _squarePaymentManager.CreateInvoiceAsync(order, squareOrderModel);

                        return await RedirectToActionAsync(order, response.Error, invoice: response.Invoice);
                    }
                }
                else
                {
                    return await RedirectToActionAsync(order, await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Common.Fail"), isError: true);
                }
            }
            else if (squareOrderMapping.InvoiceAmountType == InvoiceAmountType.Full)
            {
                if (squareOrderModel.HasFullAmount && string.IsNullOrWhiteSpace(invoiceLineItemNames))
                {
                    var response = await _squarePaymentManager.UpdateInvoiceAsync(squareOrderMapping.SquareInvoiceId, order, invoiceResponse.Invoice);

                    return await RedirectToActionAsync(order, response.Error, await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Invoice.Update.Success"), invoice: response.Invoice);
                }
                else if (squareOrderModel.HasFullAmount && !string.IsNullOrWhiteSpace(invoiceLineItemNames))
                {
                    if (await DeleteOrCancelInvoiceAsync(invoiceResponse.Invoice, order))
                    {
                        squareOrderModel.HasDepositAmount = false;
                        squareOrderModel.LineItems = null;

                        var response = await _squarePaymentManager.CreateInvoiceAsync(order, squareOrderModel);

                        return await RedirectToActionAsync(order, response.Error, invoice: response.Invoice);
                    }
                }
                else if (squareOrderModel.HasAdditionalAmount)
                {
                    if (await DeleteOrCancelInvoiceAsync(invoiceResponse.Invoice, order))
                    {
                        squareOrderModel.HasDepositAmount = false;
                        squareOrderModel.HasFullAmount = true;

                        var response = await _squarePaymentManager.CreateInvoiceAsync(order, squareOrderModel);

                        return await RedirectToActionAsync(order, response.Error, invoice: response.Invoice);
                    }
                }
                else
                {
                    return await RedirectToActionAsync(order, await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Common.Fail"), isError: true);
                }
            }
            else
            {
                if (squareOrderModel.HasAdditionalAmount)
                {
                    if (HasUpdatedLineItem(invoiceAmount, invoiceLineItemNames, squareOrderModel))
                    {
                        var response = await _squarePaymentManager.UpdateInvoiceAsync(squareOrderMapping.SquareInvoiceId, order, invoiceResponse.Invoice);

                        return await RedirectToActionAsync(order, response.Error, await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Invoice.Update.Success"), invoice: response.Invoice);
                    }
                    else
                    {
                        if (await DeleteOrCancelInvoiceAsync(invoiceResponse.Invoice, order))
                        {
                            if (hasAnyInvoicePaid)
                            {
                                squareOrderModel.HasDepositAmount = false;
                                squareOrderModel.HasFullAmount = false;
                            }
                            else
                            {
                                var hasFullAmountInvoice = await _squareOrderMappingService.GetSquareOrderMappingByOrderIdAsync(order.Id, (int)InvoiceAmountType.Full) != null;

                                squareOrderModel.HasDepositAmount = !hasFullAmountInvoice;
                                squareOrderModel.HasFullAmount = hasFullAmountInvoice;
                            }

                            var response = await _squarePaymentManager.CreateInvoiceAsync(order, squareOrderModel);

                            return await RedirectToActionAsync(order, response.Error, invoice: response.Invoice);
                        }
                    }
                }
                else if (squareOrderModel.HasFullAmount)
                {
                    if (await DeleteOrCancelInvoiceAsync(invoiceResponse.Invoice, order))
                    {
                        squareOrderModel.HasDepositAmount = false;
                        squareOrderModel.LineItems = null;

                        var response = await _squarePaymentManager.CreateInvoiceAsync(order, squareOrderModel);

                        return await RedirectToActionAsync(order, response.Error, invoice: response.Invoice);
                    }
                }
                else
                {
                    return await RedirectToActionAsync(order, await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Common.Fail"), isError: true);
                }
            }

            return RedirectToAction("Details", "Order", new { id = id });
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> PublishInvoice(string invoiceId)
        {
            if (!await _permissionService.AuthorizeAsync(SquarePermissionProvider.ManageSquareInvoice))
            {
                return Json(new
                {
                    Status = false,
                    Message = await _localizationService.GetResourceAsync("Admin.AccessDenied.Description")
                });
            }

            var customer = await _workContext.GetCurrentCustomerAsync();
            var squareOrderMapping = await _squareOrderMappingService.GetSquareOrderMappingBySquareInvoiceIdAsync(invoiceId);
            var order = await _orderService.GetOrderByOrderIdAsync(squareOrderMapping?.OrderId ?? 0);

            if (squareOrderMapping == null)
            {
                return Json(new
                {
                    Status = false,
                    Message = await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Invoice.NotFound.FromSystem")
                });
            }

            if (order == null || order.Deleted || order.LeadId <= 0)
            {
                return Json(new
                {
                    Status = false,
                    Message = "Order has been deleted or not found"
                });
            }

            var invoiceResponse = await _squarePaymentManager.GetInvoiceAsync(invoiceId);

            if (!string.IsNullOrWhiteSpace(invoiceResponse.Error))
            {
                return Json(new
                {
                    Status = false,
                    Message = invoiceResponse.Error
                });
            }

            if (invoiceResponse.Invoice == null)
            {
                return Json(new
                {
                    Status = false,
                    Message = await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.InvoiceId.NotFound")
                });
            }

            var response = await _squarePaymentManager.PublishInvoiceAsync(invoiceResponse.Invoice);
            if (!string.IsNullOrWhiteSpace(response.Error))
            {
                return Json(new
                {
                    Status = false,
                    Message = response.Error
                });
            }

            var invoice = response.Invoice;
            if (string.IsNullOrWhiteSpace(invoice?.PublicUrl))
            {
                return Json(new
                {
                    Status = false,
                    Message = await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Invoice.PublicUrl.NotFound")
                });
            }

            _ = Enum.TryParse<InvoiceStatus>(invoice.Status, true, out var status);

            squareOrderMapping.InvoiceStatusId = (int)status;
            squareOrderMapping.InvoiceLink = invoice.PublicUrl;
            await _squareOrderMappingService.UpdateSquareOrderMappingAsync(squareOrderMapping);

            await SetOrderHistoryAsync("Square invoice has been published", order.Id, customerId: customer.Id);

            var addressService = EngineContext.Current.Resolve<IAddressService>();

            var pickupAddressId = order.PickupAddressId ?? 0;
            if (order.IsChildOrder)
            {
                var parentOrderMapping = await _orderService.GetLogisticsChildOrderMappingByOrderIdAsync(order.Id);
                var parentOrder = await _orderService.GetOrderByOrderIdAsync(parentOrderMapping?.ParentOrderId ?? 0);
                pickupAddressId = parentOrder?.PickupAddressId ?? pickupAddressId;
            }

            var originAddress = await addressService.GetAddressByIdAsync(pickupAddressId);
            if (string.IsNullOrWhiteSpace(originAddress?.Email))
            {
                return Json(new
                {
                    Status = true,
                    Message = string.Format(
                     await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Invoice.PublicUrl.Generate"),
                     invoice.InvoiceNumber)
                });
            }

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
                { "PaymentUrl", invoice.PublicUrl },
                { "VehicleTypes", string.Join(", ", vehicleList) },
            };

            if (squareOrderMapping.InvoiceAmountType == InvoiceAmountType.Deposit)
            {
                dictionary["Deposit"] = await priceFormatter.FormatPriceAsync(squareOrderMapping.OrderSubTotalAmount, true, false);
                dictionary["PendingAmount"] = await priceFormatter.FormatPriceAsync(order.Price - squareOrderMapping.OrderSubTotalAmount, true, false);

                _ = await squarePaymentMessageService.SendDepositPaymentDetailsEmailNotificationAsync(dictionary);
            }
            else if (squareOrderMapping.InvoiceAmountType == InvoiceAmountType.Full)
            {
                dictionary["FullPayment"] = await priceFormatter.FormatPriceAsync(squareOrderMapping.OrderSubTotalAmount, true, false);

                _ = await squarePaymentMessageService.SendFullPaymentDetailsEmailNotificationAsync(dictionary);
            }
            else
            {
                dictionary["AdditionalAmount"] = await priceFormatter.FormatPriceAsync(squareOrderMapping.OrderSubTotalAmount, true, false);

                _ = await squarePaymentMessageService.SendCustomPaymentDetailsEmailNotificationAsync(dictionary);
            }

            return Json(new
            {
                Status = true,
                Message = string.Format(
                await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Invoice.PublicUrl.Generate"),
                invoice.InvoiceNumber)
            });
        }

        #endregion

        #region Webhook event configuration

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> ReceiveEvent()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();

                var root = JObject.Parse(body);

                if (!root.ContainsKey("type"))
                {
                    return Ok(new { status = "Webhook received successfully" });
                }

                switch (CheckAndGetWebhookEvent(root.Value<string>("type")))
                {
                    case (SquarePaymentDefaults.WebhookEvent.PAYMENT_UPDATED):
                        return await PaymentUpdatedWebhookEventAsync(root.SelectToken("data.object.payment"));

                    case (SquarePaymentDefaults.WebhookEvent.REFUND_UPDATED):
                        return await RefundUpdatedWebhookEventAsync(root.SelectToken("data.object.refund"));

                    case (SquarePaymentDefaults.WebhookEvent.INVOICE_CANCELED):
                        return await InvoiceCanceledWebhookEventAsync(root.SelectToken("data.object.invoice"));

                    default:
                        return Ok(new { status = "Webhook received successfully" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        #endregion

        #endregion
    }
}