namespace MiCake.EntityFrameworkCore.StorageInterpretor.Strategy
{
    internal static class EFInterpretorOptionsExtension
    {
        public static EFInterpretorOptions AddCoreStrategies(this EFInterpretorOptions options)
        {
            options.AddStrategy(new EntityAddIgnoredMemberStrategy());
            options.AddStrategy(new EntityAddQueryFilterStrategy());
            options.AddStrategy(new PropertyConfigStrategy());

            return options;
        }
    }
}
