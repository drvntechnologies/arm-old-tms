using System;
using Nop.Core;

namespace ARM.Logistics.Payments.Square.Domain
{
    public class SquareTransactionOrderMapping : BaseEntity
    {
        #region Properties

        /// <summary>
        /// Get or set the order identifier
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Get or set the square order mapping identifier
        /// </summary>
        public int SquareOrderMappingId { get; set; }

        /// <summary>
        /// Get or set the payment identifier
        /// </summary>
        public string PaymentId { get; set; }

        /// <summary>
        /// Get or set the transaction identifier
        /// </summary>
        public string TransactionId { get; set; }

        /// <summary>
        /// Get or set the paid date on utc time
        /// </summary>
        public DateTime? PaidDateTimeOnUtc { get; set; }

        /// <summary>
        /// Get or set the date and time of entity refund
        /// </summary>
        public DateTime? RefundDateOnUtc { get; set; }

        #endregion
    }
}