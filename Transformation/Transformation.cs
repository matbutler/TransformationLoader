using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Reflection;
using TransformationCore.Attributes;
using TransformationCore.Enums;
using TransformationCore.Exceptions;
using System.ComponentModel;
using TransformationCore.Helpers;
using TransformationCore.Interfaces;
using System.Collections.ObjectModel;

namespace TransformationCore
{
    public abstract class Transformation : ITransformation
    {
        protected List<TransformationFilter> _filters;
        protected List<TransformationField> _inputfields = new List<TransformationField>();
        protected List<TransformationField> _outputfields = new List<TransformationField>();
        protected ReadOnlyDictionary<string, object> GlobalData { get; set; }
        protected long RowNo { get; set; }

        public void Initialise(XElement configXML, ReadOnlyDictionary<string, object> globalData, ILogger logger)
        {
            GlobalData = globalData;

            if (configXML != null)
            {
                SetupFields(configXML);

                ConfigureFilters(configXML);
            }

            Initialise(configXML, logger);
        }

        private void SetupFields(XElement configXML)
        {
            var properties = this.GetType().GetProperties().Where(x => x.CanRead).ToList();
            foreach (var property in properties)
            {
                var attributes = Attribute.GetCustomAttributes(property, typeof(TransformationFieldAttrib));
                foreach (TransformationFieldAttrib attr in attributes)
                {
                    string PropName = property.Name.ToLower();

                    if (attr.FieldType == TransformationFieldTypeEnum.Config)
                    {
                        SetupConfigField(configXML, property, attr, PropName);
                    }
                    else
                    {
                        SetupInputOutputFields(configXML.Element("mappings"), property, attr, PropName);
                    }
                }
            }
        }

        private void SetupInputOutputFields(XElement mappingConfigXML, PropertyInfo property, TransformationFieldAttrib attr, string PropName)
        {
            if ((attr.FieldType == TransformationFieldTypeEnum.Input || attr.FieldType == TransformationFieldTypeEnum.InOut) && !property.CanWrite)
            {
                throw new ConfigException(string.Format("{0} Input Transformation Properties Must be Writtable", PropName));
            }

            var mappedName = mappingConfigXML == null || mappingConfigXML.Element(PropName) == null ? "" : mappingConfigXML.Element(PropName).ToString();
            var tranFld = new TransformationField()
            {
                Name = PropName,
                Map = !string.IsNullOrEmpty(mappedName) ? mappedName : PropName,
                Required = attr.Required,
                FldType = attr.FieldType,
            };

            if (tranFld.Map.StartsWith("@"))
            {
                tranFld.IsGlobalVar = true;
                object globalObject = null;
                GlobalData.TryGetValue(tranFld.Map, out globalObject);

                tranFld.GlobalVal = globalObject;
            }

            tranFld.PropertyDefault = property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null;
            tranFld.PropertyGet = LinqAccessors.BuildGetAccessor(property.GetGetMethod());
            tranFld.PropertySet = LinqAccessors.BuildSetAccessor(property.GetSetMethod());

            if (attr.FieldType == TransformationFieldTypeEnum.Input || attr.FieldType == TransformationFieldTypeEnum.InOut)
            {
                _inputfields.Add(tranFld);
            }

            if (attr.FieldType == TransformationFieldTypeEnum.Output || attr.FieldType == TransformationFieldTypeEnum.InOut)
            {
                _outputfields.Add(tranFld);
            }
        }

        private void SetupConfigField(XElement configXML, PropertyInfo p, TransformationFieldAttrib attr, string PropName)
        {
            var configElement = configXML.Element("config");
            if (configElement != null && configElement.Element(PropName) != null)
            {
                string configProp = configElement.Element(PropName).Value;

                object configObj = TypeDescriptor.GetConverter(p.PropertyType).ConvertFromString(configElement.Element(PropName).Value);

                if (configObj == null)
                {
                    throw new ConfigException(string.Format("{0} invalid type conversion", PropName));
                }

                p.SetValue(this, configObj, null);
            }
            else if (attr.Required)
            {
                throw new ConfigException(string.Format("{0} Requires a Value", PropName));
            }
        }

        protected abstract void Initialise(XElement configXML, ILogger logger);
        protected abstract void Transform();

        public virtual void Close()
        {

        }

        protected virtual void PreTransform(Dictionary<string, object> row)
        {
        }

        protected virtual void PostTransform(Dictionary<string, object> row)
        {
        }

        private void ConfigureFilters(XElement configXML)
        {
            _filters = new List<TransformationFilter>();
            if (configXML.Element("config") != null && configXML.Element("config").Elements("filter") != null)
            {
                foreach (var filterConfig in configXML.Elements("config").Elements("filter"))
                {
                    _filters.Add(new TransformationFilter(filterConfig));
                }
            }
        }

        public void Transform(Dictionary<string, object> row)
        {
            RowNo = (long)row["#row"];

            if (_filters != null)
            {
                foreach (var filter in _filters)
                {
                    if (!filter.Check(row[filter.FieldName]))
                    {
                        return;
                    }
                }
            }

            PreTransform(row);

            foreach (var fld in _inputfields)
            {
                SetInputField(row, fld);
            }

            foreach (var fld in _outputfields.Where(f => f.FldType == TransformationFieldTypeEnum.Output))
            {
                fld.PropertySet(this, fld.PropertyDefault);
            }

            Transform();

            foreach (var fld in _outputfields)
            {
                row[fld.Map] = fld.PropertyGet(this);
            }

            PostTransform(row);
        }

        private void SetInputField(Dictionary<string, object> row, TransformationField fld)
        {
            try
            {
                object rowVal = null;

                if (fld.IsGlobalVar)
                {
                    rowVal = fld.GlobalVal;
                }
                else
                {
                    rowVal = row[fld.Map];
                }

                if (rowVal != null && fld.PropertySet != null)
                {
                    fld.PropertySet(this, rowVal);
                }
                else if (fld.Required)
                {
                    throw new TransformationException(string.Format("{1} ({0}) is Empty and Required", fld.Name, fld.Map));
                }
                else
                {
                    fld.PropertySet(this, fld.PropertyDefault);
                }
            }
            catch (Exception ex)
            {
                throw new TransformationException(string.Format("{0} - {2} ({1})", ex.Message, fld.Name, fld.Map));
            }
        }
    }
}
