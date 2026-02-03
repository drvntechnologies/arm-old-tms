using ARM.Logistics.Payments.Square.Domain;
using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;

namespace ARM.Logistics.Payments.Square.Data.Mapping.Builders
{
    /// <summary>
    /// Represents a square order item builder
    /// </summary>
    public partial class SquareOrderItemBuilder : NopEntityBuilder<SquareOrderItem>
    {
        #region Method

        /// <summary>
        /// Apply entity configuration
        /// </summary>
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(SquareOrderItem.SquareOrderMappingId)).AsInt32().NotNullable()
                .WithColumn(nameof(SquareOrderItem.Name)).AsString(2000).Nullable()
                .WithColumn(nameof(SquareOrderItem.Amount)).AsDecimal(18, 4).NotNullable().WithDefaultValue(decimal.Zero);
        }

        #endregion
    }
}