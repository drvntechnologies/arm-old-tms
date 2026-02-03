namespace ARM.Logistics.Payments.Square.Domain
{
    /// <summary>
    /// Represents invoice status enumeration
    /// </summary>
    public enum InvoiceStatus
    {
        /// <summary>
        /// Invoice is draft
        /// </summary>
        Draft = 10,

        /// <summary>
        /// Invoice is unpaid
        /// </summary>
        UnPaid = 20,

        /// <summary>
        /// Invoice is scheduled
        /// </summary>
        Scheduled = 30,

        /// <summary>
        /// Invoice is partially paid
        /// </summary>
        Partially_Paid = 40,

        /// <summary>
        /// Invoice is paid
        /// </summary>
        Paid = 50,

        /// <summary>
        /// Invoice is partially refunded
        /// </summary>
        Partially_Refunded = 60,

        /// <summary>
        /// Invoice is refunded
        /// </summary>
        Refunded = 70,

        /// <summary>
        /// Invoice is canceled
        /// </summary>
        Canceled = 80,

        /// <summary>
        /// Payment is failed
        /// </summary>
        Failed = 90,

        /// <summary>
        /// Payment is pending
        /// </summary>
        Payment_Pending = 100
    }
}