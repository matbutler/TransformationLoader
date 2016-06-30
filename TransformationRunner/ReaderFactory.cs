using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using TransformationCore.Interfaces;

namespace Transformation.Loader
{
    public class ReaderFactory
    {

#pragma warning disable 0649
        [ImportMany(RequiredCreationPolicy = CreationPolicy.NonShared)]
        private Lazy<IReader, IDictionary<string, object>>[] _availableReaders;
#pragma warning restore 0649

        /// <summary> 
        /// The Allows MEF to produce multiple instances of the same transformation Classes
        /// </summary> 
        /// <param name="readerName">The name of the Transformation Class to use</param> 
        public IReader CreateReader(string readerName, string version)
        {
            foreach (var reader in _availableReaders)
            {
                if (reader.Metadata.ContainsKey("Name") && reader.Metadata["Name"].ToString().ToUpper() == readerName.ToUpper() && (version == string.Empty || reader.Metadata["Version"].ToString().ToUpper() == version.ToUpper()))
                {
                    return reader.Value;
                }
            }

            throw new Exception(string.Format("Unable to Locate Reader {0}", readerName));
        }
    }
}
