using System.Collections.Generic;
using FluentMigrator;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Data.Migrations;
using Nop.Services.Localization;
using Nop.Web.Framework.Extensions;

namespace ARM.Logistics.Payments.Square.Data.Migrations
{
    [NopMigration("2025/07/09 12:13:12", "Quickbook Localization", UpdateMigrationType.Localization, MigrationProcessType.Update)]
    public class SquareLocalizationMigration : AutoReversingMigration
    {
        #region Methods

        public override void Up()
        {
            if (!DataSettingsManager.IsDatabaseInstalled())
                return;

            //do not use DI, because it produces exception on the installation process
            var localizationService = EngineContext.Current.Resolve<ILocalizationService>();
            var (languageId, _) = this.GetLanguageData();

            #region Add or update locales

            localizationService.AddOrUpdateLocaleResource(new Dictionary<string, string>
            {
                ["ARM.Logistics.Payments.Square.Invoice.Field.Button.SendSquareInvoice"] = "Create Square Invoice",
                ["ARM.Logistics.Payments.Square.Invoice.Field.Button.SendSquareInvoice.Text"] = "Send Square Invoice",
                ["ARM.Logistics.Payments.Square.Invoice.Note.SubOrder.FullAmount.Create"] = "Are you sure you want to create an invoice for amount <strong>{0} ({1})</strong>?",
                ["ARM.Logistics.Payments.Square.Invoice.PublicUrl.Generate"] = "Invoice link has been generated successfully for invoice number {0}.",
                ["ARM.Logistics.Payments.Square.Fields.AutomaticSentInvoice"] = "Automatic Sent Invoice",
                ["ARM.Logistics.Payments.Square.Fields.AutomaticSentInvoice.Hint"] = "Check if sent invoice to the customer automatically."
            }, languageId);

            #endregion
        }

        #endregion
    }
}
