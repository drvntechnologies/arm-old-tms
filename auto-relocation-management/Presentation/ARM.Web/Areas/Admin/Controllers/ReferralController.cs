using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ARM.Web.Areas.Admin.Factories;
using ARM.Web.Areas.Admin.Models.Referrals;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Referrals;
using Nop.Core.Infrastructure;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.LogisticsQuotes;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Referrals;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Controllers;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Framework.Mvc;

namespace ARM.Web.Areas.Admin.Controllers
{
    public partial class ReferralController : BaseAdminController
    {
        #region Field

        private readonly IPermissionService _permissionService;
        private readonly IReferralService _referralService;
        private readonly IReferralModelFactory _referralModelFactory;
        private readonly IBaseAdminModelFactory _baseAdminModelFactory;
        private readonly ILocalizationService _localizationService;
        private readonly ICustomerService _customerService;
        private readonly IOrderService _orderService;
        private readonly ILogisticsAccessorialService _logisticsAccessorialService;
        private readonly INotificationService _notificationService;

        #endregion

        #region Ctor

        public ReferralController(IPermissionService permissionService,
            IReferralService referralService,
            IReferralModelFactory referralModelFactory,
            IBaseAdminModelFactory baseAdminModelFactory,
            ILocalizationService localizationService,
            ICustomerService customerService,
            IOrderService orderService,
            ILogisticsAccessorialService logisticsAccessorialService,
            INotificationService notificationService)
        {
            _permissionService = permissionService;
            _referralService = referralService;
            _referralModelFactory = referralModelFactory;
            _baseAdminModelFactory = baseAdminModelFactory;
            _localizationService = localizationService;
            _customerService = customerService;
            _orderService = orderService;
            _logisticsAccessorialService = logisticsAccessorialService;
            _notificationService = notificationService;
        }

        #endregion

        #region Method

        public virtual IActionResult Index()
        {
            return RedirectToAction("List");
        }

        public virtual async Task<IActionResult> List()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
                return AccessDeniedView();

            var model = await _referralModelFactory.PrepareReferralSearchModelAsync(new ReferralSearchModel());

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> List(ReferralSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
                return await AccessDeniedDataTablesJson();

            var model = await _referralModelFactory.PrepareReferralListModelAsync(searchModel);

            return Json(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Create(ReferralModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
                return AccessDeniedView();

            if (string.IsNullOrWhiteSpace(model?.Name))
                return ErrorJson(await _localizationService.GetResourceAsync("Admin.Referral.Fields.Name.Required"));

            try
            {
                await _referralService.InsertReferralAsync(new Referral()
                {
                    Name = model.Name,
                    Active = model.Active,
                });

                return Json(new { Result = true });
            }
            catch (Exception ex)
            {
                return ErrorJson(ex.Message);
            }
        }

        [HttpPost]
        public virtual async Task<IActionResult> Update(ReferralModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
                return AccessDeniedView();

            if (string.IsNullOrWhiteSpace(model?.Name))
                return ErrorJson(await _localizationService.GetResourceAsync("Admin.Referral.Fields.Name.Required"));

            try
            {
                var referral = await _referralService.GetReferralByIdAsync(model.Id)
                ?? throw new ArgumentException("No referral found with the specified id");

                var referralName = referral.Name;

                referral.Name = model.Name;
                referral.Active = model.Active;
                await _referralService.UpdateReferralAsync(referral);

                var orderService = EngineContext.Current.Resolve<IOrderService>();

                await orderService.UpdateReferralNameInOrderAsync(referralName, referral.Name);

                return new NullJsonResult();
            }
            catch (Exception ex)
            {
                return ErrorJson(ex.Message);
            }
        }

        [HttpPost]
        public virtual async Task<IActionResult> Delete(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
                return await AccessDeniedDataTablesJson();

            var referral = await _referralService.GetReferralByIdAsync(id)
                ?? throw new ArgumentException("No referral found with the specified id");

            await _referralService.DeleteReferralAsync(referral);

            return new NullJsonResult();
        }

        [HttpPost]
        public virtual async Task<IActionResult> AddRecord(ReferralModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageSettings))
                return await AccessDeniedDataTablesJson();

            if (string.IsNullOrWhiteSpace(model?.Name))
                return Json(new { Status = false, Message = await _localizationService.GetResourceAsync("Admin.Referral.Fields.Name.Required") });

            try
            {
                Referral referral = new()
                {
                    Name = model.Name,
                    Active = model.Active,
                };

                await _referralService.InsertReferralAsync(referral);

                List<SelectListItem> records = new();

                await _baseAdminModelFactory.PrepareReferredAsync(records, true, await _localizationService.GetResourceAsync("Admin.Orders.Select.Select"));

                var referralName = referral.Active ? referral.Name : "0";

                return Json(new { Status = true, Records = records, referralName = referralName });
            }
            catch (Exception ex)
            {
                return Json(new { Status = false, Message = ex.Message });
            }
        }

