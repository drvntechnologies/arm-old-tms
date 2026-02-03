using Nop.Web.Framework.Models;

namespace ARM.Logistics.Payments.Square.Models.Invoices
{
    /// <summary>
    /// Represents a invoice list model
    /// </summary>
    public partial record InvoiceListModel : BasePagedListModel<InvoiceModel>
    {
    }
}