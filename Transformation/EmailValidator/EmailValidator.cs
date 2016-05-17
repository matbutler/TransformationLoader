using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TransformationCore;
using TransformationCore.Attributes;
using TransformationCore.Enums;
using TransformationCore.Interfaces;

namespace EmailValidator
{
    [Export(typeof(ITransformation))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ExportMetadata("Name", "EmailValidator")]
    [ExportMetadata("Version", "1.0.0")]
    public class EmailValidator : Transformation
    {
        [TransformationFieldAttrib(TransformationFieldTypeEnum.Input, true)]
        public string Email { get; set; }

        [TransformationFieldAttrib(TransformationFieldTypeEnum.Output, false)]
        public bool Valid { get; set; }

        protected override void Initialise(XElement configXML, ILogger logger)
        {
        }

        protected override void Transform()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                Valid = false;
                return;
            }

            Valid = true;
        }
    }
}
