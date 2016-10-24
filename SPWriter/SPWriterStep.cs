using Logging;
using SPWriter.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TransformationCore;
using TransformationCore.Exceptions;
using TransformationCore.Interfaces;
using TransformationCore.Models;

namespace SPWriter
{
    [Export(typeof(IProcessStep))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ExportMetadata("Name", "SPWriterStep")]
    [ExportMetadata("Version", "1.0.0")]
    public class SPWriterStep : IProcessStep
    {
        private string _storedProcedure;
        private string _connStr;
        private List<ParameterMap> _parameters;

        public void Initialise(XElement config, CancellationTokenSource cancellationTokenSource, ILogger logger, IRowLogger rowlogger, CompositionContainer container)
        {
            if (config == null)
            {
                throw new ConfigException("Missing Config");
            }

            _storedProcedure = config.Attribute("procedure")?.Value;

            if (string.IsNullOrWhiteSpace(_storedProcedure))
            {
                throw new ConfigException("Missing stored procedure name");
            }

            _connStr = config.Attribute("connection")?.Value;

            _parameters =  PropertyMapper.Map(config);
        }

        private void SetGlobalParameters(GlobalData globalData)
        {
            var globalParameters = _parameters.Where(x => x.IsGlobal).ToList();

            globalParameters.ForEach(x => x.Value = globalData.Data[x.Map]);
        }

        public async Task<bool> Process(XElement processInfo, GlobalData globalData, bool previousStepSucceeded = true)
        {
            if (string.IsNullOrWhiteSpace(_connStr))
            {
                _connStr = globalData.Data["connection"].ToString();
            }

            SetGlobalParameters(globalData);

            using (var conn = new SqlConnection(_connStr))
            using (var command = new SqlCommand(_storedProcedure, conn)
            {
                CommandType = CommandType.StoredProcedure
            })
            {
                foreach (var parameter in _parameters)
                {
                    if (parameter.Size.HasValue)
                    {
                        command.Parameters.Add(parameter.Name, parameter.DbType, parameter.Size.Value).Value = parameter.Value;
                    }
                    else
                    {
                        command.Parameters.Add(parameter.Name, parameter.DbType).Value = parameter.Value;
                    }
                }

                conn.Open();
                await command.ExecuteNonQueryAsync();
            }

            return true;
        }
    }
}
