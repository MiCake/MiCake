using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Aop
{
    /// <summary>
    /// Encapsulates an invocation of a proxied method.
    /// </summary>
    public interface IMiCakeInvocation
    {
        /// <summary>
        ///  Gets the arguments that the Castle.DynamicProxy.IInvocation.Method has been invoked with.
        /// </summary>
        object[] Arguments { get; }

        /// <summary>
        /// Gets the generic arguments of the method.
        /// </summary>
        Type[] GenericArguments { get; }

        /// <summary>
        /// Gets the System.Reflection.MethodInfo representing the method being invoked on the proxy.
        /// </summary>
        MethodInfo Method { get; }

        /// <summary>
        /// Gets or sets the return value of the method.
        /// </summary>
        object ReturnValue { get; set; }

        /// <summary>
        /// Gets the type of the target object for the intercepted method.
        /// </summary>
        Type TargetType { get; }

        /// <summary>
        ///  Proceeds the call to the next interceptor in line, and ultimately to the target method.
        /// </summary>
        void Proceed();

        /// <summary>
        ///  Proceeds the call to the next interceptor in line, and ultimately to the target method.
        /// </summary>
        Task ProceedAsync(CancellationToken cancellationToken = default);
    }
}
