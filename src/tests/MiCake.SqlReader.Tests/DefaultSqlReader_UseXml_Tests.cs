using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MiCake.SqlReader.Tests
{
    public class DefaultSqlReader_UseXml_Tests
    {
        [Fact]
        public void GetSqlValue_GivenRightSectionNameAndSqlKey_ShouldHasMatchingData()
        {
            var sqlReader = GetSqlReader();

            var result = sqlReader.Get("Test.Query.1", "Demo-Sql");
            var result_g = sqlReader.Get<SqlValue>("Test.Query.1", "Demo-Sql");

            Assert.NotNull(result);
            Assert.NotNull(result_g);
        }

        [Fact]
        public void GetSqlValue_GivenWrongSectionNameAndExistSqlKey_ShouldNoData()
        {
            var sqlReader = GetSqlReader();

            var result = sqlReader.Get("Test.Query.1", "Wrong-Sub-Demo-Sql");
            var result_g = sqlReader.Get<SqlValue>("Test.Query.1", "Wrong-Sub-Demo-Sql");

            Assert.Null(result);
            Assert.Null(result_g);
        }

        public static ISqlReader GetSqlReader()
        {
            var services = new ServiceCollection();

            services.AddSqlReader(options =>
            {
                options.UseXmlFileProvider(xmlOptions =>
                {
                    xmlOptions.FolderPath = "Data";
                });
            });

            return services.BuildServiceProvider().GetService<ISqlReader>();
        }
    }
}
