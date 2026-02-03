using ARM.Logistics.Payments.Square.Domain;
using FluentMigrator;
using Nop.Data;
using Nop.Data.Extensions;
using Nop.Data.Mapping;
using Nop.Data.Migrations;

namespace ARM.Logistics.Payments.Square.Data.Migrations
{
    [NopMigration("2024/04/10 18:22:00", "Square Payment Table Create", MigrationProcessType.Installation)]
    public class SquarePaymentMigration : AutoReversingMigration
    {
        #region Methods

        public override void Up()
        {
            if (!DataSettingsManager.IsDatabaseInstalled())
            {
                return;
            }

            if (!Schema.Table(NameCompatibilityManager.GetTableName(typeof(SquareOrderMapping))).Exists())
            {
                Create.TableFor<SquareOrderMapping>();
            }

            if (!Schema.Table(NameCompatibilityManager.GetTableName(typeof(SquareTransactionOrderMapping))).Exists())
            {
                Create.TableFor<SquareTransactionOrderMapping>();
            }

            if (!Schema.Table(NameCompatibilityManager.GetTableName(typeof(SquareOrderItem))).Exists())
            {
                Create.TableFor<SquareOrderItem>();
            }
        }

        #endregion
    }
}
