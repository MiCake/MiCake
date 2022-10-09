namespace MiCake.Core.Tests.DependencyInjection
{
    [AutoDI]
    public class CurrentMiCakeModule : MiCakeModule
    {
    }

    [DisableAutoDI]
    public class CurrentDisableDIMiCakeModule : MiCakeModule
    {
    }

    [AutoDI]
    [DisableAutoDI]
    public class BothTwoDITagMiCakeModule : MiCakeModule
    {
    }
}
