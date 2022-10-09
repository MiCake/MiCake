using System.Xml.Serialization;

namespace MiCake.SqlReader
{
    [XmlRoot]
    [Serializable]
    public class SqlConfig
    {
        [XmlArrayItem(nameof(SqlValue))]
        public List<SqlValue> SqlValues { get; set; } = new();
    }

    [Serializable]
    public class SqlValue
    {
        [XmlAttribute]
        public string SqlKey { get; set; } = string.Empty;

        [XmlElement]
        public string CommandText { get; set; } = string.Empty;

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
