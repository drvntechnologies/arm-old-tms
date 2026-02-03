using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ARM.Logistics.Payments.Square.Domain;
using ARM.Logistics.Payments.Square.Models;
using ARM.Logistics.Payments.Square.Services;
using ARM.Logistics.Payments.Square.Services.SquareOrderMappings;
using Nop.Core;
using Nop.Core.Domain.LogisticsFieldsHistory;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.QuotesStatus;
using Nop.Core.Events;
using Nop.Core.Infrastructure;
using Nop.Services.Events;
using Nop.Services.LogisticsFieldsHistory;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.QuotesStatus;
using Nop.Web.Areas.Admin.Models.Orders;

namespace ARM.Logistics.Payments.Square.Events
{
    public class SquarePaymentEventConsumer : IConsumer<SquareInvoiceEvent>,
        IConsumer<EntityDeletedEvent<Order>>,
        IConsumer<AdditionalTokensAddedEvent>,
        IConsumer<CommissionReportOrderEvent>,
        IConsumer<OrderModel>
    {
        #region Fields

        private readonly SquarePaymentManager _squarePaymentManager;
        private readonly ILogisticsStatusService _logisticsStatusService;
        private readonly ISquareOrderMappingService _squareOrderMappingService;
        private readonly SquarePaymentSettings _squarePaymentSettings;

        #endregion

        #region Ctor

        public SquarePaymentEventConsumer(SquarePaymentManager squarePaymentManager,
            ILogisticsStatusService logisticsStatusService,
            ISquareOrderMappingService squareOrderMappingService,
            SquarePaymentSettings squarePaymentSettings)
        {
            _squarePaymentManager = squarePaymentManager;
            _logisticsStatusService = logisticsStatusService;
            _squareOrderMappingService = squareOrderMappingService;
            _squarePaymentSettings = squarePaymentSettings;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Delete invoice
        /// </summary>
        /// <param name="invoiceId">The invoice identifier</param>
        /// <param name="order">The order</param>
        /// <returns>
        /// A task that represents the async operation
        /// THe result contains the status for the invoice has cancel or deleted
        /// </returns>
        private async Task<bool> DeleteInvoiceAsync(string invoiceId)
        {
            if (string.IsNullOrWhiteSpace(invoiceId))
            {
                return false;
            }

            var (Invoice, _) = await _squarePaymentManager.GetInvoiceAsync(invoiceId);

            if (!Enum.TryParse<InvoiceStatus>(Invoice?.Status, true, out var invoiceStatus))
            {
                return false;
            }

            var isSuccess = false;
            var version = Invoice.Version ?? 0;

            if (invoiceStatus == InvoiceStatus.Draft)
            {
                (isSuccess, _) = await _squarePaymentManager.DeleteInvoiceAsync(invoiceId, version);
            }
            else
            {
                if (invoiceStatus == InvoiceStatus.Scheduled ||
                    invoiceStatus == InvoiceStatus.UnPaid)
                {
                    (isSuccess, _) = await _squarePaymentManager.CancelInvoiceAsync(invoiceId, version);
                }
            }

            return isSuccess;
        }

        #endregion

        #region Methods

        public async Task HandleEventAsync(SquareInvoiceEvent squareInvoiceEvent)
        {
            var order = squareInvoiceEvent.Order;

            var comparer = StringComparer.InvariantCultureIgnoreCase;
            var status = (await _logisticsStatusService.GetLogisticsStatusByIdAsync(order?.StatusId ?? 0))
                        ?.Title;

            if (!_squarePaymentSettings.AutomaticSentInvoice ||
                !comparer.Equals(status, LogisticsStatusDefaults.Order.Covered) ||
                !comparer.Equals(order.PaymentOption, LogisticsDefaults.PaymentOption.COD) ||
                string.IsNullOrWhiteSpace(order.PaymentOption) ||
                order.PaymentStatus == PaymentStatus.Paid ||
                order.PaymentStatus == PaymentStatus.PartiallyPaid)
            {
                return;
            }

            if (order.IsChildOrder)
            {
                var orderService = EngineContext.Current.Resolve<IOrderService>();

                var subOrderMapping = await orderService.GetLogisticsChildOrderMappingByOrderIdAsync(order.Id);
                var parentOrder = await orderService.GetOrderByOrderIdAsync(subOrderMapping?.ParentOrderId ?? 0);

                if (parentOrder == null ||
                    parentOrder.Deleted ||
                    !comparer.Equals(parentOrder.PaymentOption, LogisticsDefaults.PaymentOption.Billable) ||
                    parentOrder.PaymentStatus == PaymentStatus.Paid ||
                    parentOrder.PaymentStatus == PaymentStatus.PartiallyPaid)
                {
                    return;
                }
            }

            var hasAnyInvoiceGenerated = await _squareOrderMappingService.HasAnySquareOrderMappingsByOrderIdAsync(order.Id);
            var squareOrderMapping = await _squareOrderMappingService.GetSquareOrderMappingByOrderIdAsync(order.Id, (int)InvoiceAmountType.Deposit);

            if (squareOrderMapping != null || hasAnyInvoiceGenerated)
            {
                return;
            }

            SquareOrderModel squareOrderModel = new()
            {
                HasDepositAmount = !order.IsChildOrder,
                HasFullAmount = order.IsChildOrder
            };

            var response = await _squarePaymentManager.CreateInvoiceAsync(order, squareOrderModel);
            squareInvoiceEvent.Status = response.Invoice != null;

            if (!string.IsNullOrWhiteSpace(response.Error))
            {
                var notificationService = EngineContext.Current.Resolve<INotificationService>();

                notificationService.ErrorNotification(response.Error);
            }

            if (!string.IsNullOrWhiteSpace(response.Invoice?.InvoiceNumber))
            {
                var workContext = EngineContext.Current.Resolve<IWorkContext>();
                var logisticsFieldHistoryHistoryService = EngineContext.Current.Resolve<ILogisticsFieldHistoryHistoryService>();

                var customer = await workContext.GetCurrentCustomerAsync();
                var currentDateTime = DateTime.UtcNow;

                LogisticsFieldHistory logisticsFieldHistory = new()
                {
                    Message = "Square invoice has been created by the system.",
                    OriginalValue = null,
                    ChangeValue = $"Invoice Number - {response.Invoice.InvoiceNumber}",
                    EntityId = order.Id,
                    EntityType = nameof(FieldHistory.Order),
                    Type = (int)FieldHistory.Order,
                    CreatedBy = customer.Id,
                    UpdatedBy = customer.Id,
                    CreatedOnUtc = currentDateTime,
                    UpdatedOnUtc = currentDateTime
                };

                await logisticsFieldHistoryHistoryService.InsertLogisticsFieldHistoryAsync(logisticsFieldHistory);
            }
        }

        public async Task HandleEventAsync(EntityDeletedEvent<Order> eventMessage)
        {
            var order = eventMessage.Entity;

            var squareOrderMapping = await _squareOrderMappingService.GetUnPaidInvoiceByOrderIdAsync(order?.Id ?? 0);
            if (!await DeleteInvoiceAsync(squareOrderMapping?.SquareInvoiceId))
            {
                return;
            }

            var workContext = EngineContext.Current.Resolve<IWorkContext>();
            var logisticsFieldHistoryHistoryService = EngineContext.Current.Resolve<ILogisticsFieldHistoryHistoryService>();

            var customer = await workContext.GetCurrentCustomerAsync();
            var currentDateTime = DateTime.UtcNow;

            LogisticsFieldHistory logisticsFieldHistory = new()
            {
                Message = "Square invoice has been deleted by the system",
                OriginalValue = null,
                ChangeValue = $"Invoice Number - {squareOrderMapping.SquareInvoiceNumber}",
                EntityId = order.Id,
                EntityType = nameof(FieldHistory.Order),
                Type = (int)FieldHistory.Order,
                CreatedBy = customer.Id,
                UpdatedBy = customer.Id,
                CreatedOnUtc = currentDateTime,
                UpdatedOnUtc = currentDateTime
            };

            await logisticsFieldHistoryHistoryService.InsertLogisticsFieldHistoryAsync(logisticsFieldHistory);
        }

        public async Task HandleEventAsync(AdditionalTokensAddedEvent eventMessage)
        {
            if (eventMessage == null)
            {
                return;
            }

            var webHelper = EngineContext.Current.Resolve<IWebHelper>();
            var url = webHelper.GetThisPageUrl(false);

            if (string.IsNullOrWhiteSpace(url) ||
                !url.Contains("MessageTemplate", StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            Uri uri = new Uri(url);
            string messageTemplateId = uri.Segments?.LastOrDefault();

            if (messageTemplateId == null || !int.TryParse(messageTemplateId, out int id))
            {
                return;
            }

            var messageTemplateService = EngineContext.Current.Resolve<IMessageTemplateService>();
            var messageTemplate = await messageTemplateService.GetMessageTemplateByIdAsync(id);

            if (messageTemplate == null)
            {
                return;
            }

            var comparer = StringComparer.InvariantCultureIgnoreCase;

            if (comparer.Equals(messageTemplate.Name, SquarePaymentDefaults.MessageTemplate.DepositPaymentDetails))
            {
                eventMessage.AddTokens("%SquarePayment.PaymentURL%",
                    "%SquarePayment.Deposit%",
                    "%SquarePayment.PendingAmount%",
                    "%SquarePayment.VehicleTypes%",
                    "%Store.Name%",
                    "%Store.URL%",
                    "%Store.Email%",
                    "%Store.CompanyName%",
                    "%Store.CompanyAddress%",
                    "%Store.CompanyPhoneNumber%",
                    "%Store.Logo%",
                    "%Store.SupportEmail%");

                return;
            }

            if (comparer.Equals(messageTemplate.Name, SquarePaymentDefaults.MessageTemplate.FullPaymentDetails))
            {
                eventMessage.AddTokens("%SquarePayment.PaymentURL%",
                   "%SquarePayment.FullPayment%",
                   "%SquarePayment.VehicleTypes%",
                   "%Store.Name%",
                   "%Store.URL%",
                   "%Store.Email%",
                   "%Store.CompanyName%",
                   "%Store.CompanyAddress%",
                   "%Store.CompanyPhoneNumber%",
                   "%Store.Logo%",
                   "%Store.SupportEmail%");

                return;
            }

            if (comparer.Equals(messageTemplate.Name, SquarePaymentDefaults.MessageTemplate.AdditionalPaymentDetails))
            {
                eventMessage.AddTokens("%SquarePayment.PaymentURL%",
                   "%SquarePayment.AdditionalAmount%",
                   "%SquarePayment.VehicleTypes%",
                   "%Store.Name%",
                   "%Store.URL%",
                   "%Store.Email%",
                   "%Store.CompanyName%",
                   "%Store.CompanyAddress%",
                   "%Store.CompanyPhoneNumber%",
                   "%Store.Logo%",
                   "%Store.SupportEmail%");

                return;
            }
        }

        public async Task HandleEventAsync(CommissionReportOrderEvent commissionReportOrderEvent)
        {
            if (commissionReportOrderEvent == null)
                return;

            var squareOrderMappings = await _squareOrderMappingService.GetAllOrdersWithSquareOrdersAsync(createdByCustomer: commissionReportOrderEvent.CreatedByCustomer,
                startDateValue: commissionReportOrderEvent.StartDateValue,
                endDateValue: commissionReportOrderEvent.EndDateValue,
                companyName: commissionReportOrderEvent.CompanyName);

            ((List<CommissionReportOrder>)commissionReportOrderEvent.CommissionReportOrders).AddRange(squareOrderMappings);
        }

        #region Order List Model Prepared Event

        public async Task HandleEventAsync(OrderModel orderModel)
        {
            var comparer = StringComparer.InvariantCultureIgnoreCase;

            if (orderModel == null ||
                orderModel.Id <= 0 ||
                orderModel.LeadId <= 0 ||
                !comparer.Equals(orderModel.PaymentOption, LogisticsDefaults.PaymentOption.COD) ||
                comparer.Equals(orderModel.Status, LogisticsStatusDefaults.Order.New) ||
                comparer.Equals(orderModel.Status, LogisticsStatusDefaults.Order.Canceled))
            {
                return;
            }

            orderModel.HasSquareInvoiceGenerated = await _squareOrderMappingService.HasAnySquareOrderMappingsByOrderIdAsync(orderModel.Id);
        }

        #endregion

        #endregion
    }
}