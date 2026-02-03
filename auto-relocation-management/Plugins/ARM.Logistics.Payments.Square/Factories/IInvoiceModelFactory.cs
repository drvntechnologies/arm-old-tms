using System.Threading.Tasks;
using ARM.Logistics.Payments.Square.Models.Invoices;

namespace ARM.Logistics.Payments.Square.Factories
{
    /// <summary>
    /// Represents the invoice model factory
    /// </summary>
    public partial interface IInvoiceModelFactory
    {
        /// <summary>
        /// Prepare invoice search model
        /// </summary>
        /// <param name="searchModel">Invoice search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the invoice search model
        /// </returns>
        Task<InvoiceSearchModel> PrepareInvoiceSearchModelAsync(InvoiceSearchModel searchModel);

        /// <summary>
        /// Prepare paged invoice list model
        /// </summary>
        /// <param name="searchModel">Invoice search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the invoice list model
        /// </returns>
        Task<InvoiceListModel> PrepareInvoiceListModelAsync(InvoiceSearchModel searchModel);
    }
}