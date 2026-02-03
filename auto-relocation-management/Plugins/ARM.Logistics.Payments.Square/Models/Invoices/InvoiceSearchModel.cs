using Nop.Web.Framework.Models;

namespace ARM.Logistics.Payments.Square.Models.Invoices
{
    /// <summary>
    /// Represents a invoice search model
    /// </summary>
    public partial record InvoiceSearchModel : BaseSearchModel
    {
        public int OrderId { get; set; }
    }
}