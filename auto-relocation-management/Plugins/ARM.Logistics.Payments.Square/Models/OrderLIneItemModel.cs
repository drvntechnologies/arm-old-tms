using System.Collections.Generic;
using System.Linq;
using Nop.Web.Framework.Models;

namespace ARM.Logistics.Payments.Square.Models
{
    /// <summary>
    /// Represents order line item model
    /// </summary>
    public record OrderLineItemModel : BaseNopModel
    {
        #region Properties

        public string Name { get; set; }

        public decimal Amount { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents square order model
    /// </summary>
    public class SquareOrderModel
    {
        #region Ctor

        public SquareOrderModel()
        {
            LineItems = new List<OrderLineItemModel>();
        }

        #endregion

        #region Properties

        public decimal Amount { get; set; }

        public bool HasDepositAmount { get; set; }

        public bool HasFullAmount { get; set; }

        public IList<OrderLineItemModel> LineItems { get; set; }

        public bool HasAdditionalAmount => LineItems?.Any() ?? false;

        public string InvoiceNumber { get; set; }

        #endregion
    }
}