using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace MiCake.SqlReader
{
    [XmlRoot]
    [Serializable]
    public class SqlConfig
    {
        [XmlArrayItem(nameof(SqlValue))] public List<SqlValue> SqlValues { get; set; }
    }

    [Serializable]
    public class SqlValue
    {
        [XmlAttribute] public string SqlKey { get; set; }

        [XmlElement] public string CommandText { get; set; }

        public bool IsEmpty()
        {
            return string.IsNullOrWhiteSpace(this.CommandText);
        }

        public override string ToString()
        {
            return this.CommandText.ToString();
        }
    }
}
