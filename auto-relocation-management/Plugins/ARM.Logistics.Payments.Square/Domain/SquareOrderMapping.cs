using System;
using Nop.Core;
using Nop.Core.Domain.Common;

namespace ARM.Logistics.Payments.Square.Domain
{
    public class SquareOrderMapping : BaseEntity, ISoftDeletedEntity
    {
        #region Properties

        /// <summary>
        /// Get or set the order identifier
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Get or set the square order identifier
        /// </summary>
        public string SquareOrderId { get; set; }

        /// <summary>
        /// Get or set the square invoice identifier
        /// </summary>
        public string SquareInvoiceId { get; set; }

        /// <summary>
        /// Get or set the square invoice number
        /// </summary>
        public string SquareInvoiceNumber { get; set; }

        /// <summary>
        /// Get or set the order sub total amount
        /// </summary>
        public decimal OrderSubTotalAmount { get; set; }

        /// <summary>
        /// Get or set the invoice status
        /// </summary>
        public int InvoiceStatusId { get; set; }

        /// <summary>
        /// Get or set the invoice amount type
        /// </summary>
        public int InvoiceAmountTypeId { get; set; }

        /// <summary>
        /// Get or set the invoice link
        /// </summary>
        public string InvoiceLink { get; set; }

        /// <summary>
        /// Get or set the date and time of entity creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Get or set the date and time of entity update
        /// </summary>
        public DateTime UpdatedOnUtc { get; set; }

        /// <summary>
        /// Get or set the date and time of entity payment
        /// </summary>
        public DateTime? PaidDateOnUtc { get; set; }

        /// <summary>
        /// Get or set the date and time of entity refund
        /// </summary>
        public DateTime? RefundDateOnUtc { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity has been deleted
        /// </summary>
        public bool Deleted { get; set; }

        #endregion

        #region Custom properties

        /// <summary>
        /// Get or set the invoice status
        /// </summary>
        public InvoiceStatus InvoiceStatus
        {
            get => (InvoiceStatus)InvoiceStatusId;
            set => InvoiceStatusId = (int)value;
        }

        /// <summary>
        /// Get or set the invoice status
        /// </summary>
        public InvoiceAmountType InvoiceAmountType
        {
            get => (InvoiceAmountType)InvoiceAmountTypeId;
            set => InvoiceAmountTypeId = (int)value;
        }

        #endregion
    }
}