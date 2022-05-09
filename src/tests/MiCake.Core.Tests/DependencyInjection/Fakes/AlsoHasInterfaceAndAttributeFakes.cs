namespace MiCake.Core.Tests.DependencyInjection.Fakes
{
    [InjectService(typeof(ITwoFeatureFake), IncludeSelf = false)]
    public class TwoFeatureFake : ITwoFeatureFake, IScopedClass { }

    public interface ITwoFeatureFake { }
}
