using System;
using System.Collections.Generic;
using ARM.Logistics.Payments.Square.Domain;
using Nop.Data.Mapping;

namespace ARM.Logistics.Payments.Square.Data.Mapping
{
    /// <summary>
    /// Base instance of backward compatibility of table naming
    /// </summary>
    public partial class SquareBaseNameCompatibility : INameCompatibility
    {
        public Dictionary<Type, string> TableNames => new()
        {
            { typeof(SquareOrderMapping), "Square_Order_Mapping" },
            { typeof(SquareTransactionOrderMapping), "Square_Transaction_Order_Mapping" },
            { typeof(SquareOrderItem), "Square_OrderItem" },
        };

        public Dictionary<(Type, string), string> ColumnName => new() { };
    }
}