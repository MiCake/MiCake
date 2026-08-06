using MiCake.DDD.Domain;
using MiCake.Util.Query.Dynamic;
using MiCake.Util.Query.Paging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// Repository implementation with advanced paging and filtering capabilities for Entity Framework Core.
    /// Extends the full repository functionality with specialized methods for paginated queries and dynamic filtering.
    /// This is ideal for building list views, search functionality, and data grids with sorting and filtering.
    /// </summary>
    public class EFRepositoryHasPaging<TDbContext, TAggregateRoot, TKey> : EFRepository<TDbContext, TAggregateRoot, TKey>, IRepositoryHasPagingQuery<TAggregateRoot, TKey>
            where TAggregateRoot : class, IAggregateRoot<TKey>
            where TDbContext : DbContext
            where TKey : notnull
    {
        /// <summary>
        /// Initializes a new instance of the repository with paging support.
        /// </summary>
        /// <param name="dependencies">The dependency wrapper containing all required services</param>
        /// <exception cref="ArgumentNullException">Thrown when dependencies is null</exception>
        public EFRepositoryHasPaging(EFRepositoryDependencies<TDbContext> dependencies) : base(dependencies)
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task<PagingResponse<TAggregateRoot>> PagingQueryAsync(PagingRequest pagingRequest, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);

            // A total order is established before Skip/Take: every primary-key property
            // in EF model order, ascending, when the caller supplied no sorting.
            var query = AppendMissingPrimaryKeyOrdering(dbset.AsQueryable(), new HashSet<string>());
            var result = await query.Skip(pagingRequest.CurrentStartNo).Take(pagingRequest.PageSize).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var count = await GetCountAsync(cancellationToken).ConfigureAwait(false);

            return new PagingResponse<TAggregateRoot>(pagingRequest.PageIndex, count, result);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task<PagingResponse<TAggregateRoot>> PagingQueryAsync<TOrderKey>(PagingRequest pagingRequest, Expression<Func<TAggregateRoot, TOrderKey>> orderSelector, bool asc = true, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);

IQueryable<TAggregateRoot> query = asc
                ? dbset.AsQueryable().OrderBy(orderSelector)
                : dbset.AsQueryable().OrderByDescending(orderSelector);

            // Caller-sorted keys keep their direction; missing primary-key properties are
            // appended as ascending final ThenBy clauses so the page is fully ordered.
            query = AppendMissingPrimaryKeyOrdering(query, CollectMemberNames(orderSelector));
            var result = await query.Skip(pagingRequest.CurrentStartNo).Take(pagingRequest.PageSize).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var count = await GetCountAsync(cancellationToken).ConfigureAwait(false);

            return new PagingResponse<TAggregateRoot>(pagingRequest.PageIndex, count, result);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task<PagingResponse<TAggregateRoot>> FilterPagingQueryAsync(PagingRequest pagingRequest, FilterGroup filterGroup, List<Sort>? sorts = null, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            var query = ApplyCallerSorts(dbset.AsQueryable(), sorts).Filter(filterGroup);
            var result = await query.Skip(pagingRequest.CurrentStartNo).Take(pagingRequest.PageSize).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var count = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            return new PagingResponse<TAggregateRoot>(pagingRequest.PageIndex, count, result);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task<PagingResponse<TAggregateRoot>> FilterPagingQueryAsync(PagingRequest pagingRequest, CompositeFilterGroup compositeFilterGroup, List<Sort>? sorts = null, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            var query = ApplyCallerSorts(dbset.AsQueryable(), sorts).Filter(compositeFilterGroup);
            var result = await query.Skip(pagingRequest.CurrentStartNo).Take(pagingRequest.PageSize).ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var count = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            return new PagingResponse<TAggregateRoot>(pagingRequest.PageIndex, count, result);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task<IEnumerable<TAggregateRoot>> FilterQueryAsync(FilterGroup filterGroup, List<Sort>? sorts = null, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            var query = ApplyCallerSorts(dbset.AsQueryable(), sorts).Filter(filterGroup);

            return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task<IEnumerable<TAggregateRoot>> FilterQueryAsync(CompositeFilterGroup compositeFilterGroup, List<Sort>? sorts = null, CancellationToken cancellationToken = default)
        {
            var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
            var query = ApplyCallerSorts(dbset.AsQueryable(), sorts).Filter(compositeFilterGroup);

            return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Applies caller sorts (or none) and then appends every missing primary-key
        /// property as an ascending final ordering so the result is fully ordered.
        /// </summary>
        private IQueryable<TAggregateRoot> ApplyCallerSorts(IQueryable<TAggregateRoot> query, List<Sort>? sorts)
        {
            var orderedNames = new HashSet<string>();
            if (sorts != null && sorts.Count > 0)
            {
                query = query.Sort(sorts);
                foreach (var sort in sorts)
                {
                    orderedNames.Add(sort.PropertyName);
                }
            }

            return AppendMissingPrimaryKeyOrdering(query, orderedNames);
        }

        /// <summary>
        /// Appends every primary-key property that is not already ordered, ascending,
        /// in EF model order. Rejects keyless entity types with a clear diagnostic.
        /// </summary>
        private IQueryable<TAggregateRoot> AppendMissingPrimaryKeyOrdering(IQueryable<TAggregateRoot> query, ISet<string> alreadyOrderedNames)
        {
            var dbContext = Dependencies.ContextFactory.GetDbContext();
            var entityType = dbContext.Model.FindEntityType(typeof(TAggregateRoot));
            var primaryKey = entityType?.FindPrimaryKey()
                ?? throw new InvalidOperationException(
                    $"Paging on keyless entity type {typeof(TAggregateRoot).Name} is not supported. " +
                    "A primary key is required to establish the deterministic total order applied before Skip/Take.");

            foreach (var property in primaryKey.Properties)
            {
                if (!alreadyOrderedNames.Contains(property.Name))
                {
                    query = AppendKeyOrdering(query, property);
                }
            }

            return query;
        }

        /// <summary>
        /// Appends one primary-key property as an ascending final ordering. Shadow properties
        /// are accessed through <c>EF.Property&lt;T&gt;</c> because they have no CLR member;
        /// regular properties use a direct member expression.
        /// </summary>
        private static IQueryable<TAggregateRoot> AppendKeyOrdering(IQueryable<TAggregateRoot> query, IProperty property)
        {
            var parameter = Expression.Parameter(typeof(TAggregateRoot), "x");

            Expression body = property.IsShadowProperty()
                ? BuildShadowPropertyAccess(parameter, property)
                : Expression.Property(parameter, property.PropertyInfo!);

            var keySelector = Expression.Lambda<Func<TAggregateRoot, object>>(
                Expression.Convert(body, typeof(object)), parameter);

            return HasOrderingMethodCall(query.Expression)
                ? ((IOrderedQueryable<TAggregateRoot>)query).ThenBy(keySelector)
                : query.OrderBy(keySelector);
        }

        private static MethodCallExpression BuildShadowPropertyAccess(ParameterExpression parameter, IProperty property)
        {
            var efProperty = typeof(EF).GetMethod(nameof(EF.Property), BindingFlags.Public | BindingFlags.Static)
                ?.MakeGenericMethod(property.ClrType)
                ?? throw new InvalidOperationException(
                    $"EF.Property<T> could not be resolved for shadow property {property.Name}.");

            return Expression.Call(efProperty, parameter, Expression.Constant(property.Name));
        }

        private static bool HasOrderingMethodCall(Expression expression)
        {
            if (expression is MethodCallExpression methodCall)
            {
                var methodName = methodCall.Method.Name;
                if (methodName is nameof(Queryable.OrderBy) or nameof(Queryable.OrderByDescending)
                    or nameof(Queryable.ThenBy) or nameof(Queryable.ThenByDescending))
                {
                    return true;
                }

                if (methodCall.Arguments.Count > 0)
                {
                    return HasOrderingMethodCall(methodCall.Arguments[0]);
                }
            }

            return false;
        }

        private static HashSet<string> CollectMemberNames<TOrderKey>(Expression<Func<TAggregateRoot, TOrderKey>> orderSelector)
        {
            var names = new HashSet<string>();
            CollectMemberNames(orderSelector.Body, names);
            return names;
        }

        private static void CollectMemberNames(Expression expression, HashSet<string> names)
        {
            switch (expression)
            {
                case MemberExpression member:
                    names.Add(member.Member.Name);
                    break;
                case NewExpression newExpression:
                    foreach (var argument in newExpression.Arguments)
                    {
                        CollectMemberNames(argument, names);
                    }
                    break;
                case MemberInitExpression memberInit:
                    CollectMemberNames(memberInit.NewExpression, names);
                    break;
                case UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary:
                    CollectMemberNames(unary.Operand, names);
                    break;
            }
        }
    }
}
