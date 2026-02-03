using System;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Payments;
using Nop.Services.ScheduleTasks;

namespace ARM.Logistics.Payments.Square.Services
{
    /// <summary>
    /// Represents a schedule task to renew the access token
    /// </summary>
    public class RenewAccessTokenTask : IScheduleTask
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly SquarePaymentManager _squarePaymentManager;

        #endregion

        #region Ctor

        public RenewAccessTokenTask(ILocalizationService localizationService,
            ILogger logger,
            IPaymentPluginManager paymentPluginManager,
            ISettingService settingService,
            IStoreContext storeContext,
            SquarePaymentManager squarePaymentManager)
        {
            _localizationService = localizationService;
            _logger = logger;
            _paymentPluginManager = paymentPluginManager;
            _settingService = settingService;
            _storeContext = storeContext;
            _squarePaymentManager = squarePaymentManager;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Executes a task
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task ExecuteAsync()
        {
            //whether plugin is active
            if (!await _paymentPluginManager.IsPluginActiveAsync(SquarePaymentDefaults.SystemName))
                return;

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var settings = await _settingService.LoadSettingAsync<SquarePaymentSettings>(storeScope);

            //do not execute for sandbox environment
            if (settings.UseSandbox)
                return;

            try
            {
                //get the new access token
                var (newAccessToken, refreshToken) = await _squarePaymentManager.RenewAccessTokenAsync(storeScope);
                if (string.IsNullOrWhiteSpace(newAccessToken) || string.IsNullOrWhiteSpace(refreshToken))
                    throw new NopException("No service response");

                //if access token successfully received, save it for the further usage
                settings.AccessToken = newAccessToken;
                settings.RefreshToken = refreshToken;

                await _settingService.SaveSettingAsync(settings, x => x.AccessToken, storeScope, false);
                await _settingService.SaveSettingAsync(settings, x => x.RefreshToken, storeScope, false);

                await _settingService.ClearCacheAsync();

                //log information about the successful renew of the access token
                await _logger.InformationAsync(await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.RenewAccessToken.Success"));
            }
            catch (Exception exception)
            {
                //log error on renewing of the access token
                await _logger.ErrorAsync(await _localizationService.GetResourceAsync("ARM.Logistics.Payments.Square.RenewAccessToken.Error"), exception);
            }
        }

        #endregion
    }
}