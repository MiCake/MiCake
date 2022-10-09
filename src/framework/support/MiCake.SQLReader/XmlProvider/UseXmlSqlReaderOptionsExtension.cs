using MiCake.SqlReader.XmlProvider;

namespace MiCake.SqlReader
{
    public static class UseXmlSqlReaderOptionsExtension
    {
        public static void UseXmlFileProvider(this MiCakeSqlReaderOptions sqlReaderOptions, Action<XmlFileSqlReaderOptions>? optionsAction = null)
        {
            var options = new XmlFileSqlReaderOptions();
            optionsAction?.Invoke(options);

            sqlReaderOptions.AddProvider(new XmlFileSqlDataProvider(options));
        }

        public static void UseXmlFileProvider(this MiCakeSqlReaderOptions sqlReaderOptions, XmlFileSqlReaderOptions options)
        {
            options ??= new XmlFileSqlReaderOptions();
            sqlReaderOptions.AddProvider(new XmlFileSqlDataProvider(options));
        }
    }
}
