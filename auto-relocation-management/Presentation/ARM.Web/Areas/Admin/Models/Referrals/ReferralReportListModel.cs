using Nop.Web.Framework.Models;

namespace ARM.Web.Areas.Admin.Models.Referrals
{
    /// <summary>
    /// Represents a referral report list model
    /// </summary>
    public partial record ReferralReportListModel : BasePagedListModel<ReferralReportModel>
    {
    }
}
