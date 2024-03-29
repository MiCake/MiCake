﻿namespace MiCake.DDD.Extensions.Metadata
{
    /// <summary>
    /// Builds or modifies an <see cref="DomainObjectModel"/> for micake discovery.
    /// </summary>
    public interface IDomainObjectModelProvider
    {
        /// <summary>
        /// Gets the order value for determining the order of execution of providers. Providers execute in
        /// ascending numeric value of the <see cref="Order"/> property.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Providers are executed in an ordering determined by an ascending sort of the <see cref="Order"/> property.
        /// A provider with a lower numeric value of <see cref="Order"/> will have its
        /// <see cref="OnProvidersExecuting"/> called before that of a provider with a higher numeric value of
        /// <see cref="Order"/>. The <see cref="OnProvidersExecuted"/> method is called in the reverse ordering after
        /// all calls to <see cref="OnProvidersExecuting"/>. A provider with a lower numeric value of
        /// <see cref="Order"/> will have its <see cref="OnProvidersExecuted"/> method called after that of a provider
        /// with a higher numeric value of <see cref="Order"/>.
        /// </para>
        /// <para>
        /// If two providers have the same numeric value of <see cref="Order"/>, then their relative execution order
        /// is undefined.
        /// </para>
        /// </remarks>
        int Order { get; }

        /// <summary>
        /// Executed for the first pass of <see cref="DomainObjectModelContext"/> building. See <see cref="Order"/>.
        /// </summary>
        /// <param name="context">The <see cref="DomainObjectModelContext"/>.</param>
        void OnProvidersExecuting(DomainObjectModelContext context);

        /// <summary>
        /// Executed for the second pass of <see cref="DomainObjectModelContext"/> building. See <see cref="Order"/>.
        /// </summary>
        /// <param name="context">The <see cref="DomainObjectModelContext"/>.</param>
        void OnProvidersExecuted(DomainObjectModelContext context);
    }
}
