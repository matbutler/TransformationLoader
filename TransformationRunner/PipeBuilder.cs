using Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Xml.Linq;
using TransformationCore;
using TransformationCore.Exceptions;
using TransformationCore.Models;

namespace Transformation.Loader
{
    public class PipeBuilder : IPipeBuilder
    {
        private readonly ILogger _logger;
        private readonly CompositionContainer _container;
        private readonly IEnumerable<XElement> _transactionElements;
        private readonly GlobalData _globalData;

        public PipeBuilder(XElement config, GlobalData globalData, ILogger logger, CompositionContainer container)
        {
            _logger = logger;
            _globalData = globalData;

            _container = container;

            var pipeElement = config.Element("pipe");
            if (pipeElement == null)
            {
                throw new TransformationPipeException("Missing Pipe Config");
            }
            else if (pipeElement.Elements("transformation").Count() == 0)
            {
                throw new TransformationPipeException("Config has no Transformations");
            }

            _transactionElements = pipeElement.Elements("transformation");
        }

        public Dictionary<string, ITransformation> Build(int pipeNumber)
        {
            var pipe = new Dictionary<string, ITransformation>();

            int count = 0;
            foreach (var tranConfig in _transactionElements)
            {
                count++;

                var tranName = tranConfig.Attribute("name")?.Value;

                if (string.IsNullOrWhiteSpace(tranName))
                {
                    throw new TransformationPipeException(string.Format("Incorrect Transformation Config : Missing Name : {0}", tranConfig));
                }

                string tranVersion = tranConfig.Attribute("version")?.Value ?? "";

                try
                {
                    var _transformationFactory = new MefFactory<ITransformation>();
                    _container.ComposeParts(_transformationFactory);

                    var tran = _transformationFactory.CreateComponent(tranName, tranVersion);

                    tran.Initialise(tranConfig, _globalData, _logger, pipeNumber);

                    pipe.Add(string.Format("{0} - {1}", tranName, count), tran);

                    if(pipeNumber > 1)
                    {
                        _logger.Debug(string.Format("Pipe {0}: Transformation {1} = {2}", pipeNumber, count, tranName));
                    }
                    else
                    {
                        _logger.Info(string.Format("Pipe {0}: Transformation {1} = {2}", pipeNumber, count, tranName));
                    }
                }
                catch (Exception ex)
                {
                    _logger.Fatal(string.Format("Pipe {0}: Unable to Create Transformation {1} : {2} ({3})", pipeNumber, count, tranName, ex.Message));
                    throw new TransformationPipeException(string.Format("Unable to Create Transformation {0} : {1}", tranName, ex.Message));
                }
            }

            return pipe;
        }
    }
}
