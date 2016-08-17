using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace Transformation.Loader
{
    public interface IGlobalDictionaryBuilder
    {
        ReadOnlyDictionary<string, object> Build(XElement config);
    }
}