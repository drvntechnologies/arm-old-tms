using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ARM.Logistics.Payments.Square.Domain;
using Nop.Core.Domain.Orders;

namespace ARM.Logistics.Payments.Square.Services.SquareOrderMappings
{
    /// <summary>
    /// Square order mapping service interface
    /// </summary>
    public partial interface ISquareOrderMappingService
    {
        #region Square Order Mapping

        /// <summary>
        /// Gets a square order mapping
        /// </summary>
        /// <param name="squareOrderId">The square order identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the square order mapping
        /// </returns>
        Task<SquareOrderMapping> GetSquareOrderMappingBySquareOrderIdAsync(string squareOrderId);

        /// <summary>
        /// Gets a paid square order mapping
        /// </summary>
        /// <param name="orderId">The order identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of square order mappings
        /// </returns>
        Task<IList<SquareOrderMapping>> GetPaidSquareOrderMappingByOrderIdAsync(int orderId);

        /// <summary>
        /// check a paid square order mapping
        /// </summary>
        /// <param name="orderId">The order identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the boolean value for checking the order invoice.
        /// </returns>
        Task<bool> HasPaidSquareOrderMappingByOrderIdAsync(int orderId);

        /// <summary>
        /// Gets a square order mapping
        /// </summary>
        /// <param name="orderId">The order identifier</param>
        /// <param name="invoiceAmountTypeId">The invoice amount identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the square order mapping
        /// </returns>
        Task<SquareOrderMapping> GetSquareOrderMappingByOrderIdAsync(int orderId, int invoiceAmountTypeId);

        /// <summary>
        /// Gets a unpaid invoice
        /// </summary>
        /// <param name="orderId">The order identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the square order mapping
        /// </returns>
        Task<SquareOrderMapping> GetUnPaidInvoiceByOrderIdAsync(int orderId);

        /// <summary>
        /// Gets a square order mapping
        /// </summary>
        /// <param name="squareInvoiceId">The square invoice identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the square order mapping
        /// </returns>
        Task<SquareOrderMapping> GetSquareOrderMappingBySquareInvoiceIdAsync(string squareInvoiceId);

        /// <summary>
        /// Gets a square order mappings
        /// </summary>
        /// <param name="orderId">The order identifier</param>
        /// <param name="showHidden">If it's true then show deleted records also.</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of square order mappings with custom number
        /// </returns>
        Task<IList<SquareOrderMappingWithCustomNumber>> GetSquareOrderMappingsByOrderIdAsync(int orderId, bool showHidden = false);


        /// <summary>
        /// Has any square order mappings
        /// </summary>
        /// <param name="orderId">The order identifier</param>
        /// <param name="showHidden">If it's true then show deleted records also.</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the boolean value if any square order mappings found for order identifier
        /// </returns>
        Task<bool> HasAnySquareOrderMappingsByOrderIdAsync(int orderId, bool showHidden = false);

        /// <summary>
        /// Check a invoice is generated for the parent order with sub orders
        /// </summary>
        /// <param name="orderId">The order identifier</param>
        /// <param name="showHidden">If it's true then show deleted records also.</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the boolean value if any square invoices found for order identifier
        /// </returns>
        Task<bool> HasAnySquareInvoiceGeneratedForParentOrderAsync(int orderId)
            ;
        /// <summary>
        /// Deletes a square order mapping
        /// </summary>
        /// <param name="squareOrderMapping">The square order mapping</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task DeleteSquareOrderMappingAsync(SquareOrderMapping squareOrderMapping);

        /// <summary>
        /// Inserts a square order mapping
        /// </summary>
        /// <param name="squareOrderMapping">The square order mapping</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task InsertSquareOrderMappingAsync(SquareOrderMapping squareOrderMapping);

        /// <summary>
        /// Updates the square order mapping
        /// </summary>
        /// <param name="squareOrderMapping">The square order mapping</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task UpdateSquareOrderMappingAsync(SquareOrderMapping squareOrderMapping);

        /// <summary>
        /// Get the last invoice number for the order
        /// </summary>
        /// <param name="orderId">The order identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the last order invoice number
        /// </returns>
        Task<string> GetLastInvoiceNumberAsync(int orderId);

        /// <summary>
        /// Get the invoice number for the order
        /// </summary>
        /// <param name="order">The order</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the generated invoice number
        /// </returns>
        Task<string> GenerateInvoiceNumberAsync(Order order);

        #endregion

        #region Square Transaction Order Mapping

        /// <summary>
        /// Gets a square transaction order mapping
        /// </summary>
        /// <param name="paymentId">The payment identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the square transaction order mapping
        /// </returns>
        Task<SquareTransactionOrderMapping> GetSquareTransactionOrderMappingByPaymentIdAsync
            (string paymentId);

        /// <summary>
        /// Inserts a square transaction order mapping
        /// </summary>
        /// <param name="squareTransactionOrderMapping">The square transaction order mapping</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task InsertSquareTransactionOrderMappingAsync(SquareTransactionOrderMapping squareTransactionOrderMapping);

        /// <summary>
        /// Updates the square transaction order mapping
        /// </summary>
        /// <param name="squareTransactionOrderMapping">The square transaction order mapping</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task UpdateSquareTransactionOrderMappingAsync(SquareTransactionOrderMapping squareTransactionOrderMapping);

        #endregion

        #region Square Order Item

        /// <summary>
        /// Inserts a square order item
        /// </summary>
        /// <param name="squareOrderItem">The square order item</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task InsertSquareOrderItemAsync(SquareOrderItem squareOrderItem);

        /// <summary>
        /// Get all square order items
        /// </summary>
        /// <param name="squareOrderMappingId">The square order mapping identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of square order items
        /// </returns>
        Task<IList<SquareOrderItem>> GetAllSquareOrderItemsAsync(int squareOrderMappingId = 0);

        #endregion

        #region Commission Report Order

        /// <summary>
        /// Get all commission report orders
        /// </summary>
        /// <param name="createdByCustomer">createdByCustomer</param>
        /// <param name="startDateValue">startDateValue</param>
        /// <param name="endDateValue">endDateValue</param>
        /// <param name="companyName">The company name</param>
        /// <returns>List of CommissionReportOrder</returns>
        Task<IList<CommissionReportOrder>> GetAllOrdersWithSquareOrdersAsync(int createdByCustomer = 0,
            DateTime? startDateValue = null,
            DateTime? endDateValue = null,
            string companyName = null);

        #endregion
    }
}