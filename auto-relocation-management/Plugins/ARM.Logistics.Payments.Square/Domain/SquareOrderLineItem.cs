using Nop.Core;

namespace ARM.Logistics.Payments.Square.Domain
{
    public class SquareOrderItem : BaseEntity
    {
        #region Properties

        /// <summary>
        /// Get or set the square order mapping identifier
        /// </summary>
        public int SquareOrderMappingId { get; set; }

        /// <summary>
        /// Get or set the line item name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Get or set the line item amount
        /// </summary>
        public decimal Amount { get; set; }

        #endregion
    }
}