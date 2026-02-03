using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace ARM.Web.Areas.Admin.Models.Referrals
{
    /// <summary>
    /// Represents a referral search model
    /// </summary>
    public partial record ReferralSearchModel : BaseSearchModel
    {
        public ReferralSearchModel()
        {
            ReferralModel = new ReferralModel();
        }


        [NopResourceDisplayName("Admin.Configuration.Settings.Referral.Fields.Name")]
        public string Name { get; set; }

        public ReferralModel ReferralModel { get; set; }
    }
}