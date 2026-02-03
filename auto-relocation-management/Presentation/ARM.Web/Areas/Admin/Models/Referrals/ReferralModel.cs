using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace ARM.Web.Areas.Admin.Models.Referrals
{
    /// <summary>
    /// Represents a referral model
    /// </summary>
    public partial record ReferralModel : BaseNopEntityModel
    {
        #region Properties

        [NopResourceDisplayName("Admin.ReferralModel.Fields.Name")]
        public string Name { get; set; }

        [NopResourceDisplayName("Admin.ReferralModel.Fields.Active")]
        public bool Active { get; set; }

        #endregion
    }
}