using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Areas.Admin.Models.Common;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace ARM.Web.Areas.Admin.Models.Referrals
{
    public partial record ReferralReportSearchModel : BaseSearchModel
    {
        #region Ctor

        public ReferralReportSearchModel()
        {
            OrderStatusIds = new List<int>();
            AvailableOrderStatuses = new List<SelectListItem>();
        }

        #endregion

        #region Properties

        [NopResourceDisplayName("Admin.Referral.Reports.Field.ReferPayee")]
        public string ReferPayee { get; set; }

        [NopResourceDisplayName("Admin.Referral.Order.Field.ActualDeliveryDateFrom")]
        [UIHint("DateNullable")]
        public DateTime? ActualDeliveryDateFrom { get; set; }

        [NopResourceDisplayName("Admin.Referral.Order.Field.ActualDeliveryDateTo")]
        [UIHint("DateNullable")]
        public DateTime? ActualDeliveryDateTo { get; set; }

        [NopResourceDisplayName("Admin.Referral.Order.Field.OrderStatusIds")]
        public IList<int> OrderStatusIds { get; set; }

        public IList<SelectListItem> AvailableOrderStatuses { get; set; }

        public IEnumerable<DTOrder> Order { get; set; }

        #endregion
    }
}
