using MiCake.DDD.Domain;
using MiCake.Util.LinqFilter;
using MiCake.Util.Paging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// Repository implementation with advanced paging and filtering capabilities for Entity Framework Core.
    /// Extends the full repository functionality with specialized methods for paginated queries and dynamic filtering.
    /// This is ideal for building list views, search functionality, and data grids with sorting and filtering.
    /// </summary>
    /// <typeparam name="TDbContext">The Entity Framework DbContext type</typeparam>
    /// <typeparam name="TAggregateRoot">The aggregate root type that implements IAggregateRoot</typeparam>
    /// <typeparam name="TKey">The primary key type of the aggregate root</typeparam>
    /// <remarks>
    /// This class extends <see cref="EFRepository{TDbContext, TAggregateRoot, TKey}"/> with paging and filtering operations.
    /// It supports standard pagination, custom sorting, and dynamic filtering through FilterGroup and CompositeFilterGroup.
    /// All paging operations are optimized to execute COUNT and data queries efficiently.
    /// Users can inherit from this class to create custom repositories with domain-specific paging logic.
    /// </remarks>
    /// <example>
    /// <code>
    /// public class OrderRepositoryWithPaging : EFRepositoryHasPaging&lt;MyDbContext, Order, int&gt;
    /// {
    ///     public OrderRepositoryWithPaging(EFRepositoryDependencies&lt;MyDbContext&gt; dependencies) 
    ///         : base(dependencies)
    ///     {
    ///     }
    ///     
    ///     public async Task&lt;PagingResponse&lt;Order&gt;&gt; GetOrdersByStatusAsync(
    ///         OrderStatus status, 
    ///         PagingRequest paging)
    ///     {
    ///         var filter = new FilterGroup 
    ///         { 
    ///             Filters = new[] { new Filter("Status", status, FilterOperator.Equal) }
    ///         };
    ///         return await CommonFilterPagingQueryAsync(paging, filter);
    ///     }
    /// }
    /// </code>
    /// </example>
    public class EFRepositoryHasPaging<TDbContext, TAggregateRoot, TKey> : EFRepository<TDbContext, TAggregateRoot, TKey>, IRepositoryHasPagingQuery<TAggregateRoot, TKey>
            where TAggregateRoot : class, IAggregateRoot<TKey>
            where TDbContext : DbContext
            where TKey : notnull
    {
        private readonly Sort _defaultSort = new() { PropertyName = "Id", Ascending = false };

        /// <summary>
        /// Initializes a new instance of the repository with paging support.
        /// </summary>
        /// <param name="dependencies">The dependency wrapper containing all required services</param>
        /// <exception cref="ArgumentNullException">Thrown when dependencies is null</exception>
        public EFRepositoryHasPaging(EFRepositoryDependencies<TDbContext> dependencies) : base(dependencies)
        {
        }

        /// <summary>
        /// Performs a paginated query with basic paging parameters.
        /// Retrieves a specific page of data without custom sorting (uses default sort by Id descending).
        /// </summary>
        /// <param name="queryModel">The paging request containing page index and page size</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a paging response with the requested page of data and total count.
        /// </returns>
        /// <remarks>
        /// This method applies default sorting by Id in descending order.
        /// For custom sorting, use the overload that accepts an order selector.
        /// The method efficiently executes both the COUNT and data queries.
        /// </remarks>
        /// <example>
        /// <code>
        /// var request = new PagingRequest { PageIndex = 1, PageSize = 20 };
        /// var result = await repository.PagingQueryAsync(request);
        /// Console.WriteLine($"Total: {result.TotalCount}, Page: {result.PageIndex}");
        /// </code>
        /// </example>
        public async Task<PagingResponse<TAggregateRoot>> PagingQueryAsync(PagingRequest queryModel, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            var result = await dbset.Skip(queryModel.CurrentStartNo).Take(queryModel.PageSize).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var count = await GetCountAsync(cancellationToken).ConfigureAwait(false);

            return new PagingResponse<TAggregateRoot>(queryModel.PageIndex, count, result);
        }

        /// <summary>
        /// Performs a paginated query with custom sorting.
        /// Retrieves a specific page of data ordered by a specified property.
        /// </summary>
        /// <typeparam name="TOrderKey">The type of the property to order by</typeparam>
        /// <param name="queryModel">The paging request containing page index and page size</param>
        /// <param name="orderSelector">Expression to select the property to order by</param>
        /// <param name="asc">If true, sorts in ascending order; if false, sorts in descending order. Default is true.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a paging response with the sorted page of data and total count.
        /// </returns>
        /// <remarks>
        /// This method allows flexible sorting on any property of the aggregate root.
        /// The sorting is applied before pagination for correct results.
        /// </remarks>
        /// <example>
        /// <code>
        /// var request = new PagingRequest { PageIndex = 1, PageSize = 20 };
        /// var result = await repository.PagingQueryAsync(
        ///     request, 
        ///     o => o.CreatedDate, 
        ///     asc: false); // Sort by CreatedDate descending
        /// </code>
        /// </example>
        public async Task<PagingResponse<TAggregateRoot>> PagingQueryAsync<TOrderKey>(PagingRequest queryModel, Expression<Func<TAggregateRoot, TOrderKey>> orderSelector, bool asc = true, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);

            IEnumerable<TAggregateRoot> result;
            if (asc)
            {
                result = await dbset.OrderBy(orderSelector).Skip(queryModel.CurrentStartNo).Take(queryModel.PageSize).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                result = await dbset.OrderByDescending(orderSelector).Skip(queryModel.CurrentStartNo).Take(queryModel.PageSize).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            var count = await GetCountAsync(cancellationToken).ConfigureAwait(false);

            return new PagingResponse<TAggregateRoot>(queryModel.PageIndex, count, result);
        }

        /// <summary>
        /// Performs a paginated query with dynamic filtering using a filter group.
        /// Combines filtering, optional sorting, and pagination in a single operation.
        /// </summary>
        /// <param name="queryModel">The paging request containing page index and page size</param>
        /// <param name="filterGroup">The filter group containing filter conditions to apply</param>
        /// <param name="sorts">Optional list of sort specifications. If null, uses default sort by Id descending.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a paging response with filtered, sorted data and matching count.
        /// </returns>
        /// <remarks>
        /// FilterGroup allows building complex filter conditions programmatically.
        /// Filters are applied first, then sorting, then pagination for optimal performance.
        /// The count reflects only the filtered results, not the total table count.
        /// </remarks>
        /// <example>
        /// <code>
        /// var filterGroup = new FilterGroup 
        /// {
        ///     Filters = new[] 
        ///     {
        ///         new Filter("Status", OrderStatus.Pending, FilterOperator.Equal),
        ///         new Filter("TotalAmount", 100, FilterOperator.GreaterThan)
        ///     }
        /// };
        /// var sorts = new List&lt;Sort&gt; { new Sort { PropertyName = "CreatedDate", Ascending = false } };
        /// var result = await repository.CommonFilterPagingQueryAsync(request, filterGroup, sorts);
        /// </code>
        /// </example>
        public async Task<PagingResponse<TAggregateRoot>> CommonFilterPagingQueryAsync(PagingRequest queryModel, FilterGroup filterGroup, List<Sort>? sorts = null, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            var query = dbset.AsQueryable().Filter(filterGroup).Sort(sorts ?? [_defaultSort]);
            var result = await query.Skip(queryModel.CurrentStartNo).Take(queryModel.PageSize).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var count = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            return new PagingResponse<TAggregateRoot>(queryModel.PageIndex, count, result);
        }

        /// <summary>
        /// Performs a paginated query with dynamic filtering using a composite filter group.
        /// Supports complex filter logic with AND/OR combinations between multiple filter groups.
        /// </summary>
        /// <param name="queryModel">The paging request containing page index and page size</param>
        /// <param name="compositeFilterGroup">The composite filter group with multiple filter groups and logical operators</param>
        /// <param name="sorts">Optional list of sort specifications. If null, uses default sort by Id descending.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a paging response with filtered, sorted data and matching count.
        /// </returns>
        /// <remarks>
        /// CompositeFilterGroup enables building complex queries with nested AND/OR logic.
        /// This is useful for advanced search functionality with multiple criteria groups.
        /// The count reflects only the filtered results, not the total table count.
        /// </remarks>
        /// <example>
        /// <code>
        /// var composite = new CompositeFilterGroup 
        /// {
        ///     FilterGroups = new[] 
        ///     {
        ///         new FilterGroup { Filters = new[] { /* filters for group 1 */ } },
        ///         new FilterGroup { Filters = new[] { /* filters for group 2 */ } }
        ///     },
        ///     LogicalOperator = LogicalOperator.Or
        /// };
        /// var result = await repository.CommonFilterPagingQueryAsync(request, composite);
        /// </code>
        /// </example>
        public async Task<PagingResponse<TAggregateRoot>> CommonFilterPagingQueryAsync(PagingRequest queryModel, CompositeFilterGroup compositeFilterGroup, List<Sort>? sorts = null, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            var query = dbset.AsQueryable().Filter(compositeFilterGroup).Sort(sorts ?? [_defaultSort]);
            var result = await query.Skip(queryModel.CurrentStartNo).Take(queryModel.PageSize).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var count = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            return new PagingResponse<TAggregateRoot>(queryModel.PageIndex, count, result);
        }

        /// <summary>
        /// Performs a filtered query using a filter group without pagination.
        /// Returns all matching results with optional sorting applied.
        /// </summary>
        /// <param name="filterGroup">The filter group containing filter conditions to apply</param>
        /// <param name="sorts">Optional list of sort specifications. If null, uses default sort by Id descending.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains all entities matching the filter criteria.
        /// </returns>
        /// <remarks>
        /// Use this method when you need all filtered results without pagination.
        /// Be cautious with large result sets as all data is loaded into memory.
        /// For large datasets, consider using the paging version instead.
        /// </remarks>
        /// <example>
        /// <code>
        /// var filterGroup = new FilterGroup 
        /// {
        ///     Filters = new[] { new Filter("IsActive", true, FilterOperator.Equal) }
        /// };
        /// var activeOrders = await repository.CommonFilterQueryAsync(filterGroup);
        /// </code>
        /// </example>
        public async Task<IEnumerable<TAggregateRoot>> CommonFilterQueryAsync(FilterGroup filterGroup, List<Sort>? sorts = null, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            var query = dbset.AsQueryable().Filter(filterGroup).Sort(sorts ?? [_defaultSort]);

            return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a filtered query using a composite filter group without pagination.
        /// Returns all matching results with optional sorting applied.
        /// </summary>
        /// <param name="compositeFilterGroup">The composite filter group with multiple filter groups and logical operators</param>
        /// <param name="sorts">Optional list of sort specifications. If null, uses default sort by Id descending.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains all entities matching the composite filter criteria.
        /// </returns>
        /// <remarks>
        /// Use this method when you need all filtered results with complex filter logic and no pagination.
        /// Be cautious with large result sets as all data is loaded into memory.
        /// For large datasets, consider using the paging version instead.
        /// </remarks>
        /// <example>
        /// <code>
        /// var composite = new CompositeFilterGroup 
        /// {
        ///     FilterGroups = new[] 
        ///     {
        ///         new FilterGroup { Filters = new[] { /* high priority filters */ } },
        ///         new FilterGroup { Filters = new[] { /* urgent filters */ } }
        ///     },
        ///     LogicalOperator = LogicalOperator.Or
        /// };
        /// var importantOrders = await repository.CommonFilterQueryAsync(composite);
        /// </code>
        /// </example>
        public async Task<IEnumerable<TAggregateRoot>> CommonFilterQueryAsync(CompositeFilterGroup compositeFilterGroup, List<Sort>? sorts = null, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            var query = dbset.AsQueryable().Filter(compositeFilterGroup).Sort(sorts ?? [_defaultSort]);

            return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
