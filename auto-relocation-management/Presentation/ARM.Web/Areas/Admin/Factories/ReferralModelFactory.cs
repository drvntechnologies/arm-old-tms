using System;
using System.Linq;
using System.Threading.Tasks;
using ARM.Web.Areas.Admin.Models.Referrals;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.LogisticsQuotes;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.QuotesStatus;
using Nop.Core.Domain.Referrals;
using Nop.Core.Infrastructure;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.LogisticsQuotes;
using Nop.Services.Orders;
using Nop.Services.QuotesStatus;
using Nop.Services.Referrals;
using Nop.Web.Areas.Admin.Models.Common;
using Nop.Web.Framework.Models.Extensions;

namespace ARM.Web.Areas.Admin.Factories
{
    /// <summary>
    /// Represents the referral model factory implementation
    /// </summary>
    public partial class ReferralModelFactory : IReferralModelFactory
    {
        #region Fields

        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IReferralService _referralService;
        private readonly IOrderService _orderService;
        private readonly ILogisticsStatusService _logisticsStatusService;
        private readonly ILogisticsAccessorialService _logisticsAccessorialService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IAddressService _addressService;
        private readonly ILocalizationService _localizationService;

        #endregion

        #region Ctor

        public ReferralModelFactory(IWorkContext workContext,
            IStoreContext storeContext,
            IReferralService referralService,
            IOrderService orderService,
            ILogisticsStatusService logisticsStatusService,
            ILogisticsAccessorialService logisticsAccessorialService,
            IPriceFormatter priceFormatter,
            IAddressService addressService,
            ILocalizationService localizationService)
        {
            _workContext = workContext;
            _storeContext = storeContext;
            _referralService = referralService;
            _orderService = orderService;
            _logisticsStatusService = logisticsStatusService;
            _logisticsAccessorialService = logisticsAccessorialService;
            _priceFormatter = priceFormatter;
            _addressService = addressService;
            _localizationService = localizationService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Prepare referral search model
        /// </summary>
        /// <param name="searchModel">Referral search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the referral search model
        /// </returns>
        public virtual Task<ReferralSearchModel> PrepareReferralSearchModelAsync(ReferralSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            return Task.FromResult(searchModel);
        }

        /// <summary>
        /// Prepare paged referral list model
        /// </summary>
        /// <param name="searchModel">Referral search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the referral list model
        /// </returns>
        public virtual async Task<ReferralListModel> PrepareReferralListModelAsync(ReferralSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get referrals
            var referrals = await _referralService.GetReferralsAsync(name: searchModel.Name,
                showHidden: true,
                pageIndex: searchModel.Page - 1,
                pageSize: searchModel.PageSize);

            //prepare list model
            var model = new ReferralListModel().PrepareToGrid(searchModel, referrals, () =>
            {
                return referrals.Select(referral =>
                {
                    return new ReferralModel()
                    {
                        Name = referral.Name,
                        Id = referral.Id,
                        Active = referral.Active,
                    };
                });
            });

            return model;
        }

        #region Referral report or management

        /// <summary>
        /// Prepare referral report search model
        /// </summary>
        /// <param name="searchModel">Referral report search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the referral report search model
        /// </returns>
        public virtual Task<ReferralReportSearchModel> PrepareReferralReportSearchModelAsync(ReferralReportSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            return Task.FromResult(searchModel);
        }

        /// <summary>
        /// Prepare referral report list model
        /// </summary>
        /// <param name="searchModel">Referral report search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the referral report list model
        /// </returns>
        public virtual async Task<ReferralReportListModel> PrepareReferralReportListModelAsync(ReferralReportSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            var startDateFrom = !searchModel.ActualDeliveryDateFrom.HasValue ? null
                : (DateTime?)searchModel.ActualDeliveryDateFrom.Value;

            var endDateTo = !searchModel.ActualDeliveryDateTo.HasValue ? null
                : (DateTime?)searchModel.ActualDeliveryDateTo.Value.AddDays(1);

            var orderStatusIds = (searchModel.OrderStatusIds?.Any(statusId => statusId == 0) ?? true) ? null : searchModel.OrderStatusIds.ToList();

            DTOrder orderby = new DTOrder();
            orderby.Column = 0;
            orderby.Dir = "asc";
            if (searchModel.Order != null)
                orderby = searchModel.Order.FirstOrDefault();

            var referrals = await _referralService.GetReferralsAsync(name: searchModel.ReferPayee,
                showHidden: true,
                pageIndex: searchModel.Page - 1,
                pageSize: searchModel.PageSize,
                osIds: orderStatusIds,
                actualDeliveryDateFrom: startDateFrom,
                actualDeliveryDateTo: endDateTo,
                orderBy: orderby != null ? orderby.Dir : "",
                column: orderby != null ? orderby.Column : 0);

            var statusId = (await _logisticsStatusService.GetAllLogisticsStatusAsync(nameof(StatusType.Order)))
                .FirstOrDefault(ls => ls.Title?.Equals(LogisticsStatusDefaults.Order.Delivered, StringComparison.InvariantCultureIgnoreCase) == true)?.Id ?? 0;

            //prepare list model
            var model = await new ReferralReportListModel().PrepareToGridAsync(searchModel, referrals, () =>
            {
                //fill in model values from the entity
                return referrals.SelectAwait(async referral =>
                {
                    ReferralReportModel model = new()
                    {
                        Id = referral.Id,
                        ReferPayee = $"<a href=\"/Admin/Referral/Management/{referral.Id}\" class=\"aUnderline refer-payee\">{referral.Name}</a>"
                    };

                    var totalAmountTask = _orderService.CalculateTotalAmountForReferralAsync(referral.Name, statusId: statusId, actualDeliveryDateFrom: startDateFrom, actualDeliveryDateTo: endDateTo);

                    var paidAmountTask = _referralService.CalculateTotalPaidAmountForReferralAsync(referral.Id, statusId: statusId, actualDeliveryDateFrom: startDateFrom, actualDeliveryDateTo: endDateTo);

                    await Task.WhenAll(totalAmountTask, paidAmountTask);

                    var totalAmount = await totalAmountTask;
                    var paidAmount = await paidAmountTask;

                    model.DueAmountValue = totalAmount - paidAmount;
                    model.DueAmount = await _priceFormatter.FormatPriceAsync(model.DueAmountValue, true, false);

                    return model;
                });
            });

            return model;
        }

        /// <summary>
        /// Prepares the referral report model asynchronous.
        /// </summary>
        /// <param name="model">The referral model.</param>
        /// <param name="referralId">The referral identifier.</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the referral report model
        /// </returns>
        public virtual async Task<ReferralReportModel> PrepareReferralReportModelAsync(ReferralReportModel model, int referralId)
        {
            var referral = await _referralService.GetReferralByIdAsync(referralId)
                ?? throw new ArgumentException("No referral found with the specified id");

            model ??= new ReferralReportModel()
            {
                Id = referral.Id,
                ReferPayee = referral.Name,
                Email = referral.Email,
                PhoneNumber = referral.PhoneNumber,
                Note = referral.Note,
            };

            #region Prepare order search model

            #region Get filtering context

            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();

            var customer = await _workContext.GetCurrentCustomerAsync();
            var storeId = (await _storeContext.GetCurrentStoreAsync()).Id;

            model.ReferralOrderSearchModel.ActualDeliveryDateFrom = await genericAttributeService.GetAttributeAsync<DateTime?>(customer, ReferralManagementSearchDefaults.ActualDeliveryDateFrom, storeId);

            model.ReferralOrderSearchModel.ActualDeliveryDateTo = await genericAttributeService.GetAttributeAsync<DateTime?>(customer, ReferralManagementSearchDefaults.ActualDeliveryDateTo, storeId);

            model.ReferralOrderSearchModel.FirstPickupDate = await genericAttributeService.GetAttributeAsync<DateTime?>(customer, ReferralManagementSearchDefaults.FirstPickupDate, storeId);

            model.ReferralOrderSearchModel.LastPickupDate = await genericAttributeService.GetAttributeAsync<DateTime?>(customer, ReferralManagementSearchDefaults.LastPickupDate, storeId);

            var orderStatusIds = await genericAttributeService.GetAttributeAsync<string>(customer, ReferralManagementSearchDefaults.OrderStatusIds, storeId);

            if (!string.IsNullOrWhiteSpace(orderStatusIds))
            {
                model.ReferralOrderSearchModel.OrderStatusIds = orderStatusIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Where(id => int.TryParse(id, out _))
                    .Select(int.Parse).ToList();
            }

            #endregion

            var orderStatuses = await _logisticsStatusService.GetAllLogisticsStatusAsync(nameof(StatusType.Order));

            if (orderStatuses?.Count > 0)
            {
                var hasStatusIds = (model.ReferralOrderSearchModel.OrderStatusIds?.Any() ?? false);

                foreach (var status in orderStatuses)
                {
                    if (status.Title?.Equals(LogisticsStatusDefaults.Order.Canceled, StringComparison.InvariantCultureIgnoreCase) == true)
                    {
                        continue;
                    }

                    if (status.Title?.Equals(LogisticsStatusDefaults.Order.Delivered, StringComparison.InvariantCultureIgnoreCase) == true)
                    {
                        model.ReferralOrderSearchModel.DeliveredOrderStatusId = status.Id;

                        if (!hasStatusIds)
                        {
                            model.ReferralOrderSearchModel.OrderStatusIds?.Add(status.Id);
                        }
                    }

                    model.ReferralOrderSearchModel.AvailableOrderStatuses.Add(new SelectListItem
                    {
                        Text = status.Title,
                        Value = status.Id.ToString()
                    });
                }

                model.ReferralOrderSearchModel.AvailableOrderStatuses.Insert(0, new SelectListItem
                {
                    Text = await _localizationService.GetResourceAsync("Admin.Common.SelectAll"),
                    Value = "0"
                });
            }

            if (model.ReferralOrderSearchModel.AvailableOrderStatuses.Any())
            {
                if (model.ReferralOrderSearchModel.OrderStatusIds?.Any() ?? false)
                {
                    var ids = model.ReferralOrderSearchModel.OrderStatusIds.Select(id => id.ToString());
                    var statusItems = model.ReferralOrderSearchModel.AvailableOrderStatuses.Where(statusItem => ids.Contains(statusItem.Value)).ToList();
                    foreach (var statusItem in statusItems)
                    {
                        statusItem.Selected = true;
                    }
                }
                else
                    model.ReferralOrderSearchModel.AvailableOrderStatuses.FirstOrDefault().Selected = true;
            }

            model.ReferralOrderSearchModel.ReferralId = referral.Id;
            model.ReferralOrderSearchModel.ReferralName = referral.Name;

            //prepare page parameters
            model.ReferralOrderSearchModel.SetGridPageSize();

            #endregion

            return model;
        }

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
        public virtual async Task<ReferralReportModel> PrepareReferralReportModelAsync(ReferralOrderMapping referralOrderMapping, Referral referral, Order order)
        {
            ReferralReportModel model = new();

            if (referralOrderMapping == null || referralOrderMapping.Deleted || referral == null || order == null)
                return model;

            model.Id = referral.Id;
            model.OrderId = order.Id;
            model.CustomOrderNumber = order.CustomOrderNumber;
            model.ReferralOrderMappingId = referralOrderMapping.Id;
            model.ReferPayee = referral.Name;

            var customerInfo = await _addressService.GetAddressByIdAsync(order.BillingAddressId);
            if (customerInfo != null)
                model.CustomerName = $"{customerInfo.FirstName} {customerInfo.LastName}".Trim();

            //Calculate total amount
            model.AmountValue = await _logisticsAccessorialService.TotalAmountForLogisticsAccessorialAsync(leadId: order.LeadId, accessorialTypeId: (int)AccessorialType.Referral);
            model.Amount = await _priceFormatter.FormatPriceAsync(model.AmountValue, true, false);

            model.DueAmountValue = model.AmountValue - referralOrderMapping.PaidAmount;
            model.DueAmount = await _priceFormatter.FormatPriceAsync(model.DueAmountValue, true, false);

            return model;
        }

        /// <summary>
        /// Prepare referral order list model
        /// </summary>
        /// <param name="searchModel">Referral order search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the referral order list model
        /// </returns>
        public virtual async Task<ReferralOrderListModel> PrepareReferralOrderListModelAsync(ReferralOrderSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            var statuses = (await _logisticsStatusService.GetAllLogisticsStatusAsync(nameof(StatusType.Order)))
                .Where(ls => !string.IsNullOrWhiteSpace(ls.Title) &&
                            !ls.Title.Equals(LogisticsStatusDefaults.Order.Canceled, StringComparison.InvariantCultureIgnoreCase));

            var startDateFrom = !searchModel.ActualDeliveryDateFrom.HasValue ? null
                : (DateTime?)searchModel.ActualDeliveryDateFrom.Value;

            var endDateTo = !searchModel.ActualDeliveryDateTo.HasValue ? null
                : (DateTime?)searchModel.ActualDeliveryDateTo.Value.AddDays(1);

            var orderStatusIds = (searchModel.OrderStatusIds?.Any(statusId => statusId == 0) ?? true) ? null : searchModel.OrderStatusIds.ToList();

            if (orderStatusIds == null || !orderStatusIds.Any())
            {
                orderStatusIds = statuses.Select(s => s.Id).ToList();
            }

            #region Set filtering context

            var customer = await _workContext.GetCurrentCustomerAsync();
            var storeId = (await _storeContext.GetCurrentStoreAsync()).Id;
            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();

            await genericAttributeService.SaveAttributeAsync(customer, ReferralManagementSearchDefaults.ActualDeliveryDateFrom, searchModel.ActualDeliveryDateFrom, storeId);
            await genericAttributeService.SaveAttributeAsync(customer, ReferralManagementSearchDefaults.ActualDeliveryDateTo, searchModel.ActualDeliveryDateTo, storeId);
            await genericAttributeService.SaveAttributeAsync(customer, ReferralManagementSearchDefaults.FirstPickupDate, searchModel.FirstPickupDate, storeId);
            await genericAttributeService.SaveAttributeAsync(customer, ReferralManagementSearchDefaults.LastPickupDate, searchModel.LastPickupDate, storeId);
            await genericAttributeService.SaveAttributeAsync(customer, ReferralManagementSearchDefaults.OrderStatusIds, searchModel.OrderStatusIds != null ? string.Join(',', searchModel.OrderStatusIds) : string.Empty, storeId);

            #endregion

            var referralOrderMappings = await _referralService.GetAllReferralOrderMappingsAsync(referralId: searchModel.ReferralId,
                osIds: orderStatusIds,
                actualDeliveryDateFrom: startDateFrom,
                actualDeliveryDateTo: endDateTo,
                firstPickupDate: searchModel.FirstPickupDate,
                lastPickupDate: searchModel.LastPickupDate,
                pageIndex: searchModel.Page - 1,
                pageSize: searchModel.PageSize);

            //prepare list model
            var model = await new ReferralOrderListModel().PrepareToGridAsync(searchModel, referralOrderMappings, () =>
            {
                //fill in model values from the entity
                return referralOrderMappings.SelectAwait(async referralOrderMapping =>
                {
                    ReferralOrderModel model = new()
                    {
                        Id = referralOrderMapping.Id,
                    };

                    var order = await _orderService.GetOrderByOrderIdAsync(referralOrderMapping.OrderId);
                    if (order != null)
                    {
                        var isNewOrder = _orderService.IsNewOrder(order);
                        model.OrderId = order.LeadId;
                        model.OrderNumber = order.CustomOrderNumber;

                        var customerInfo = await _addressService.GetAddressByIdAsync(order.PickupAddressId.HasValue ? order.PickupAddressId.Value : 0);

                        if (customerInfo != null)
                        {
                            if (!string.IsNullOrWhiteSpace(customerInfo.ContactName))
                            {
                                model.CustomerName = customerInfo.ContactName;
                            }
                            else
                            {
                                model.CustomerName = $"{customerInfo.FirstName} {customerInfo.LastName}".Trim();
                            }
                        }

                        var actualDeliveryDate = order.ActualDeliveryDate;

                        if (isNewOrder)
                        {
                            var subOrders = (await _orderService.GetAllLogisticsChildOrderMappingsAsync(order.Id))?.LastOrDefault();
                            var subOrder = await _orderService.GetOrderByOrderIdAsync(subOrders?.ChildOrderId ?? 0);
                            var orderDetails = subOrder != null ? subOrder : order;
                            actualDeliveryDate = orderDetails.ActualDeliveryDate;
                        }

                        model.DeliveryDate = actualDeliveryDate.ToDateFormat();

                        model.Status = (await _logisticsStatusService.GetLogisticsStatusByIdAsync(order.StatusId))?.Title ?? string.Empty;

                        model.AmountValue = await _logisticsAccessorialService.TotalAmountForLogisticsAccessorialAsync(leadId: order.LeadId, accessorialTypeId: (int)AccessorialType.Referral);
                        model.Amount = await _priceFormatter.FormatPriceAsync(model.AmountValue, true, false);
                    }

                    model.DueAmountValue = model.AmountValue - referralOrderMapping.PaidAmount;

                    model.DueAmount = await _priceFormatter.FormatPriceAsync(model.DueAmountValue, true, false);

                    return model;
                });
            });

            return model;
        }

        /// <summary>
        /// Prepare referral order report aggregates
        /// </summary>
        /// <param name="searchModel">Referral order search model</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the referral order report aggregates
        /// </returns>
        public virtual async Task<ReferralOrderAggreratorModel> PrepareReferralOrderReportAggregatesAsync(ReferralOrderSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            var statuses = (await _logisticsStatusService.GetAllLogisticsStatusAsync(nameof(StatusType.Order)))
                .Where(ls => !string.IsNullOrWhiteSpace(ls.Title) &&
                            !ls.Title.Equals(LogisticsStatusDefaults.Order.Canceled, StringComparison.InvariantCultureIgnoreCase));

            var startDateFrom = !searchModel.ActualDeliveryDateFrom.HasValue ? null
                : (DateTime?)searchModel.ActualDeliveryDateFrom.Value;

            var endDateTo = !searchModel.ActualDeliveryDateTo.HasValue ? null
                : (DateTime?)searchModel.ActualDeliveryDateTo.Value.AddDays(1);

            var orderStatusIds = (searchModel.OrderStatusIds?.Any(statusId => statusId == 0) ?? true) ? null : searchModel.OrderStatusIds.ToList();

            if (orderStatusIds == null || !orderStatusIds.Any())
            {
                orderStatusIds = statuses.Select(s => s.Id).ToList();
            }

            var referralOrderMappings = await _referralService.GetAllReferralOrderMappingsAsync(referralId: searchModel.ReferralId,
                osIds: orderStatusIds,
                actualDeliveryDateFrom: startDateFrom,
                actualDeliveryDateTo: endDateTo,
                firstPickupDate: searchModel.FirstPickupDate,
                lastPickupDate: searchModel.LastPickupDate);

            var totalUnpaidAmount = decimal.Zero;
            var totalPaidAmount = decimal.Zero;
            var totalAmount = decimal.Zero;

            foreach (var referralOrderMapping in referralOrderMappings)
            {
                var order = await _orderService.GetOrderByOrderIdAsync(referralOrderMapping.OrderId);
                if (order == null)
                    continue;

                var totalAmountValue = await _logisticsAccessorialService.TotalAmountForLogisticsAccessorialAsync(leadId: order.LeadId, accessorialTypeId: (int)AccessorialType.Referral);

                totalUnpaidAmount += totalAmountValue - referralOrderMapping.PaidAmount;
                totalPaidAmount += referralOrderMapping.PaidAmount;
                totalAmount += totalAmountValue;
            }

            ReferralOrderAggreratorModel model = new()
            {
                AggregatorUnpaidAmountTotal = await _priceFormatter.FormatPriceAsync(totalUnpaidAmount, true, false),
                AggregatorPaidAmountTotal = await _priceFormatter.FormatPriceAsync(totalPaidAmount, true, false),
                AggregatorTotal = await _priceFormatter.FormatPriceAsync(totalAmount, true, false)
            };

            return model;
        }

        #endregion

        #endregion
    }
}