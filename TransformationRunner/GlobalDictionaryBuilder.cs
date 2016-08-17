using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using TransformationCore.Helpers;

namespace Transformation.Loader
{
    public class GlobalDictionaryBuilder : IGlobalDictionaryBuilder
    {
        public ReadOnlyDictionary<string, object> Build(XElement config)
        {
            var data = new Dictionary<string, object>();

            foreach (var globalEl in config.Elements("globalvar"))
            {
                if (globalEl.Attribute("name") != null && globalEl.Attribute("value") != null && globalEl.Attribute("valuetype") != null)
                {
                    try
                    {
                        var converter = TypeConverter.GetConverter(globalEl.Attribute("valuetype")?.Value, globalEl.Attribute("dateformat")?.Value);
                        var val = converter(globalEl.Attribute("value")?.Value);
                        data.Add(globalEl.Attribute("name").Value, val);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format("Error setting up global var {0} - {1}", globalEl.Attribute("name").ToString(), ex.Message));
                    }
                }
                else
                {
                    throw new Exception(string.Format("Error setting up global var {0} missing (name, value, valuetype)", globalEl.ToString()));
                }
            }

            return new ReadOnlyDictionary<string, object>(data);
        }
    }
}
