namespace MiCake.SqlReader.XmlProvider
{
    /// <summary>
    /// </summary>
    public class XmlFileSqlReaderOptions
    {
        /// <summary>
        /// The folder path to the sql xml file.
        /// </summary>
        public string FolderPath { get; set; } = string.Empty;

        /// <summary>
        /// Indicate which file suffix can be loaded
        /// </summary>
        public string FileSuffix { get; set; } = ".xml";
    }
}
