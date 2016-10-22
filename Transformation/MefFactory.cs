using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace TransformationCore
{
    public class MefFactory<T>
    {

#pragma warning disable 0649
        [ImportMany(RequiredCreationPolicy = CreationPolicy.NonShared)]
        private Lazy<T, IDictionary<string, object>>[] _availableComponents;
#pragma warning restore 0649

        /// <summary> 
        /// The Allows MEF to produce multiple instances of the same Component Classes
        /// </summary> 
        /// <param name="name">The name of the Compoent Class to use</param> 
        public T CreateComponent(string name, string version)
        {
            foreach (var component in _availableComponents)
            {
                if (component.Metadata.ContainsKey("Name") && component.Metadata["Name"].ToString().ToUpper() == name.ToUpper() && (version == string.Empty || component.Metadata["Version"].ToString() == version))
                {
                    return component.Value;
                }
            }

            throw new Exception(string.Format("Unable to Locate Component {0}", name));
        }
    }
}
