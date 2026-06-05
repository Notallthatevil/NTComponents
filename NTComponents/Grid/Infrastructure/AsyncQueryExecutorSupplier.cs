using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace NTComponents.Grid.Infrastructure;

/// <summary>
///     Provides methods for asynchronous evaluation of queries against an <see cref="IQueryable{T}" />.
/// </summary>
[Obsolete("This legacy grid API is obsolete. Use NTDataGrid, NTPropertyColumn, and NTTemplateColumn instead.")]
internal interface IAsyncQueryExecutor {

    /// <summary>
    ///     Asynchronously counts the items in the <see cref="IQueryable{T}" />, if supported.
    /// </summary>
    /// <typeparam name="T">The data type.</typeparam>
    /// <param name="queryable">        An <see cref="IQueryable{T}" /> instance.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>The number of items in <paramref name="queryable" />.</returns>
    Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Determines whether the <see cref="IQueryable{T}" /> is supported by this <see cref="IAsyncQueryExecutor" /> type.
    /// </summary>
    /// <typeparam name="T">The data type.</typeparam>
    /// <param name="queryable">An <see cref="IQueryable{T}" /> instance.</param>
    /// <returns>True if this <see cref="IAsyncQueryExecutor" /> instance can perform asynchronous queries for the supplied <paramref name="queryable" />, otherwise false.</returns>
    bool IsSupported<T>(IQueryable<T> queryable);

    /// <summary>
    ///     Asynchronously materializes the <see cref="IQueryable{T}" /> as an array, if supported.
    /// </summary>
    /// <typeparam name="T">The data type.</typeparam>
    /// <param name="queryable">        An <see cref="IQueryable{T}" /> instance.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>The items in the <paramref name="queryable" />.</returns>
    Task<T[]> ToArrayAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default);
}

[ExcludeFromCodeCoverage]
[Obsolete("This legacy grid API is obsolete. Use NTDataGrid, NTPropertyColumn, and NTTemplateColumn instead.")]
internal static class AsyncQueryExecutorSupplier {
    private static readonly ConcurrentDictionary<Type, bool> _isEntityFrameworkProviderTypeCache = new();

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2111",
               Justification = "The reflection is a best effort to warn developers about sync-over-async behavior which can cause thread pool starvation.")]
    public static IAsyncQueryExecutor? GetAsyncQueryExecutor<T>(IServiceProvider services, IQueryable<T>? queryable) {
        if (queryable is not null) {
            var executor = services.GetService<IAsyncQueryExecutor>();

            if (executor is null) {
                // It's useful to detect if the developer is unaware that they should be using the EF adapter, otherwise they will likely never notice and simply deploy an inefficient app that blocks
                // threads on each query.
                var providerType = queryable.Provider?.GetType();
                if (providerType is not null && _isEntityFrameworkProviderTypeCache.GetOrAdd(providerType, IsEntityFrameworkProviderType)) {
                    throw new InvalidOperationException($"The supplied {nameof(IQueryable)} is provided by Entity Framework.");
                }
            }
            else if (executor.IsSupported(queryable)) {
                return executor;
            }
        }

        return null;
    }

    // We have to do this via reflection because the whole point is to avoid any static dependency on EF unless you reference the adapter. Trimming won't cause us any problems because this is only a
    // way of detecting misconfiguration so it's sufficient if it can detect the misconfiguration in development.
    private static bool IsEntityFrameworkProviderType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type queryableProviderType)
        => queryableProviderType.GetInterfaces().Any(x => string.Equals(x.FullName, "Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider", StringComparison.Ordinal));
}