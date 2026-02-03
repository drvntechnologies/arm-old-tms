using System.Collections.Generic;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Security;
using Nop.Services.Security;

namespace ARM.Logistics.Payments.Square.Security
{
    /// <summary>
    /// Square permission provider
    /// </summary>
    public partial class SquarePermissionProvider : IPermissionProvider
    {
        public static readonly PermissionRecord ManageSquareInvoice = new() { Name = "Admin area. Manage Square Invoice", SystemName = "ManageSquareInvoice", Category = "Square" };

        /// <summary>
        /// Get permissions
        /// </summary>
        /// <returns>Permissions</returns>
        public virtual IEnumerable<PermissionRecord> GetPermissions()
        {
            return new[]
            {
                ManageSquareInvoice
            };
        }

        /// <summary>
        /// Get default permissions
        /// </summary>
        /// <returns>Permissions</returns>
        public virtual HashSet<(string systemRoleName, PermissionRecord[] permissions)> GetDefaultPermissions()
        {
            return new HashSet<(string, PermissionRecord[])>
            {
                (
                    NopCustomerDefaults.AdministratorsRoleName,
                    new[]
                    {
                        ManageSquareInvoice
                    }
                )
            };
        }
    }
}
