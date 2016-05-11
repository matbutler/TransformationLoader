using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransformationCore.Enums;

namespace TransformationCore.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class TransformationFieldAttrib : Attribute
    {

        public TransformationFieldTypeEnum FieldType { get; private set; }
        public bool Required { get; private set; }

        public TransformationFieldAttrib(TransformationFieldTypeEnum tranType, bool req)
        {
            FieldType = tranType;
            Required = req;
        }

        public override string ToString()
        {
            return "Transformation Field";
        }

    }
}
