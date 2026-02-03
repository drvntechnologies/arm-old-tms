using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework.Models;

namespace ARM.Logistics.Payments.Square.Models
{
    /// <summary>
    /// Represents invoice model
    /// </summary>
    public record InvoiceInfoModel : BaseNopModel
    {
        #region Properties

        public int LeadId { get; set; }

        [UIHint("DecimalDollar")]
        public decimal SquareInvoiceAmountValue { get; set; }

        public string SquareInvoiceAmount { get; set; }

        public bool IsInvoicePaid { get; set; }

        public bool IsInvoiceAvailable { get; set; }

        public bool AllowToSendFullInvoice { get; set; }

        public bool HasOpenOrderStatus { get; set; }

        public bool AllowToSendDepositInvoice { get; set; }

        public bool HasAdditionalInvoiceWithDepositOrFullInvoice { get; set; }

        public bool AllowToInvoiceCanceled { get; set; }

        public bool AllowToInvoiceUpdate { get; set; }

        public string Status { get; set; }

        public bool IsSubOrder { get; set; }

        public string SubOrderInvoiceNote { get; set; }

        public string DepositInvoiceNote { get; set; }

        public string FullInvoiceNote { get; set; }

        public string InvoiceNote { get; set; }

        public string ErrorMessage { get; set; }

        #endregion
    }
}