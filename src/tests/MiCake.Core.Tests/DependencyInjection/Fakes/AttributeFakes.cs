namespace MiCake.Core.Tests.DependencyInjection.Fakes
{
    [InjectService()]
    public class DefaultAttributeClass
    {
    }

    [InjectService(typeof(InjectServiceAttribute))]
    public class HasOneServiceAttributeClass
    {
    }

    [InjectService(
        typeof(InjectServiceAttribute),
        typeof(InjectServiceTwo),
        typeof(InjectServiceThree))]
    public class HasMoreServiceAttributeClass
    {
    }

    [InjectService(ReplaceServices = true)]
    public class ReplaceAttributeClass { }

    [InjectService(TryRegister = true)]
    public class TryRegisterAttributeClass { }

    [InjectService(IncludeSelf = false)]
    public class NotIncludeItSelfAttributeClass { }

    [InjectService(Lifetime = MiCakeServiceLifetime.Singleton)]
    public class SinglethonAttributeClass { }

    [InjectService(Lifetime = MiCakeServiceLifetime.Scoped)]
    public class ScopedAttributeClass { }

    public interface InjectServiceOne { }
    public interface InjectServiceTwo { }
    public interface InjectServiceThree { }
}