        #region Referral Management Or Report

        public virtual async Task<IActionResult> Report()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReferralManagements))
                return AccessDeniedView();

            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReferralManagementsView))
                return AccessDeniedView();

            //prepare model
            var model = await _referralModelFactory.PrepareReferralReportSearchModelAsync(new ReferralReportSearchModel());

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> ReportList(ReferralReportSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReferralManagements))
                return await AccessDeniedDataTablesJson();

            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReferralManagementsView))
                return await AccessDeniedDataTablesJson();

            //prepare model
            var model = await _referralModelFactory.PrepareReferralReportListModelAsync(searchModel);

            return Json(model);
        }

        public virtual async Task<IActionResult> Management(int id, DateTime? actualDeliveryDateFrom = null, DateTime? actualDeliveryDateTo = null)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReferralManagements))
                return AccessDeniedView();

            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReferralManagementsView))
                return AccessDeniedView();

            var model = await _referralModelFactory.PrepareReferralReportModelAsync(null, id);

            #region Prepare order search model

            if (actualDeliveryDateFrom.HasValue)
            {
                model.ReferralOrderSearchModel.ActualDeliveryDateFrom = actualDeliveryDateFrom.Value;
            }

            if (actualDeliveryDateTo.HasValue)
            {
                model.ReferralOrderSearchModel.ActualDeliveryDateTo = actualDeliveryDateTo.Value;
            }

            #endregion

            return View(model);
        }

        [HttpPost, ActionName("Management")]
        public virtual async Task<IActionResult> ProfileUpdate(ReferralReportModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReferralManagements))
                return AccessDeniedView();

            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReferralManagementsView))
                return AccessDeniedView();

            var referral = await _referralService.GetReferralByIdAsync(model.Id);
            if (referral == null)
                return RedirectToAction(nameof(this.Report));

            model.ReferPayee = referral.Name;

            if (!string.IsNullOrEmpty(model.Email) && !CommonHelper.IsValidEmail(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), await _localizationService.GetResourceAsync("Admin.Common.WrongEmail"));
            }

            var emailPattern = _customerService.EmailPatternCheck(model.Email);
            if (!emailPattern)
            {
                ModelState.AddModelError(nameof(model.Email), await _localizationService.GetResourceAsync("Admin.Common.Fields.ValidEmail.Required"));
            }

            if (!string.IsNullOrWhiteSpace(model.PhoneNumber) && !Regex.IsMatch(model.PhoneNumber, @"^(\(\d{3}\) \d{3}-\d{4}|\d{10})$"))
            {
                ModelState.AddModelError(nameof(model.PhoneNumber), await _localizationService.GetResourceAsync("Admin.Fields.PhoneNumber.InValid"));
            }

            #region Prepare order search model

            model.ReferralOrderSearchModel.ActualDeliveryDateFrom = model.ActualDeliveryDateFrom;
            model.ReferralOrderSearchModel.ActualDeliveryDateTo = model.ActualDeliveryDateTo;

            #endregion

            if (!ModelState.IsValid)
            {
                model = await _referralModelFactory.PrepareReferralReportModelAsync(model, model.Id);

                return View(model);
            }

            referral.Email = model.Email;
            referral.PhoneNumber = PhoneNumberFormatter.GetNumberOnly(model.PhoneNumber);
            referral.Note = model.Note;
            await _referralService.UpdateReferralAsync(referral);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Referral.Profile.Updated"));

            return RedirectToAction(nameof(this.Management), new
            {
                id = model.Id,
                actualDeliveryDateFrom = model.ActualDeliveryDateFrom,
                actualDeliveryDateTo = model.ActualDeliveryDateTo
            });
        }

        [HttpPost]
        public virtual async Task<IActionResult> ManagementList(ReferralOrderSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReferralManagements))
                return await AccessDeniedDataTablesJson();

            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReferralManagementsView))
                return await AccessDeniedDataTablesJson();

            var model = await _referralModelFactory.PrepareReferralOrderListModelAsync(searchModel);

            return Json(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> OrderReportAggregates(ReferralOrderSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReferralManagements))
                return await AccessDeniedDataTablesJson();

            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReferralManagementsView))
                return await AccessDeniedDataTablesJson();

            //prepare model
            var model = await _referralModelFactory.PrepareReferralOrderReportAggregatesAsync(searchModel);

            return Json(model);
        }

        public virtual async Task<IActionResult> Pay(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReferralManagements))
                return AccessDeniedView();

            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReferralManagementsView))
                return AccessDeniedView();

            var referralOrderMapping = await _referralService.GetReferralOrderMappingByIdAsync(id);
            if (referralOrderMapping == null || referralOrderMapping.Deleted)
                return RedirectToAction(nameof(this.Report));

            var referral = await _referralService.GetReferralByIdAsync(referralOrderMapping.ReferralId);
            if (referral == null)
                return RedirectToAction(nameof(this.Report));

            var order = await _orderService.GetOrderByOrderIdAsync(referralOrderMapping.OrderId);
            if (order == null)
                return RedirectToAction(nameof(this.Report));

            var model = await _referralModelFactory.PrepareReferralReportModelAsync(referralOrderMapping, referral, order);

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Pay(ReferralReportModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReferralManagements))
                return AccessDeniedView();

            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReferralManagementsView))
                return AccessDeniedView();

            var referralOrderMapping = await _referralService.GetReferralOrderMappingByIdAsync(model.ReferralOrderMappingId);
            if (referralOrderMapping == null || referralOrderMapping.Deleted)
                return RedirectToAction(nameof(this.Report));

            if (model.PaidAmountValue < 0)
            {
                ModelState.AddModelError(nameof(model.PaidAmountValue), await _localizationService.GetResourceAsync("Admin.Referral.Order.PaidAmount.Minimum.Required"));
            }
            else if (model.PaidAmountValue > model.DueAmountValue)
            {
                ModelState.AddModelError(nameof(model.PaidAmountValue), await _localizationService.GetResourceAsync("Admin.Referral.Order.PaidAmount.Maximum.Required"));
            }

            if (!ModelState.IsValid)
            {
                var referral = await _referralService.GetReferralByIdAsync(referralOrderMapping.ReferralId);
                if (referral == null)
                    return RedirectToAction(nameof(this.Report));

                var order = await _orderService.GetOrderByOrderIdAsync(referralOrderMapping.OrderId);
                if (order == null)
                    return RedirectToAction(nameof(this.Report));

                model = await _referralModelFactory.PrepareReferralReportModelAsync(referralOrderMapping, referral, order);

                return View(model);
            }

            referralOrderMapping.PaidAmount = model.PaidAmountValue + referralOrderMapping.PaidAmount;

            await _referralService.UpdateReferralOrderMappingAsync(referralOrderMapping);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Referral.Order.Pay.Success"));

            return RedirectToAction(nameof(this.Management), new { id = model.Id });
        }

        [HttpPost]
        public virtual async Task<IActionResult> DeleteSelected(ICollection<int> selectedIds)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReferralManagements))
                return await AccessDeniedDataTablesJson();

            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageReferralManagementsView))
                return await AccessDeniedDataTablesJson();

            if (selectedIds == null || !selectedIds.Any())
                return Json(new { error = await _localizationService.GetResourceAsync("Admin.Common.Alert.NothingSelected") });

            var referralOrderMappings = await _referralService.GetReferralOrderMappingsByIdsAsync(selectedIds.ToArray());

            var orderIds = referralOrderMappings.Select(rom => rom.OrderId).ToArray();
            var leadIds = await _orderService.GetListOfOrderLeadIdsFromOrderId(orderIds);

            var logisticsAccessorials = await _logisticsAccessorialService.GetAllLogisticsAccessorialsByLeadIdsAsync(leadIds);

            await _logisticsAccessorialService.DeleteLogisticsAccessorialsAsync(logisticsAccessorials);

            await _referralService.DeleteReferralOrderMappingsAsync(referralOrderMappings);

            return Json(new { Result = true });
        }

        #endregion

        #endregion
    }
}