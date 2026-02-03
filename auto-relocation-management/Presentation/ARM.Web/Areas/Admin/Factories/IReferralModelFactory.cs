using System.Threading.Tasks;
using ARM.Web.Areas.Admin.Models.Referrals;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Referrals;

namespace ARM.Web.Areas.Admin.Factories
{
    /// <summary>
    /// Represents the referral model factory
    /// </summary>
    public partial interface IReferralModelFactory
    {
        /// <summary>
        /// Prepare referral search model
        /// </summary>
        /// <param name="searchModel">Referral search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the referral search model
        /// </returns>
        Task<ReferralSearchModel> PrepareReferralSearchModelAsync(ReferralSearchModel searchModel);

        /// <summary>
        /// Prepare paged referral list model
        /// </summary>
        /// <param name="searchModel">Referral search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the referral list model
        /// </returns>
        Task<ReferralListModel> PrepareReferralListModelAsync(ReferralSearchModel searchModel);

        #region Referral report or management

        /// <summary>
        /// Prepare referral report search model
        /// </summary>
        /// <param name="searchModel">Referral report search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the referral report search model
        /// </returns>
        Task<ReferralReportSearchModel> PrepareReferralReportSearchModelAsync(ReferralReportSearchModel searchModel);

        /// <summary>
        /// Prepare referral report list model
        /// </summary>
        /// <param name="searchModel">Referral report search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the referral report list model
        /// </returns>
        Task<ReferralReportListModel> PrepareReferralReportListModelAsync(ReferralReportSearchModel searchModel);

        /// <summary>
        /// Prepares the referral report model asynchronous.
        /// </summary>
        /// <param name="model">The referral model.</param>
        /// <param name="referralId">The referral identifier.</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the referral report model
        /// </returns>
        Task<ReferralReportModel> PrepareReferralReportModelAsync(ReferralReportModel model, int referralId);

        /// <summary>
        /// Prepares the referral report model asynchronous.
        /// </summary>
        /// <param name="referralOrderMapping">The referral order mapping.</param>
        /// <param name="referral">The referral.</param>
        /// <param name="order">The order.</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the referral report model
        /// </returns>
        Task<ReferralReportModel> PrepareReferralReportModelAsync(ReferralOrderMapping referralOrderMapping, Referral referral, Order order);

        /// <summary>
        /// Prepare referral order list model
        /// </summary>
        /// <param name="searchModel">Referral order search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the referral order list model
        /// </returns>
        Task<ReferralOrderListModel> PrepareReferralOrderListModelAsync(ReferralOrderSearchModel searchModel);

        /// <summary>
        /// Prepare referral order report aggregates
        /// </summary>
        /// <param name="searchModel">Referral order search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the referral order report aggregates
        /// </returns>
        Task<ReferralOrderAggreratorModel> PrepareReferralOrderReportAggregatesAsync(ReferralOrderSearchModel searchModel);

        #endregion
    }
}