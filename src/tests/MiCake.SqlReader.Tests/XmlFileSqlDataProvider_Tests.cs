using MiCake.SqlReader.XmlProvider;
using System;
using Xunit;

namespace MiCake.SqlReader.Tests
{
    public class XmlFileSqlDataProvider_Tests
    {
        const string RightFolderName = "Data";
        const string WrongFolderName = "Data-Wrong";

        public XmlFileSqlDataProvider_Tests()
        {
        }

        [Fact]
        public void PopulateSql_GivenRightFolder_ShouldLoadAllData()
        {
            var provider = new XmlFileSqlDataProvider(new XmlFileSqlReaderOptions
            {
                FolderPath = RightFolderName
            });

            provider.PopulateSql();

            Assert.NotEmpty(provider.SqlValueData);
        }

        [Fact]
        public void PopulateSql_GivenWrongFolder_ShouldHasException()
        {
            var provider = new XmlFileSqlDataProvider(new XmlFileSqlReaderOptions
            {
                FolderPath = WrongFolderName
            });

            Assert.ThrowsAny<Exception>(() =>
            {
                provider.PopulateSql();
            });
        }

        [Fact]
        public void GetSqlValue_GivenExistSqlKey_ShouldHasMatchingData()
        {
            var provider = new XmlFileSqlDataProvider(new XmlFileSqlReaderOptions
            {
                FolderPath = RightFolderName
            });
            provider.PopulateSql();

            var result = provider.Get("Test.Query.1");
            var result_g = provider.Get<SqlValue>("Test.Query.1");

            Assert.NotNull(result);
            Assert.NotNull(result_g);
        }

        [Fact]
        public void GetSqlValue_GivenNoExistSqlKey_ShouldNoData()
        {
            var provider = new XmlFileSqlDataProvider(new XmlFileSqlReaderOptions
            {
                FolderPath = RightFolderName
            });
            provider.PopulateSql();

            var result = provider.Get("Null.Test.Query.1");
            var result_g = provider.Get<SqlValue>("Null.Test.Query.1");

            Assert.Null(result);
            Assert.Null(result_g);
        }

        [Fact]
        public void GetSqlValue_GivenRightSectionNameAndSqlKey_ShouldHasMatchingData()
        {
            var provider = new XmlFileSqlDataProvider(new XmlFileSqlReaderOptions
            {
                FolderPath = RightFolderName
            });
            provider.PopulateSql();

            var result = provider.Get("Test.Query.1", "Demo-Sql");
            var result_g = provider.Get<SqlValue>("Test.Query.1", "Demo-Sql");

            Assert.NotNull(result);
            Assert.NotNull(result_g);
        }

        [Fact]
        public void GetSqlValue_GivenWrongSectionNameAndExistSqlKey_ShouldNoData()
        {
            var provider = new XmlFileSqlDataProvider(new XmlFileSqlReaderOptions
            {
                FolderPath = RightFolderName
            });
            provider.PopulateSql();

            var result = provider.Get("Test.Query.1", "Wrong-Sub-Demo-Sql");
            var result_g = provider.Get<SqlValue>("Test.Query.1", "Wrong-Sub-Demo-Sql");

            Assert.Null(result);
            Assert.Null(result_g);
        }
    }
}
