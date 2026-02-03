using ARM.Logistics.Payments.Square.Domain;
using Nop.Web.Framework.Models;

namespace ARM.Logistics.Payments.Square.Models.Invoices
{
    /// <summary>
    /// Represents a invoice model
    /// </summary>
    public partial record InvoiceModel : BaseNopEntityModel
    {
        #region Properties

        public string InvoiceId { get; set; }

        public string CustomOrderNumber { get; set; }

        public string Number { get; set; }

        public string LineItem { get; set; }

        public string Link { get; set; }

        public int StatusId { get; set; }

        public string Amount { get; set; }

        public string CreatedDate { get; set; }

        public string PaidDate { get; set; }

        #region Custom property

        public string Status
        {
            get => ((InvoiceStatus)StatusId).ToString();
        }

        #endregion

        #endregion
    }
}