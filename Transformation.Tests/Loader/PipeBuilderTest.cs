using Logging;
using Moq;
using NUnit.Framework;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Xml.Linq;
using Transformation.Loader;
using TransformationCore;
using TransformationCore.Exceptions;
using TransformationCore.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition.Primitives;

namespace Transformation.Tests.Loader
{
    [TestFixture]
    public class PipeBuilderTest
    {
        public static void ComposeExportedValue(CompositionContainer container, object exportedValue)
        {
            CompositionBatch batch = new CompositionBatch();

            var metadata = new Dictionary<string, object> {
                { "ExportTypeIdentity", "TransformationCore.ITransformation"},
                { "Name", "test" }
            };

            batch.AddExport(new Export("TransformationCore.ITransformation", metadata, () => exportedValue));
            
            container.Compose(batch);
        }

        private static IPipeBuilder BuildPipeBuilder(string configXML, ITransformation transformation = null)
        {
            var mockLogger = new Mock<ILogger>();
            var config = XElement.Parse(configXML);
            var globalData = new GlobalData();

            var container = new CompositionContainer();

            if (transformation != null)
            {
                ComposeExportedValue(container, transformation);
            }

            return new PipeBuilder(config, globalData, mockLogger.Object, container);
        }

        [Test]
        public void PipeBuilder_CTOR_MissingConfig_ThrowsException()
        {
            var exception = Assert.Catch(() => BuildPipeBuilder("<step></step>"));

            Assert.That(exception, Is.InstanceOf<TransformationPipeException>());
            Assert.That(((TransformationPipeException)exception).Message, Is.EqualTo("Missing Pipe Config"));
        }

        [Test]
        public void PipeBuilder_CTOR_MissingTransformations_ThrowsException()
        {
            var exception = Assert.Catch(() => BuildPipeBuilder("<step><pipe></pipe></step>"));

            Assert.That(exception, Is.InstanceOf<TransformationPipeException>());
            Assert.That(((TransformationPipeException)exception).Message, Is.EqualTo("Config has no Transformations"));
        }

        [Test]
        public void PipeBuilder_Build_MissingTransformationName_ThrowsException()
        {
            var pipeBuilder = BuildPipeBuilder("<step><pipe><transformation /></pipe></step>");

            var exception = Assert.Catch(() => pipeBuilder.Build(1));

            Assert.That(exception, Is.InstanceOf<TransformationPipeException>());
            Assert.That(((TransformationPipeException)exception).Message, Does.Contain("Missing Name"));
        }

        [Test]
        public void PipeBuilder_Build_WithValidTransformation_AddsToPipe()
        {
            var mockTransformation = new Mock<ITransformation>();

            var pipeBuilder = BuildPipeBuilder("<step><pipe><transformation name=\"test\"/></pipe></step>", mockTransformation.Object);

            var pipe = pipeBuilder.Build(1);

            Assert.That(pipe.Count, Is.EqualTo(1));
        }

        [Test]
        public void PipeBuilder_Build_WithValidTransformation_AddsCorrectTransformation()
        {
            var mockTransformation = new Mock<ITransformation>();

            var pipeBuilder = BuildPipeBuilder("<step><pipe><transformation name=\"test\"/></pipe></step>", mockTransformation.Object);

            var pipe = pipeBuilder.Build(1);

            Assert.That(pipe["test - 1"], Is.EqualTo(mockTransformation.Object));
        }

        [Test]
        public void PipeBuilder_Build_WithValidTransformation_CallTransformationInitialise()
        {
            var mockTransformation = new Mock<ITransformation>();

            var pipeBuilder = BuildPipeBuilder("<step><pipe><transformation name=\"test\"/></pipe></step>", mockTransformation.Object);

            var pipe = pipeBuilder.Build(1);

            mockTransformation.Verify(x => x.Initialise(It.IsAny<XElement>(), It.IsAny<GlobalData>(), It.IsAny<ILogger>(), It.Is<int>(y => y == 1)), Times.Once);
        }
    }
}
