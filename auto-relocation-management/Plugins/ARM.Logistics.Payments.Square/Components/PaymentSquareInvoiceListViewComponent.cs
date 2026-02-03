using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ARM.Logistics.Payments.Square.Factories;
using ARM.Logistics.Payments.Square.Models.Invoices;
using ARM.Logistics.Payments.Square.Services.SquareOrderMappings;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.QuotesStatus;
using Nop.Services.Orders;
using Nop.Services.QuotesStatus;

namespace ARM.Logistics.Payments.Square.Components
{
    /// <summary>
    /// Represents payment square invoice list view component
    /// </summary>
    public class PaymentSquareInvoiceListViewComponent : ViewComponent
    {
        #region Fields

        private readonly IOrderService _orderService;
        private readonly ILogisticsStatusService _logisticsStatusService;
        private readonly IInvoiceModelFactory _invoiceModelFactory;
        private readonly ISquareOrderMappingService _squareOrderMappingService;

        #endregion

        #region Ctor

        public PaymentSquareInvoiceListViewComponent(IOrderService orderService,
            ILogisticsStatusService logisticsStatusService,
            IInvoiceModelFactory invoiceModelFactory,
            ISquareOrderMappingService squareOrderMappingService)
        {
            _orderService = orderService;
            _logisticsStatusService = logisticsStatusService;
            _invoiceModelFactory = invoiceModelFactory;
            _squareOrderMappingService = squareOrderMappingService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Invoke view component
        /// </summary>
        /// <param name="widgetZone">Widget zone name</param>
        /// <param name="additionalData">Additional data</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the view component result
        /// </returns>
        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
        {
            if (string.IsNullOrWhiteSpace(widgetZone) ||
                !widgetZone.Equals("admin_order_details_invoice_section") ||
                additionalData is not int leadId)
            {
                return Content(string.Empty);
            }

            var order = await _orderService.GetOrderByIdAsync(leadId);
            if (order == null ||
                order.Deleted)
            {
                return Content(string.Empty);
            }

            var comparer = StringComparer.InvariantCultureIgnoreCase;

            if (string.IsNullOrWhiteSpace(order.PaymentOption))
            {
                return Content(string.Empty);
            }

            if (order.IsChildOrder)
            {
                if (!comparer.Equals(order.PaymentOption, LogisticsDefaults.PaymentOption.COD))
                {
                    return Content(string.Empty);
                }

                var subOrderMapping = await _orderService.GetLogisticsChildOrderMappingByOrderIdAsync(order.Id);
                var parentOrder = await _orderService.GetOrderByOrderIdAsync(subOrderMapping?.ParentOrderId ?? 0);
                if (parentOrder == null ||
                    parentOrder.Deleted ||
                    !comparer.Equals(parentOrder.PaymentOption, LogisticsDefaults.PaymentOption.Billable))
                {
                    return Content(string.Empty);
                }
            }
            else
            {
                var hasAnyInvoiceGenerated = await _squareOrderMappingService.HasAnySquareInvoiceGeneratedForParentOrderAsync(order.Id);

                if (!hasAnyInvoiceGenerated)
                {
                    return Content(string.Empty);
                }
            }

            var status = (await _logisticsStatusService.GetLogisticsStatusByIdAsync(order.StatusId))?.Title
                    ?? string.Empty;

            List<string> allowStatus = new(5)
            {
                LogisticsStatusDefaults.Order.Open,
                LogisticsStatusDefaults.Order.Covered,
                LogisticsStatusDefaults.Order.Dispatched,
                LogisticsStatusDefaults.Order.InTransit,
                LogisticsStatusDefaults.Order.Delivered
            };

            if (string.IsNullOrWhiteSpace(status) ||
                !allowStatus.Exists(s => comparer.Equals(s, status)))
            {
                return Content(string.Empty);
            }

            var model = await _invoiceModelFactory.PrepareInvoiceSearchModelAsync(new InvoiceSearchModel());

            model.OrderId = order.Id;

            const string viewPath = "~/Plugins/Logistics.Payments.Square/Views/Shared/Components/PaymentSquareInvoiceList/Default.cshtml";

            return View(viewPath, model);
        }

        #endregion
    }
}