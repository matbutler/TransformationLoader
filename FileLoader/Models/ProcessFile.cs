using System.Xml.Linq;

namespace FileProcessing.Loader.Models
{
    public class ProcessFile
    {
        public string FilePath { get; set; }
        public XElement Config { get; set; }
    }
}
