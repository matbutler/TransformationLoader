using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using TransformationCore;
using TransformationCore.Exceptions;

namespace Transformation.Loader
{
    public class TransformationFactory
    {

#pragma warning disable 0649
        [ImportMany(RequiredCreationPolicy = CreationPolicy.NonShared)]
        private Lazy<ITransformation, IDictionary<string, object>>[] _availableTransformations;
#pragma warning restore 0649

        /// <summary> 
        /// The Allows MEF to produce multiple instances of the same transformation Classes
        /// </summary> 
        /// <param name="tranName">The name of the Transformation Class to use</param> 
        public ITransformation CreateTransformation(string tranName, string version)
        {
            foreach (var tran in _availableTransformations)
            {
                if (tran.Metadata.ContainsKey("Name") && tran.Metadata["Name"].ToString().ToUpper() == tranName.ToUpper() && (version == string.Empty || tran.Metadata["Version"].ToString() == version))
                {
                    return tran.Value;
                }
            }

            throw new TransformationPipeException(string.Format("Unable to Locate Transformation {0}", tranName));
        }
    }
}
