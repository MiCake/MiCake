namespace MiCake.EntityFrameworkCore.Interprets.Strategies
{
    internal static class EFExpressionOptionsExtension
    {
        public static EFExpressionOptions AddCoreStrategies(this EFExpressionOptions options)
        {
            options.AddStrategy(new EntityAddIgnoredMemberStrategy());
            options.AddStrategy(new EntityAddQueryFilterStrategy());
            options.AddStrategy(new PropertyConfigStrategy());

            return options;
        }
    }
}
