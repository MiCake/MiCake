﻿using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MiCake.SqlReader.XmlProvider
{
    internal class XmlFileSqlDataProvider : BaseSqlDataProvider
    {
        private readonly XmlFileSqlReaderOptions _options;

        public XmlFileSqlDataProvider(XmlFileSqlReaderOptions options)
        {
            _options = options ?? new XmlFileSqlReaderOptions();
        }

        public override void Dispose()
        {
            SqlValueData.Clear();
            SqlValueData = null;
        }

        public override void PopulateSql()
        {
            var allFiles = Directory.EnumerateFiles(_options.FolderPath, $"*{_options.FileSuffix}", SearchOption.AllDirectories);

            foreach (var fileItem in allFiles)
            {
                var currentXmlFile = new FileInfo(fileItem);
                string text = File.ReadAllText(fileItem);

                if (string.IsNullOrWhiteSpace(text))
                    continue;

                var sqlConfig = XmlToObject<SqlConfig>(text);

                SqlValueData.Add(currentXmlFile.Name.Replace(_options.FileSuffix, ""), sqlConfig.SqlValues.ToDictionary(s => s.SqlKey));
            }
        }


        protected T XmlToObject<T>(string xml) where T : class
        {
            var serializer = new XmlSerializer(typeof(T));
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            return (T)serializer.Deserialize(ms);
        }
    }
}
