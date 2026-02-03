using ARM.Logistics.Payments.Square.Factories;
using ARM.Logistics.Payments.Square.Services;
using ARM.Logistics.Payments.Square.Services.Messages;
using ARM.Logistics.Payments.Square.Services.SquareOrderMappings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Web.Framework.Infrastructure.Extensions;

namespace ARM.Logistics.Payments.Square.Infrastructure
{
    /// <summary>
    /// Represents object for the configuring services on application startup
    /// </summary>
    public class NopStartup : INopStartup
    {
        /// <summary>
        /// Add and configure any of the middleware
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="configuration">Configuration of the application</param>
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            //services
            services.AddScoped<ISquarePaymentMessageService, SquarePaymentMessageService>();
            services.AddScoped<ISquareOrderMappingService, SquareOrderMappingService>();
            services.AddScoped<SquarePaymentManager>();

            //client to request Square authorization service
            services.AddHttpClient<SquareAuthorizationHttpClient>().WithProxy();

            //factories
            services.AddScoped<IInvoiceModelFactory, InvoiceModelFactory>();
        }

        /// <summary>
        /// Configure the using of added middleware
        /// </summary>
        /// <param name="application">Builder for configuring an application's request pipeline</param>
        public void Configure(IApplicationBuilder application)
        {
        }

        /// <summary>
        /// Gets order of this startup configuration implementation
        /// </summary>
        public int Order => 101;
    }
}