namespace MiCake.Core.Tests.DependencyInjection.Fakes
{
    public class SinglethonClass : ISinglethonClass
    {
    }

    public class ScopedClass : IScopedClass, IScopedService
    {
    }

    public class TransientClass : ITransientClass, ITransientService
    {
    }

    public class OnlyAutoInjectClass : IAutoInjectService
    {
    }

    public class HasTwoInterfaceClass : IHasTwoInterfaceClass, ISingletonService, IScopedService
    {
    }

    public interface ISinglethonClass : ISingletonService { }

    public interface IScopedClass { }

    public interface ITransientClass { }

    public interface IOnlyAutoInjectClass { }

    public interface IHasTwoInterfaceClass { }
}
