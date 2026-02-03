using ARM.Logistics.Payments.Square.Domain;
using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;

namespace ARM.Logistics.Payments.Square.Data.Mapping.Builders
{
    /// <summary>
    /// Represents a square order mapping builder
    /// </summary>
    public partial class SquareOrderMappingBuilder : NopEntityBuilder<SquareOrderMapping>
    {
        #region Method

        /// <summary>
        /// Apply entity configuration
        /// </summary>
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(SquareOrderMapping.OrderId)).AsInt32().NotNullable()
                .WithColumn(nameof(SquareOrderMapping.SquareOrderId)).AsString(200).Nullable()
                .WithColumn(nameof(SquareOrderMapping.SquareInvoiceId)).AsString(200).Nullable()
                .WithColumn(nameof(SquareOrderMapping.SquareInvoiceNumber)).AsString(200).Nullable()
                .WithColumn(nameof(SquareOrderMapping.OrderSubTotalAmount)).AsDecimal(18, 4).NotNullable().WithDefaultValue(decimal.Zero)
                .WithColumn(nameof(SquareOrderMapping.InvoiceStatusId)).AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn(nameof(SquareOrderMapping.InvoiceAmountTypeId)).AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn(nameof(SquareOrderMapping.InvoiceLink)).AsString(2000).Nullable()
                .WithColumn(nameof(SquareOrderMapping.CreatedOnUtc)).AsDateTime2().NotNullable()
                .WithColumn(nameof(SquareOrderMapping.UpdatedOnUtc)).AsDateTime2().NotNullable()
                .WithColumn(nameof(SquareOrderMapping.PaidDateOnUtc)).AsDateTime2().Nullable()
                .WithColumn(nameof(SquareOrderMapping.RefundDateOnUtc)).AsDateTime2().Nullable()
                .WithColumn(nameof(SquareOrderMapping.Deleted)).AsBoolean().NotNullable().WithDefaultValue(false);
        }

        #endregion
    }
}