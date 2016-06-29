using System.Collections.Generic;
using TransformationCore;

namespace Transformation.Loader
{
    public interface IPipeBuilder
    {
        Dictionary<string, ITransformation> Build(int pipeNumber);
    }
}