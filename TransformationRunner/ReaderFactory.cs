using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Xml.Linq;
using TransformationCore;
using TransformationCore.Interfaces;

namespace Transformation.Loader
{
    public class ReaderFactory : MefFactory<IReader>
    {
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

                return CreateComponent(readerName, readerVersion);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Unable to create Loader {0} : {1}", readerName, ex.Message));
            }
        }
    }
}
