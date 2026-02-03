using Nop.Web.Framework.Models;

namespace ARM.Logistics.Payments.Square.Models
{
    /// <summary>
    /// Represents payment detail model
    /// </summary>
    public record PaymentDetailModel : BaseNopModel
    {
        public string AmountPaid { get; set; }

        public string AmountDue { get; set; }
    }
}