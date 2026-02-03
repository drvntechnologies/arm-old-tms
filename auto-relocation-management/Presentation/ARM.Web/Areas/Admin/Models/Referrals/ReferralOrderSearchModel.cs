using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace ARM.Web.Areas.Admin.Models.Referrals
{
    public partial record ReferralOrderSearchModel : BaseSearchModel
    {
        #region Ctor

        public ReferralOrderSearchModel()
        {
            OrderStatusIds = new List<int>();
            AvailableOrderStatuses = new List<SelectListItem>();
        }

        #endregion

        #region Properties

        public int ReferralId { get; set; }

        public string ReferralName { get; set; }

        [NopResourceDisplayName("Admin.Referral.Order.Field.ActualDeliveryDateFrom")]
        [UIHint("DateNullable")]
        public DateTime? ActualDeliveryDateFrom { get; set; }

        [NopResourceDisplayName("Admin.Referral.Order.Field.ActualDeliveryDateTo")]
        [UIHint("DateNullable")]
        public DateTime? ActualDeliveryDateTo { get; set; }

        public DateTime? FirstPickupDate { get; set; }

        public DateTime? LastPickupDate { get; set; }

        [NopResourceDisplayName("Admin.Referral.Order.Field.OrderStatusIds")]
        public IList<int> OrderStatusIds { get; set; }

        public int DeliveredOrderStatusId { get; set; }

        public IList<SelectListItem> AvailableOrderStatuses { get; set; }

        #endregion
    }
}
