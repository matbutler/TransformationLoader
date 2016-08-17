using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Xml.Linq;
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
        private IReader CreateReader(string readerName, string version)
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

        public IReader GetReader(XElement readerConfig)
        {
            string readerName = string.Empty;
            string readerVersion = string.Empty;

            try
            {
                readerName = readerConfig?.Attribute("name")?.Value;

                readerVersion = readerConfig?.Attribute("version")?.Value ?? "";

                if (string.IsNullOrWhiteSpace(readerName))
                {
                    throw new Exception("Reader name missing from Config.");
                }

                var catalog = new DirectoryCatalog("Engine");
                var container = new CompositionContainer(catalog);

                container.ComposeParts(this);

                return CreateReader(readerName, readerVersion);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Unable to create Loader {0} : {1}", readerName, ex.Message));
            }
        }
    }
}
