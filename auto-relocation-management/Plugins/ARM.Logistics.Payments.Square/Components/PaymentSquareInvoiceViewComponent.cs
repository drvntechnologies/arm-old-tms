using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ARM.Logistics.Payments.Square.Domain;
using ARM.Logistics.Payments.Square.Models;
using ARM.Logistics.Payments.Square.Security;
using ARM.Logistics.Payments.Square.Services.SquareOrderMappings;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.QuotesStatus;
using Nop.Core.Infrastructure;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.LogisticsQuotes;
using Nop.Services.Orders;
using Nop.Services.QuotesStatus;
using Nop.Services.Security;

namespace ARM.Logistics.Payments.Square.Components
{
    /// <summary>
    /// Represents payment square invoice info view component
    /// </summary>
    public class PaymentSquareInvoiceViewComponent : ViewComponent
    {
        #region Fields

        private readonly IOrderService _orderService;
        private readonly ISquareOrderMappingService _squareOrderMappingService;
        private readonly ILogisticsStatusService _logisticsStatusService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly ILocalizationService _localizationService;
        private readonly IPermissionService _permissionService;

        #endregion

        #region Ctor

        public PaymentSquareInvoiceViewComponent(IOrderService orderService,
            ISquareOrderMappingService squareOrderMappingService,
            ILogisticsStatusService logisticsStatusService,
            IPriceFormatter priceFormatter,
            ILocalizationService localizationService,
            IPermissionService permissionService)
        {
            _orderService = orderService;
            _squareOrderMappingService = squareOrderMappingService;
            _logisticsStatusService = logisticsStatusService;
            _priceFormatter = priceFormatter;
            _localizationService = localizationService;
            _permissionService = permissionService;
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
            if (!await _permissionService.AuthorizeAsync(SquarePermissionProvider.ManageSquareInvoice))
            {
                return Content(string.Empty);
            }

            if (string.IsNullOrWhiteSpace(widgetZone) || !widgetZone.Equals("admin_order_details_square_payment_invoice_buttons") || additionalData is not int leadId)
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

            if (string.IsNullOrWhiteSpace(order.PaymentOption) ||
                !comparer.Equals(order.PaymentOption, LogisticsDefaults.PaymentOption.COD))
            {
                return Content(string.Empty);
            }

            if (order.IsChildOrder)
            {
                var subOrderMapping = await _orderService.GetLogisticsChildOrderMappingByOrderIdAsync(order.Id);
                var parentOrder = await _orderService.GetOrderByOrderIdAsync(subOrderMapping?.ParentOrderId ?? 0);
                if (parentOrder == null ||
                    parentOrder.Deleted ||
                    !comparer.Equals(parentOrder.PaymentOption, LogisticsDefaults.PaymentOption.Billable))
                {
                    return Content(string.Empty);
                }
            }

            var status = (await _logisticsStatusService.GetLogisticsStatusByIdAsync(order.StatusId))?.Title
                    ?? string.Empty;

            var allowStatus = new List<string>(5)
            {
                LogisticsStatusDefaults.Order.Open,
                LogisticsStatusDefaults.Order.Covered,
                LogisticsStatusDefaults.Order.Dispatched,
                LogisticsStatusDefaults.Order.InTransit,
                LogisticsStatusDefaults.Order.Delivered
            };

            if (string.IsNullOrWhiteSpace(status) ||
                !allowStatus.Any(s => comparer.Equals(s, status)))
            {
                return Content(string.Empty);
            }

            var hasAnyInvoiceGenerated = await _squareOrderMappingService.HasAnySquareOrderMappingsByOrderIdAsync(order.Id);

            if (order.IsChildOrder && hasAnyInvoiceGenerated)
            {
                return Content(string.Empty);
            }

            var depositInvoice = await _squareOrderMappingService.GetSquareOrderMappingByOrderIdAsync(order.Id, (int)InvoiceAmountType.Deposit);

            InvoiceInfoModel model = new()
            {
                LeadId = order.LeadId,
                HasOpenOrderStatus = comparer.Equals(LogisticsStatusDefaults.Order.Open, status),
                AllowToSendDepositInvoice = depositInvoice == null &&
                                            !hasAnyInvoiceGenerated &&
                                            !comparer.Equals(LogisticsStatusDefaults.Order.Open, status),
                HasAdditionalInvoiceWithDepositOrFullInvoice = false,
                AllowToSendFullInvoice = !await _squareOrderMappingService.HasPaidSquareOrderMappingByOrderIdAsync(order.Id),
                IsSubOrder = order.IsChildOrder
            };

            const string viewPath = "~/Plugins/Logistics.Payments.Square/Views/Shared/Components/PaymentSquareInvoice/Default.cshtml";

            var price = order.Price;
            var carrierPay = order.CarrierPay;

            if (!order.IsChildOrder)
            {
                var financeDetails = await _orderService.GetParentOrderFinanceDetailsAsync(order);

                if (financeDetails != null)
                {
                    price = financeDetails.CustomerPrice;
                    carrierPay = financeDetails.CarrierPay;
                }
            }

            if (model.AllowToSendDepositInvoice || model.IsSubOrder)
            {
                var depositAmountInDecimal = !model.IsSubOrder
                                ? price - carrierPay
                                : decimal.Zero;

                if (model.IsSubOrder)
                {
                    var logisticsAccessorialService = EngineContext.Current.Resolve<ILogisticsAccessorialService>();

                    var accessorials = await logisticsAccessorialService.GetAllLogisticsAccessorialsAsync(order.LeadId);

                    if (accessorials?.Count > 0)
                    {
                        depositAmountInDecimal = accessorials.Sum(a => a.AR);
                    }
                }

                var depositAmount = await _priceFormatter.FormatPriceAsync(depositAmountInDecimal, true, false);
                var fullAmount = await _priceFormatter.FormatPriceAsync(price, true, false);

                if (model.IsSubOrder)
                {
                    model.SubOrderInvoiceNote = string.Format(await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Invoice.Note.SubOrder.FullAmount.Create"),
                    depositAmount,
                    SquarePaymentDefaults.Order.TotalCost_LineItem);

                    return View(viewPath, model);
                }

                model.DepositInvoiceNote = string.Format(await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Invoice.Note.DepositAmount.Create"),
                depositAmount,
                SquarePaymentDefaults.Order.Deposit_LineItem);

                model.FullInvoiceNote = string.Format(await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Invoice.Note.FullAmount.Create"),
                    fullAmount,
                    SquarePaymentDefaults.Order.TotalCost_LineItem);

                model.InvoiceNote = await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Invoice.Note.CustomAmount.Create");

                return View(viewPath, model);
            }

            var squareOrderMapping = await _squareOrderMappingService.GetUnPaidInvoiceByOrderIdAsync(order.Id);

            if (string.IsNullOrWhiteSpace(squareOrderMapping?.SquareInvoiceId) ||
                string.IsNullOrWhiteSpace(squareOrderMapping?.SquareOrderId))
            {
                if (model.HasOpenOrderStatus)
                {
                    var fullAmount = await _priceFormatter.FormatPriceAsync(price, true, false);
                    model.FullInvoiceNote = string.Format(await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Invoice.Note.FullAmount.Create"),
                        fullAmount,
                        SquarePaymentDefaults.Order.TotalCost_LineItem);
                }
                else
                {
                    model.InvoiceNote = await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Invoice.Note.CustomAmount.Create");
                }

                return View(viewPath, model);
            }

            var squareOrder = await _squareOrderMappingService.GetAllSquareOrderItemsAsync(squareOrderMapping.Id);
            if (squareOrder == null || squareOrder.Count <= 0)
            {
                model.ErrorMessage = await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Order.Details.NotFound");

                return View(viewPath, model);
            }

            model.HasAdditionalInvoiceWithDepositOrFullInvoice = true;

            var firstLineItem = squareOrder.First();

            model.SquareInvoiceAmountValue = firstLineItem.Amount;
            model.SquareInvoiceAmount = await _priceFormatter.FormatPriceAsync(model.SquareInvoiceAmountValue, true, false);

            StringBuilder sb = new();

            sb.AppendFormat(await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Invoice.Note.DepositOrFullAmount"),
                model.SquareInvoiceAmount,
                firstLineItem.Name);

            var additionalLineItems = squareOrder.Skip(1);
            if (additionalLineItems.Any())
            {
                sb.AppendLine();

                var lineItemNames = string.Join(", ", additionalLineItems.Select(ol => ol.Name));

                var additionalAmountValue = additionalLineItems.Sum(ol => ol.Amount);
                var additionalAmount = await _priceFormatter.FormatPriceAsync(additionalAmountValue, true, false);

                sb.AppendFormat(await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.Invoice.Note.AdditionalAmount"),
                    additionalAmount,
                    lineItemNames);
            }

            model.InvoiceNote = sb.ToString();

            return View(viewPath, model);
        }

        #endregion
    }
}