using System;
using System.Threading.Tasks;
using Nop.Core.Infrastructure;
using Nop.Services.Catalog;

namespace ARM.Logistics.Payments.Square.Services
{
    public static class SquarePaymentHelper
    {
        #region Fields

        private const int _convertAmount = 100;
        private static readonly IPriceCalculationService _priceCalculationService = EngineContext.Current.Resolve<IPriceCalculationService>();

        #endregion

        #region Methods

        #region Common

        public static async Task<decimal> RoundAsync(decimal amount)
        {
            return await _priceCalculationService.RoundPriceAsync(amount);
        }

        #endregion

        #region Dollars To Cents Conversion

        public static async Task<long> DollarsToCentsAsync(decimal amount)
        {
            return await DollarsToCentsAsync(amount, true);
        }

        public static async Task<long> DollarsToCentsAsync(decimal amount, bool roundPrice)
        {
            var price = roundPrice ? await RoundAsync(amount) : amount;

            var amountInCents = price * _convertAmount;

            return (long)amountInCents;
        }

        #endregion

        #region Cents To Dollars Conversion

        public static async Task<decimal> CentsToDollarsAsync(decimal amount)
        {
            return await CentsToDollarsAsync(amount, true);
        }

        public static async Task<decimal> CentsToDollarsAsync(decimal amount, bool roundPrice)
        {
            var amountInDecimal = amount / Convert.ToDecimal(_convertAmount);

            if (roundPrice)
            {
                return await RoundAsync(amountInDecimal);
            }

            return amountInDecimal;
        }

        #endregion

        #endregion
    }
}
