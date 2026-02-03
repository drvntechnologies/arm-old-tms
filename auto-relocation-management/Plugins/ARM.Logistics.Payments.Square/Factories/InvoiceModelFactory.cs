using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ARM.Logistics.Payments.Square.Models.Invoices;
using ARM.Logistics.Payments.Square.Services.SquareOrderMappings;
using Nop.Services.Catalog;
using Nop.Services.Helpers;
using Nop.Web.Framework.Models.Extensions;

namespace ARM.Logistics.Payments.Square.Factories
{
    /// <summary>
    /// Represents the invoice model factory implementation
    /// </summary>
    public partial class InvoiceModelFactory : IInvoiceModelFactory
    {
        #region Fields

        private readonly ISquareOrderMappingService _squareOrderMappingService;
        private readonly IPriceFormatter _priceFormatter;

        #endregion

        #region Ctor

        public InvoiceModelFactory(ISquareOrderMappingService squareOrderMappingService,
            IPriceFormatter priceFormatter)
        {
            _squareOrderMappingService = squareOrderMappingService;
            _priceFormatter = priceFormatter;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Prepare invoice search model
        /// </summary>
        /// <param name="searchModel">Invoice search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the invoice search model
        /// </returns>
        public virtual Task<InvoiceSearchModel> PrepareInvoiceSearchModelAsync(InvoiceSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            return Task.FromResult(searchModel);
        }

        /// <summary>
        /// Prepare paged invoice list model
        /// </summary>
        /// <param name="searchModel">Invoice search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the invoice list model
        /// </returns>
        public virtual async Task<InvoiceListModel> PrepareInvoiceListModelAsync(InvoiceSearchModel searchModel)
        {
            if (searchModel == null)
            {
                throw new ArgumentNullException(nameof(searchModel));
            }

            //get square invoices
            var invoices = await _squareOrderMappingService.GetSquareOrderMappingsByOrderIdAsync(orderId: searchModel.OrderId);

            var invoicePagedList = invoices.ToPagedList(searchModel);

            //prepare list model
            var model = await new InvoiceListModel().PrepareToGridAsync(searchModel, invoicePagedList, () =>
            {
                return invoicePagedList.SelectAwait(async map =>
                {
                    var invoice = map.Mapping;

                    InvoiceModel invoiceModel = new()
                    {
                        Id = invoice.Id,
                        InvoiceId = invoice.SquareInvoiceId,
                        CustomOrderNumber = map.CustomOrderNumber,
                        Number = invoice.SquareInvoiceNumber,
                        Link = invoice.InvoiceLink,
                        StatusId = invoice.InvoiceStatusId,
                        CreatedDate = invoice.CreatedOnUtc.ToDateFormat(),
                        PaidDate = invoice.PaidDateOnUtc.ToDateFormat(),
                        Amount = await _priceFormatter.FormatPriceAsync(invoice.OrderSubTotalAmount, true, false)
                    };

                    var orderItems = await _squareOrderMappingService.GetAllSquareOrderItemsAsync(invoice.Id);

                    if (orderItems == null || orderItems.Count <= 0)
                    {
                        return invoiceModel;
                    }

                    if (orderItems.Count == 1)
                    {
                        invoiceModel.LineItem = orderItems.FirstOrDefault()?.Name ?? string.Empty;

                        return invoiceModel;
                    }

                    var length = orderItems.Count;
                    StringBuilder sb = new();

                    for (int item = 1; item <= length; item++)
                    {
                        if (sb.Length > 0)
                        {
                            sb.Append("<br />");
                        }

                        sb.AppendFormat("({0}) {1}", item, orderItems[item - 1].Name);
                    }

                    invoiceModel.LineItem = sb.ToString();

                    return invoiceModel;
                });
            });

            return model;
        }

        #endregion
    }
}