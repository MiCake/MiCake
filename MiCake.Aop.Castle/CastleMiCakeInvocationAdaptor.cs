using Castle.DynamicProxy;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Aop.Castle
{
    public class CastleMiCakeInvocationAdaptor : IMiCakeInvocation
    {
        public object[] Arguments => Invocation.Arguments;

        public Type[] GenericArguments => Invocation.GenericArguments;

        public object TargetObject => Invocation.InvocationTarget ?? Invocation.MethodInvocationTarget;

        public MethodInfo Method => Invocation.MethodInvocationTarget ?? Invocation.Method;

        public Type TargetType => Invocation.TargetType;

        public object ReturnValue
        {
            get => Invocation.ReturnValue;
            set => Invocation.ReturnValue = value;
        }

        protected IInvocation Invocation { get; }
        protected IInvocationProceedInfo ProceedInfo { get; }

        public CastleMiCakeInvocationAdaptor(IInvocation invocation, IInvocationProceedInfo proceedInfo)
        {
            Invocation = invocation;
            ProceedInfo = proceedInfo;

           // ProceedInfo = invocation.CaptureProceedInfo();  if there is in async,will throw a exception in other thread.
        }

        public void Proceed()
        {
            ProceedInfo.Invoke();
        }

        public Task ProceedAsync(CancellationToken cancellationToken)
        {
            ProceedInfo.Invoke();

            return Method.GetMethodType() == MethodType.Synchronous ? 
                        Task.FromResult(Invocation.ReturnValue) : 
                        (Task)Invocation.ReturnValue;
        }
    }
}
