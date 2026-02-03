using System.Linq;
using System.Threading.Tasks;
using ARM.Logistics.Payments.Square.Models;
using ARM.Logistics.Payments.Square.Services.SquareOrderMappings;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Catalog;
using Nop.Services.Orders;

namespace ARM.Logistics.Payments.Square.Components
{
    /// <summary>
    /// Represents payment square details view component
    /// </summary>
    public class PaymentSquareDetailsViewComponent : ViewComponent
    {
        #region Fields

        private readonly IOrderService _orderService;
        private readonly ISquareOrderMappingService _squareOrderMappingService;
        private readonly IPriceFormatter _priceFormatter;

        #endregion

        #region Ctor

        public PaymentSquareDetailsViewComponent(IOrderService orderService,
            ISquareOrderMappingService squareOrderMappingService,
            IPriceFormatter priceFormatter)
        {
            _orderService = orderService;
            _squareOrderMappingService = squareOrderMappingService;
            _priceFormatter = priceFormatter;
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
                !widgetZone.Equals("admin_order_details_square_payment_details") ||
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

            var firstPaidInvoiceId = (await _squareOrderMappingService.GetPaidSquareOrderMappingByOrderIdAsync(order.Id))
                        ?.FirstOrDefault()?.Id ?? 0;
            var squareOrderItem = (await _squareOrderMappingService.GetAllSquareOrderItemsAsync(firstPaidInvoiceId))
                        ?.FirstOrDefault();

            if (squareOrderItem == null)
            {
                return Content(string.Empty);
            }

            var price = order.Price;

            if (!order.IsChildOrder)
            {
                var financeDetails = await _orderService.GetParentOrderFinanceDetailsAsync(order);
                price = financeDetails != null
                    ? financeDetails.CustomerPrice
                    : price;
            }

            PaymentDetailModel model = new()
            {
                AmountPaid = await _priceFormatter.FormatPriceAsync(squareOrderItem.Amount, true, false),
                AmountDue = await _priceFormatter.FormatPriceAsync(price - squareOrderItem.Amount, true, false)
            };

            const string viewPath = "~/Plugins/Logistics.Payments.Square/Views/Shared/Components/PaymentSquareDetails/Default.cshtml";

            return View(viewPath, model);
        }

        #endregion
    }
}