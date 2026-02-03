using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ARM.Logistics.Payments.Square.Domain;
using Nop.Core.Domain.Companies;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.QuotesStatus;
using Nop.Data;
using Nop.Services.Orders;

namespace ARM.Logistics.Payments.Square.Services.SquareOrderMappings
{
    /// <summary>
    /// Square order mapping service
    /// </summary>
    public partial class SquareOrderMappingService : ISquareOrderMappingService
    {
        #region Fields

        private readonly IRepository<SquareOrderMapping> _squareOrderMappingRepository;
        private readonly IRepository<SquareTransactionOrderMapping> _squareTransactionOrderMappingRepository;
        private readonly IRepository<SquareOrderItem> _squareOrderItemRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<LogisticsStatus> _logisticsStatusRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<Company> _companyRepository;
        private readonly IRepository<LogisticsChildOrderMapping> _logisticsChildOrderMappingRepository;

        #endregion

        #region Ctor

        public SquareOrderMappingService(IRepository<SquareOrderMapping> squareOrderMappingRepository,
            IRepository<SquareTransactionOrderMapping> squareTransactionOrderMappingRepository,
            IRepository<SquareOrderItem> squareOrderItemRepository,
            IRepository<Order> orderRepository,
            IRepository<LogisticsStatus> logisticsStatusRepository,
            IRepository<Customer> customerRepository,
            IRepository<Company> companyRepository,
            IRepository<LogisticsChildOrderMapping> logisticsChildOrderMappingRepository)
        {
            _squareOrderMappingRepository = squareOrderMappingRepository;
            _squareTransactionOrderMappingRepository = squareTransactionOrderMappingRepository;
            _squareOrderItemRepository = squareOrderItemRepository;
            _orderRepository = orderRepository;
            _logisticsStatusRepository = logisticsStatusRepository;
            _customerRepository = customerRepository;
            _companyRepository = companyRepository;
            _logisticsChildOrderMappingRepository = logisticsChildOrderMappingRepository;
        }

        #endregion

        #region Methods

        #region Square Order Mapping

