using Nop.Web.Framework.Models;

namespace ARM.Web.Areas.Admin.Models.Referrals
{
    /// <summary>
    /// Represents a referral order model
    /// </summary>
    public partial record ReferralOrderModel : BaseNopModel
    {
        #region Properties

        public int Id { get; set; }

        public int OrderId { get; set; }

        public string OrderNumber { get; set; }

        public string CustomerName { get; set; }

        public string DeliveryDate { get; set; }

        public string Status { get; set; }

        public string DueAmount { get; set; }

        public decimal DueAmountValue { get; set; }

        public string Amount { get; set; }

        public decimal AmountValue { get; set; }

        #endregion
    }

    public partial record ReferralOrderAggreratorModel : BaseNopModel
    {
        //aggergator properties

        public string AggregatorTotal { get; set; }

        public string AggregatorPaidAmountTotal { get; set; }

        public string AggregatorUnpaidAmountTotal { get; set; }
    }
}