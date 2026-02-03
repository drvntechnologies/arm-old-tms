using System;
using System.Linq;
using ARM.Logistics.Payments.Square.Security;
using FluentMigrator;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Security;
using Nop.Data;
using Nop.Data.Migrations;

namespace ARM.Logistics.Payments.Square.Data.Migrations
{
    [NopMigration("2025/06/17 18:22:00", "Square Permission Migration", MigrationProcessType.Update)]
    public class SquarePermissionMigration : Migration
    {
        #region Field

        private readonly INopDataProvider _dataProvider;

        #endregion

        #region Ctor

        public SquarePermissionMigration(INopDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        #endregion

        #region Utility

        private void InsertPermissionRecordCustomerRoleMapping(CustomerRole customerRole, PermissionRecord permissionRecord)
        {
            if (customerRole == null || permissionRecord == null)
            {
                return;
            }

            _dataProvider.InsertEntity(new PermissionRecordCustomerRoleMapping
            {
                CustomerRoleId = customerRole.Id,
                PermissionRecordId = permissionRecord.Id
            });
        }

        #endregion

        #region Methods

        public override void Up()
        {
            if (!DataSettingsManager.IsDatabaseInstalled())
            {
                return;
            }

            //add it to the Admin role by default
            var adminRole = _dataProvider
                .GetTable<CustomerRole>()
                .FirstOrDefault(x => x.IsSystemRole && x.SystemName == NopCustomerDefaults.AdministratorsRoleName);

            var permissionRecordTable = _dataProvider.GetTable<PermissionRecord>();

            var permission = SquarePermissionProvider.ManageSquareInvoice;

            if (!permissionRecordTable.Any(pr => string.Compare(pr.SystemName, permission.SystemName, StringComparison.InvariantCultureIgnoreCase) == 0))
            {
                var permissionRecord = new PermissionRecord
                {
                    Name = permission.Name,
                    SystemName = permission.SystemName,
                    Category = permission.Category
                };

                _dataProvider.InsertEntity(permissionRecord);

                InsertPermissionRecordCustomerRoleMapping(adminRole, permissionRecord);
            }
        }

        public override void Down()
        {
        }

        #endregion
    }
}
