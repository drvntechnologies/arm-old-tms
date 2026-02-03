using ARM.Logistics.Payments.Square.Domain;
using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;

namespace ARM.Logistics.Payments.Square.Data.Mapping.Builders
{
    /// <summary>
    /// Represents a square transaction order mapping builder
    /// </summary>
    public partial class SquareTransactionOrderMappingBuilder : NopEntityBuilder<SquareTransactionOrderMapping>
    {
        #region Method

        /// <summary>
        /// Apply entity configuration
        /// </summary>
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(SquareTransactionOrderMapping.OrderId)).AsInt32().NotNullable()
                .WithColumn(nameof(SquareTransactionOrderMapping.SquareOrderMappingId)).AsInt32().NotNullable()
                .WithColumn(nameof(SquareTransactionOrderMapping.PaymentId)).AsString(200).NotNullable()
                .WithColumn(nameof(SquareTransactionOrderMapping.TransactionId)).AsString(200).Nullable()
                .WithColumn(nameof(SquareTransactionOrderMapping.PaidDateTimeOnUtc)).AsDateTime2().Nullable()
                .WithColumn(nameof(SquareTransactionOrderMapping.RefundDateOnUtc)).AsDateTime2().Nullable();
        }

        #endregion
    }
}