        /// <summary>
        /// Gets a square order mapping
        /// </summary>
        /// <param name="squareOrderId">The square order identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the square order mapping
        /// </returns>
        public virtual async Task<SquareOrderMapping> GetSquareOrderMappingBySquareOrderIdAsync(string squareOrderId)
        {
            if (string.IsNullOrWhiteSpace(squareOrderId))
                return null;

            var query = from q in _squareOrderMappingRepository.Table
                        where q.SquareOrderId == squareOrderId && !q.Deleted
                        orderby q.Id
                        select q;

            return await query.FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets a paid square order mapping
        /// </summary>
        /// <param name="orderId">The order identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of square order mappings
        /// </returns>
        public virtual async Task<IList<SquareOrderMapping>> GetPaidSquareOrderMappingByOrderIdAsync(int orderId)
        {
            if (orderId <= 0)
            {
                return Enumerable.Empty<SquareOrderMapping>().ToList();
            }

            var query = from q in _squareOrderMappingRepository.Table
                        where q.OrderId == orderId && !q.Deleted &&
                              q.PaidDateOnUtc.HasValue
                        orderby q.Id
                        select q;

            return await query.ToListAsync();
        }

        /// <summary>
        /// check a paid square order mapping
        /// </summary>
        /// <param name="orderId">The order identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the boolean value for checking the order invoice.
        /// </returns>
        public virtual async Task<bool> HasPaidSquareOrderMappingByOrderIdAsync(int orderId)
        {
            if (orderId <= 0)
                return false;

            var query = from q in _squareOrderMappingRepository.Table
                        where q.OrderId == orderId && !q.Deleted &&
                            (q.PaidDateOnUtc.HasValue ||
                            q.InvoiceStatusId == (int)InvoiceStatus.Partially_Paid ||
                            q.InvoiceStatusId == (int)InvoiceStatus.Paid)
                        orderby q.Id
                        select q;

            return await query.AnyAsync();
        }

        /// <summary>
        /// Gets a square order mapping
        /// </summary>
        /// <param name="orderId">The order identifier</param>
        /// <param name="invoiceAmountTypeId">The invoice amount identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the square order mapping
        /// </returns>
        public virtual async Task<SquareOrderMapping> GetSquareOrderMappingByOrderIdAsync(int orderId, int invoiceAmountTypeId)
        {
            if (orderId <= 0 || invoiceAmountTypeId <= 0)
            {
                return null;
            }

            var query = from q in _squareOrderMappingRepository.Table
                        where q.OrderId == orderId &&
                              q.InvoiceAmountTypeId == invoiceAmountTypeId
                        orderby q.Id
                        select q;

            return await query.FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets a unpaid invoice
        /// </summary>
        /// <param name="orderId">The order identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the square order mapping
        /// </returns>
        public virtual async Task<SquareOrderMapping> GetUnPaidInvoiceByOrderIdAsync(int orderId)
        {
            if (orderId <= 0)
            {
                return null;
            }

            var query = from q in _squareOrderMappingRepository.Table
                        where q.OrderId == orderId && !q.Deleted &&
                            (q.InvoiceStatusId == (int)InvoiceStatus.Draft ||
                            q.InvoiceStatusId == (int)InvoiceStatus.UnPaid ||
                            q.InvoiceStatusId == (int)InvoiceStatus.Scheduled)
                        orderby q.Id
                        select q;

            return await query.FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets a square order mapping
        /// </summary>
        /// <param name="squareInvoiceId">The square invoice identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the square order mapping
        /// </returns>
        public virtual async Task<SquareOrderMapping> GetSquareOrderMappingBySquareInvoiceIdAsync(string squareInvoiceId)
        {
            if (string.IsNullOrWhiteSpace(squareInvoiceId))
                return null;

            var query = from q in _squareOrderMappingRepository.Table
                        where q.SquareInvoiceId == squareInvoiceId && !q.Deleted
                        orderby q.Id
                        select q;

            return await query.FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets a square order mappings
        /// </summary>
        /// <param name="orderId">The order identifier</param>
        /// <param name="showHidden">If it's true then show deleted records also.</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of square order mappings with custom number
        /// </returns>
        public virtual async Task<IList<SquareOrderMappingWithCustomNumber>> GetSquareOrderMappingsByOrderIdAsync(int orderId, bool showHidden = false)
        {
            if (orderId <= 0)
            {
                return new List<SquareOrderMappingWithCustomNumber>();
            }

            var orderQuery = _orderRepository.Table;

            if (!showHidden)
            {
                orderQuery = orderQuery.Where(q => !q.Deleted);
            }

            var order = await orderQuery
                            .Where(o => o.Id == orderId)
                            .Select(o => new { o.Id, o.CustomOrderNumber })
                            .FirstOrDefaultAsync();

            if (order == null)
            {
                return new List<SquareOrderMappingWithCustomNumber>();
            }

            var results = new List<(int OrderId, string CustomOrderNumber, int DisplayOrder)>
            {
                (order.Id, order.CustomOrderNumber, 0)
            };

            var childOrders = await (
                                from m in _logisticsChildOrderMappingRepository.Table
                                join o in orderQuery on m.ChildOrderId equals o.Id
                                where m.ParentOrderId == order.Id
                                orderby m.DisplayOrder, o.Id
                                select new { o.Id, o.CustomOrderNumber, m.DisplayOrder }
                            ).ToListAsync();

            results.AddRange(childOrders.Select(c => (c.Id, c.CustomOrderNumber, c.DisplayOrder)));

            var orderIds = results.Select(x => x.OrderId).Distinct().ToList();

            var mappings = await _squareOrderMappingRepository.Table
                            .Where(x => orderIds.Contains(x.OrderId))
                            .OrderByDescending(q => q.Id)
                            .ToListAsync();

            var finalResults = new List<SquareOrderMappingWithCustomNumber>(mappings.Count);

            foreach (var (id, customNumber, _) in results.OrderBy(x => x.DisplayOrder).ThenBy(x => x.OrderId))
            {
                foreach (var map in mappings)
                {
                    if (map.OrderId == id)
                    {
                        finalResults.Add(new SquareOrderMappingWithCustomNumber
                        {
                            Mapping = map,
                            CustomOrderNumber = customNumber
                        });
                    }
                }
            }

            return finalResults;
        }

        /// <summary>
        /// Has any square order mappings
        /// </summary>
        /// <param name="orderId">The order identifier</param>
        /// <param name="showHidden">If it's true then show deleted records also.</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the boolean value if any square order mappings found for order identifier
        /// </returns>
        public virtual async Task<bool> HasAnySquareOrderMappingsByOrderIdAsync(int orderId, bool showHidden = false)
        {
            if (orderId <= 0)
            {
                return false;
            }

            var query = _squareOrderMappingRepository.Table
                    .Where(q => q.OrderId == orderId);

            if (!showHidden)
            {
                query = query.Where(q => !q.Deleted);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// Check a invoice is generated for the parent order with sub orders
        /// </summary>
        /// <param name="orderId">The order identifier</param>
        /// <param name="showHidden">If it's true then show deleted records also.</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the boolean value if any square invoices found for order identifier
        /// </returns>
        public virtual async Task<bool> HasAnySquareInvoiceGeneratedForParentOrderAsync(int orderId)
        {
            if (orderId <= 0)
            {
                return false;
            }

            var squareOrderMappingQuery = _squareOrderMappingRepository.Table
                        .Where(q => !q.Deleted);

            var query = from m in _logisticsChildOrderMappingRepository.Table
                        where m.ParentOrderId == orderId
                        join o in _orderRepository.Table on m.ChildOrderId equals o.Id
                        where !o.Deleted
                        join som in squareOrderMappingQuery on o.Id equals som.OrderId into childMappings
                        from cm in childMappings.DefaultIfEmpty()
                        where cm != null
                        select cm;

            query = query.Union(from spom in squareOrderMappingQuery
                                where orderId == spom.OrderId
                                select spom);

            return await query.AnyAsync();
        }

        /// <summary>
        /// Deletes a square order mapping
        /// </summary>
        /// <param name="squareOrderMapping">The square order mapping</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task DeleteSquareOrderMappingAsync(SquareOrderMapping squareOrderMapping)
        {
            if (squareOrderMapping == null)
                throw new ArgumentNullException(nameof(squareOrderMapping));

            squareOrderMapping.UpdatedOnUtc = DateTime.UtcNow;
            await _squareOrderMappingRepository.DeleteAsync(squareOrderMapping);
        }

        /// <summary>
        /// Inserts a square order mapping
        /// </summary>
        /// <param name="squareOrderMapping">The square order mapping</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task InsertSquareOrderMappingAsync(SquareOrderMapping squareOrderMapping)
        {
            if (squareOrderMapping == null)
                throw new ArgumentNullException(nameof(squareOrderMapping));

            squareOrderMapping.CreatedOnUtc = DateTime.UtcNow;
            squareOrderMapping.UpdatedOnUtc = DateTime.UtcNow;
            await _squareOrderMappingRepository.InsertAsync(squareOrderMapping);
        }

        /// <summary>
        /// Updates the square order mapping
        /// </summary>
        /// <param name="squareOrderMapping">The square order mapping</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task UpdateSquareOrderMappingAsync(SquareOrderMapping squareOrderMapping)
        {
            if (squareOrderMapping == null)
                throw new ArgumentNullException(nameof(squareOrderMapping));

            squareOrderMapping.UpdatedOnUtc = DateTime.UtcNow;
            await _squareOrderMappingRepository.UpdateAsync(squareOrderMapping);
        }

        /// <summary>
        /// Get the last invoice number for the order
        /// </summary>
        /// <param name="orderId">The order identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the last order invoice number
        /// </returns>
        public virtual async Task<string> GetLastInvoiceNumberAsync(int orderId)
        {
            if (orderId <= 0)
                return null;

            var query = from q in _squareOrderMappingRepository.Table
                        where q.OrderId == orderId && !q.Deleted
                        orderby q.Id descending
                        select q.SquareInvoiceNumber;

            return await query.FirstOrDefaultAsync();
        }

        /// <summary>
        /// Get the invoice number for the order
        /// </summary>
        /// <param name="order">The order</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the generated invoice number
        /// </returns>
        public virtual async Task<string> GenerateInvoiceNumberAsync(Order order)
        {
            if (order == null)
            {
                return null;
            }

            var invoiceNumber = order.CustomOrderNumber;
            if (order.IsChildOrder)
            {
                invoiceNumber = $"{invoiceNumber}-1";
            }

            invoiceNumber = invoiceNumber.Replace(" ", string.Empty);

            var lastInvoiceNumber = await GetLastInvoiceNumberAsync(order.Id);
            if (string.IsNullOrWhiteSpace(lastInvoiceNumber))
            {
                return invoiceNumber;
            }

            if (!lastInvoiceNumber.Contains('-'))
            {
                return $"{invoiceNumber}-1";
            }

            var parts = lastInvoiceNumber
                        .Split('-', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .ToList();

            _ = int.TryParse(parts.LastOrDefault(), out int lastNumber);

            parts[parts.Count - 1] = (lastNumber + 1).ToString();

            return string.Join("-", parts);
        }

        #endregion

        #region Square Transaction Order Mapping

        /// <summary>
        /// Gets a square transaction order mapping
        /// </summary>
        /// <param name="paymentId">The payment identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the square transaction order mapping
        /// </returns>
        public virtual async Task<SquareTransactionOrderMapping> GetSquareTransactionOrderMappingByPaymentIdAsync(string paymentId)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
                return null;

            var query = from q in _squareTransactionOrderMappingRepository.Table
                        where q.PaymentId == paymentId
                        orderby q.Id
                        select q;

            return await query.FirstOrDefaultAsync();
        }

        /// <summary>
        /// Inserts a square transaction order mapping
        /// </summary>
        /// <param name="squareTransactionOrderMapping">The square transaction order mapping</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task InsertSquareTransactionOrderMappingAsync(SquareTransactionOrderMapping squareTransactionOrderMapping)
        {
            if (squareTransactionOrderMapping == null)
                throw new ArgumentNullException(nameof(squareTransactionOrderMapping));

            await _squareTransactionOrderMappingRepository.InsertAsync(squareTransactionOrderMapping);
        }

        /// <summary>
        /// Updates the square transaction order mapping
        /// </summary>
        /// <param name="squareTransactionOrderMapping">The square transaction order mapping</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task UpdateSquareTransactionOrderMappingAsync(SquareTransactionOrderMapping squareTransactionOrderMapping)
        {
            if (squareTransactionOrderMapping == null)
                throw new ArgumentNullException(nameof(squareTransactionOrderMapping));

            await _squareTransactionOrderMappingRepository.UpdateAsync(squareTransactionOrderMapping);
        }

        #endregion

        #region Square Order Item

        /// <summary>
        /// Inserts a square order item
        /// </summary>
        /// <param name="squareOrderItem">The square order item</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public virtual async Task InsertSquareOrderItemAsync(SquareOrderItem squareOrderItem)
        {
            if (squareOrderItem == null)
                throw new ArgumentNullException(nameof(squareOrderItem));

            await _squareOrderItemRepository.InsertAsync(squareOrderItem);
        }

        /// <summary>
        /// Get all square order items
        /// </summary>
        /// <param name="squareOrderMappingId">The square order mapping identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of square order items
        /// </returns>
        public virtual async Task<IList<SquareOrderItem>> GetAllSquareOrderItemsAsync(int squareOrderMappingId = 0)
        {
            if (squareOrderMappingId <= 0)
            {
                return new List<SquareOrderItem>();
            }

            return await _squareOrderItemRepository.Table
                    .Where(q => q.SquareOrderMappingId == squareOrderMappingId)
                    .OrderBy(q => q.Id)
                    .ToListAsync();
        }

        #endregion

        #region Commission Report Order

        /// <summary>
        /// Get all commission report orders
        /// </summary>
        /// <param name="createdByCustomer">createdByCustomer</param>
        /// <param name="startDateValue">startDateValue</param>
        /// <param name="endDateValue">endDateValue</param>
        /// <param name="companyName">The company name</param>
        /// <returns>List of CommissionReportOrder</returns>
        public virtual async Task<IList<CommissionReportOrder>> GetAllOrdersWithSquareOrdersAsync(int createdByCustomer = 0,
            DateTime? startDateValue = null,
            DateTime? endDateValue = null,
            string companyName = null)
        {
            var query = from o in _orderRepository.Table
                        join s in _logisticsStatusRepository.Table on o.StatusId equals s.Id
                        where !o.Deleted && !o.IsChildOrder &&
                              o.PaymentOption == LogisticsDefaults.PaymentOption.COD
                        select o;

            if (createdByCustomer > 0)
                query = query.Where(o => o.ARMSaleRepId == createdByCustomer);

            if (!string.IsNullOrWhiteSpace(companyName))
            {
                companyName = companyName.Trim().ToLower();

                query = from o in query
                        join c in _customerRepository.Table on o.CustomerId equals c.Id
                        join comp in _companyRepository.Table on c.CompanyId equals comp.Id
                        where comp.Name.ToLower().Contains(companyName)
                        select o;
            }

            var squarePaidMinDates = from so in _squareOrderMappingRepository.Table
                                     where !so.Deleted && so.PaidDateOnUtc.HasValue
                                     group so by so.OrderId into g
                                     let minDate = g.Min(x => x.PaidDateOnUtc)
                                     select new
                                     {
                                         OrderId = g.Key,
                                         PaidDateOnUtc = minDate
                                     };

            var squareOrderQuery = from so in _squareOrderMappingRepository.Table
                                   join g in squarePaidMinDates
                                       on new { so.OrderId, so.PaidDateOnUtc } equals new { g.OrderId, g.PaidDateOnUtc }
                                   select so;

            var queryResult = from o in query
                              join so in squareOrderQuery on o.Id equals so.OrderId
                              where !o.Deleted
                              select new CommissionReportOrder
                              {
                                  OrderId = o.Id,
                                  LeadId = o.LeadId,
                                  Price = o.Price,
                                  CarrierPay = o.CarrierPay,
                                  CreatedOnUtc = o.CreatedOnUtc,
                                  SquarePaidDateAndQuickBookDateOnUtc = so.PaidDateOnUtc,
                                  IsChildOrder = o.IsChildOrder,
                                  CustomOrderNumber = o.CustomOrderNumber,
                                  BillingAddressId = o.BillingAddressId,
                              };

            if (startDateValue.HasValue)
                queryResult = queryResult.Where(o => startDateValue.Value <= o.SquarePaidDateAndQuickBookDateOnUtc);

            if (endDateValue.HasValue)
                queryResult = queryResult.Where(o => endDateValue.Value >= o.SquarePaidDateAndQuickBookDateOnUtc);

            return await queryResult.ToListAsync();
        }

        #endregion

        #endregion
    }
}