using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransformationCore.Enums;

namespace TransformationCore
{
    public class TransformationField
    {
        public TransformationFieldTypeEnum FldType { get; set; }
        public string Name { get; set; }
        public string Map { get; set; }
        public bool IsProperty { get; set; }
        public bool IsGlobalVar { get; set; }
        public object GlobalVal { get; set; }
        public bool Required { get; set; }
        public Func<object, object> PropertyGet { get; set; }
        public Action<object, object> PropertySet { get; set; }
        public object PropertyDefault { get; set; }
    }
}
