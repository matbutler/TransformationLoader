using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransformationCore.Models
{
    public class ReaderField
    {
        public string Name { get; set; }
        public int? Index { get; set; }
        public Func<string, object> Converter { get; set; }
    }
}
