using System;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace ARM.Web.Areas.Admin.Models.Referrals
{
    /// <summary>
    /// Represents a referral report model
    /// </summary>
    public partial record ReferralReportModel : BaseNopModel
    {
        #region Ctor

        public ReferralReportModel()
        {
            ReferralOrderSearchModel = new ReferralOrderSearchModel();
        }

        #endregion

        #region Properties

        public int Id { get; set; }

        [NopResourceDisplayName("Admin.Referral.Reports.Field.Email")]
        public string Email { get; set; }

        [NopResourceDisplayName("Admin.Referral.Reports.Field.PhoneNumber")]
        public string PhoneNumber { get; set; }

        [NopResourceDisplayName("Admin.Referral.Reports.Field.Note")]
        public string Note { get; set; }

        public int OrderId { get; set; }

        public string CustomOrderNumber { get; set; }

        [NopResourceDisplayName("Admin.Referral.Reports.Field.ReferenceNumber")]
        public int ReferralOrderMappingId { get; set; }

        [NopResourceDisplayName("Admin.Referral.Reports.Field.ReferPayee")]
        public string ReferPayee { get; set; }

        [NopResourceDisplayName("Admin.Referral.Reports.Field.CustomerName")]
        public string CustomerName { get; set; }

        public string DueAmount { get; set; }

        [NopResourceDisplayName("Admin.Referral.Reports.Field.DueAmount")]
        public decimal DueAmountValue { get; set; }

        [NopResourceDisplayName("Admin.Referral.Reports.Field.PaidAmount")]
        public decimal PaidAmountValue { get; set; }

        [NopResourceDisplayName("Admin.Referral.Order.Field.ReferralAmount")]
        public decimal AmountValue { get; set; }

        [NopResourceDisplayName("Admin.Referral.Order.Field.ReferralAmount")]
        public string Amount { get; set; }

        public DateTime? ActualDeliveryDateFrom { get; set; }

        public DateTime? ActualDeliveryDateTo { get; set; }

        public ReferralOrderSearchModel ReferralOrderSearchModel { get; set; }

        #endregion
    }
}