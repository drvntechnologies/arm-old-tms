namespace ARM.Logistics.Payments.Square.Domain
{
    /// <summary>
    /// Represents invoice amount type enumeration
    /// </summary>
    public enum InvoiceAmountType
    {
        /// <summary>
        /// Get the deposit amount
        /// </summary>
        Deposit = 10,

        /// <summary>
        /// Get the full amount
        /// </summary>
        Full = 20,

        /// <summary>
        /// Get the custom amount
        /// </summary>
        Custom = 30,
    }
}