using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TransformationCore.Interfaces;

namespace TransformationCore
{
    public class PipeRunner
    {


        #region "Public Events"
        public event StartedEventHandler Started;
        public delegate void StartedEventHandler();
        public event FinishedEventHandler Finished;
        public delegate void FinishedEventHandler(bool success);
        #endregion

        #region "Private Variables"
        private Stopwatch _sw = new Stopwatch();

        private System.Threading.Timer _LogTimer;
        private ILogger _logger;
        private CancellationTokenSource _tokenSource;
        private BlockingCollection<Dictionary<string, object>> _inputQueue;
        private string _connstr;
        private bool _logging = false;
        private bool _isfile = false;
        private int _fileLogId;
        private int _startRow = 0;
        private int _fileState = 0;
        private bool _running = false;
        private string _filePattern = string.Empty;
        private string _fileDateFormat = string.Empty;
        private string _filename = string.Empty;
        private string _filepath = string.Empty;
        private bool _backedup;
        #endregion

        #region "Public Properties"
        private int _pipeCount;
        public int PipeCount
        {
            get { return _pipeCount; }
        }

        private int _activePipeCount;
        public int ActivePipeCount
        {
            get { return _activePipeCount; }
        }

        private int _rowprocessedCount;
        public int RowprocessedCount
        {
            get { return _rowprocessedCount; }
        }

        private int _rowSkippedCount;
        public int RowSkippedCount
        {
            get { return _rowSkippedCount; }
        }

        private int _rowErrorCount;
        public int RowErrorCount
        {
            get { return _rowErrorCount; }
        }

        private Dictionary<string, object> _globalData;
        public Dictionary<string, object> GlobalData
        {
            get { return _globalData; }
        }

        private XElement _config;
        public XElement Config
        {
            get { return _config; }
            set { _config = value; }
        }

        #endregion

        #region "Private Functions"

        private async void FinishProcess(bool success, string errorMsg)
        {
            if (_logging)
            {
                FinishLogging(success, errorMsg);
            }
            if (_isfile)
            {
                await FileOperations.ProcessCompletedFile(_filepath, _filename, _backedup, _logger);
            }

            _sw.Stop();
            _LogTimer.Dispose();

            _logger.Log(string.Format("Process : Finished ({1:#,##0} rows ({2:#,##0} skipped) in {0:#,##0.00} Secs / {3:#,##0} RPS)", _sw.Elapsed.TotalSeconds, _rowprocessedCount, _rowSkippedCount, (_rowprocessedCount + _rowSkippedCount) / _sw.Elapsed.TotalSeconds), MessageLevel.Action);

            //Sleep to give log a chance to update before completion
            Thread.Sleep(1000);

            //Reset Vars
            _inputQueue = null;
            _globalDS = null;
            _tokenSource = null;
            _rowsProcessedTable = null;
            _rowErrorCount = 0;
            _rowprocessedCount = 0;
            _rowSkippedCount = 0;
            _pipeCount = 0;
            _startRow = 0;
            _connstr = "";
            _logging = false;

            _running = false;
            if (Finished != null)
            {
                Finished(success);
            }
        }

        private void StartLogging()
        {
            _fileLogId = -1;

            using (var loggingConn = new SqlConnection(_connstr))
            {
                loggingConn.Open();
                using (var fileLogCmd = loggingConn.CreateCommand())
                {

                    fileLogCmd.CommandText = "dbo.usp_filelogging_Start";
                    fileLogCmd.CommandType = CommandType.StoredProcedure;
                    fileLogCmd.CommandTimeout = 8600;

                    fileLogCmd.Parameters.Clear();
                    fileLogCmd.Parameters.AddWithValue("@FL_Filename", _filename);
                    fileLogCmd.Parameters.AddWithValue("@FL_User", StringFunctions.Truncate(string.Format("SYS:{0}", Environment.UserName), 50));

                    var myParm = fileLogCmd.Parameters.Add("@FL_ID", SqlDbType.Int, 0);
                    myParm.Direction = ParameterDirection.Output;

                    myParm = fileLogCmd.Parameters.Add("@FL_StartRow", SqlDbType.Int, 0);
                    myParm.Direction = ParameterDirection.Output;

                    myParm = fileLogCmd.Parameters.Add("@FL_State", SqlDbType.TinyInt, 0);
                    myParm.Direction = ParameterDirection.Output;

                    myParm = fileLogCmd.Parameters.Add("@BLF_Pattern", SqlDbType.NVarChar, 255);
                    myParm.Direction = ParameterDirection.Output;

                    myParm = fileLogCmd.Parameters.Add("@BLF_FileDateFormat", SqlDbType.VarChar, 20);
                    myParm.Direction = ParameterDirection.Output;

                    try
                    {
                        using (var myReader = fileLogCmd.ExecuteReader())
                        {
                            _rowsProcessedTable = new DataTable();
                            _rowsProcessedTable.Load(myReader);

                            //'Set Primary Key
                            DataColumn[] pkc = new DataColumn[2];
                            pkc[0] = _rowsProcessedTable.Columns["RowNumber"];
                            _rowsProcessedTable.PrimaryKey = pkc;

                            if (fileLogCmd.Parameters["@FL_ID"].Value == DBNull.Value)
                            {
                                _fileLogId = Convert.ToInt32(fileLogCmd.Parameters["@FL_ID"].Value);
                            }
                            else
                            {
                                throw new Exception("FL_Id is Null");
                            }

                            if (fileLogCmd.Parameters["@FL_StartRow"].Value == DBNull.Value)
                            {
                                _startRow = Convert.ToInt32(fileLogCmd.Parameters["@FL_StartRow"].Value);
                            }
                            else
                            {
                                _startRow = 0;
                            }

                            if (fileLogCmd.Parameters["@FL_State"].Value == DBNull.Value)
                            {
                                _fileState = Convert.ToInt16(fileLogCmd.Parameters["@FL_State"].Value);
                            }
                            else
                            {
                                _fileState = 0;
                            }

                            if (fileLogCmd.Parameters["@BLF_Pattern"].Value == DBNull.Value)
                            {
                                _filePattern = fileLogCmd.Parameters["@BLF_Pattern"].Value.ToString();
                            }
                            else
                            {
                                _filePattern = string.Empty;
                            }

                            if (fileLogCmd.Parameters["@BLF_FileDateFormat"].Value == DBNull.Value)
                            {
                                _fileDateFormat = fileLogCmd.Parameters["@BLF_FileDateFormat"].Value.ToString();
                            }
                            else
                            {
                                _fileDateFormat = "yyMMdd";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format("Error Starting Logging : {0}", ex.Message));
                    }
                }
                loggingConn.Close();
            }
        }

        private void FinishLogging(bool success, string errorMsg)
        {
            try
            {
                if (_fileLogId == -1)
                {
                    throw new Exception("Invalid File Logging ID, usp_filelogging_Start Failure");
                }

                using (var loggingConn = new SqlConnection(_connstr))
                {
                    loggingConn.Open();
                    using (var fileLogCmd = loggingConn.CreateCommand())
                    {
                        fileLogCmd.CommandText = "dbo.usp_filelogging_Finish";
                        fileLogCmd.CommandType = CommandType.StoredProcedure;
                        fileLogCmd.CommandTimeout = 8600;

                        fileLogCmd.Parameters.Clear();
                        fileLogCmd.Parameters.AddWithValue("@FL_ID", _fileLogId);
                        fileLogCmd.Parameters.AddWithValue("@FL_Complete", success);
                        fileLogCmd.Parameters.AddWithValue("@FL_Error", errorMsg);
                        fileLogCmd.ExecuteNonQuery();
                    }
                    loggingConn.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.Log(string.Format("Error Finishing Logging : {0}", ex.Message), MessageLevel.Critical);
            }
        }

        private void RunStoredProcs(string prepost, bool success)
        {
            if (_config.Element(prepost) != null && !string.IsNullOrEmpty(_connstr))
            {
                try
                {
                    //Call pre process procedures
                    using (var trSqlConn = new SqlConnection(_connstr))
                    {
                        trSqlConn.Open();

                        foreach (var postProc in _config.Element(prepost).Elements("procedure"))
                        {
                            string proc = postProc.Attribute("name").Value;

                            _logger.Log(string.Format("Process : {0} = {1}", prepost, proc), MessageLevel.Action);
                            using (var sqlComm = trSqlConn.CreateCommand())
                            {
                                sqlComm.CommandText = proc;
                                sqlComm.CommandType = CommandType.StoredProcedure;
                                sqlComm.CommandTimeout = 86400;
                                //Seconds

                                if (prepost == "postprocess")
                                {
                                    sqlComm.Parameters.AddWithValue("@success", success);
                                }

                                //Load Parameters
                                sqlComm.Parameters.AddWithValue("@FL_ID", _fileLogId);

                                foreach (var prm in postProc.Elements("parameter"))
                                {
                                    SqlParameter tmpPrm = new SqlParameter();

                                    sqlComm.Parameters.AddWithValue(string.Format("@{0}", prm.Attribute("name").Value), prm.Attribute("value").Value);
                                }

                                sqlComm.ExecuteNonQuery();
                            }
                        }

                        trSqlConn.Close();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("{0} Error : {1}", prepost, ex.Message));
                }
            }
        }

        private void LogTimerTick(object state)
        {
            _logger.Log(string.Format("Process : {0} Active Pipes ({2:#,##0} rows ({3:#,##0} skipped) in {1:#,##0.00} Secs / {4:#,##0} RPS)", _activePipeCount, _sw.Elapsed.TotalSeconds, _rowprocessedCount, _rowSkippedCount, (_rowprocessedCount + _rowSkippedCount) / _sw.Elapsed.TotalSeconds), MessageLevel.Info);
        }

        private DataTable SetupGlobalVar()
        {
            var globalTable = _globalDS.Tables.Add("GLOBALVARS");

            globalTable.Columns.Add("Name", typeof(string));
            globalTable.Columns.Add("Value", typeof(object));
            globalTable.PrimaryKey = new DataColumn[] { globalTable.Columns["Name"] };

            foreach (var globalEl in _config.Elements("globalvar"))
            {
                if (globalEl.Attribute("name") != null && globalEl.Attribute("value") != null && globalEl.Attribute("valuetype") != null)
                {
                    var row = globalTable.NewRow();
                    row["Name"] = globalEl.Attribute("name").ToString();

                    object val = null;
                    string configVal = globalEl.Attribute("value").ToString();
                    try
                    {
                        switch (globalEl.Attribute("valuetype").ToString())
                        {
                            case "DATETIME":
                                if (globalEl.Attribute("dateformat") == null)
                                {
                                    throw new Exception("Missing dateformat");
                                }
                                val = DateTime.ParseExact(configVal, globalEl.Attribute("dateformat").ToString(), null);
                                break;
                            case "NUMBER":
                                val = long.Parse(configVal);
                                break;
                            case "LONG":
                                val = long.Parse(configVal);
                                break;
                            case "INT":
                                val = int.Parse(configVal);
                                break;
                            case "DECIMAL":
                                val = decimal.Parse(configVal);
                                break;
                            case "TEXT":
                                val = configVal;
                                break;
                            case "BOOL":
                                val = Convert.ToBoolean(configVal);
                                break;
                            default:
                                throw new Exception("Invalid Value Type");
                        };
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format("Error setting up global var {0} - {1}", globalEl.Attribute("name").ToString(), ex.Message));
                    }


                    row["Value"] = val;

                    globalTable.Rows.Add(row);
                }
                else
                {
                    throw new Exception(string.Format("Error setting up global var {0} missing (name, value, valuetype)", globalEl.ToString()));
                }
            }

            return globalTable;
        }
        #endregion

        #region "Public Functions"
        public void Start(ILogger logger)
        {
            Start(string.Empty, logger);
        }

        public async void Start(string filepath, ILogger logger)
        {
            //Create Logging Timer
            _LogTimer = new System.Threading.Timer(LogTimerTick, null, 10000, 10000);

            _logger = logger;

            if (_running)
            {
                _logger.Log("Process : The process is already running", MessageLevel.Critical);
                if (Finished != null)
                {
                    Finished(false);
                }
                return;
            }

            if (Config == null)
            {
                _logger.Log("Process : Config XML is Missing", MessageLevel.Critical);
                if (Finished != null)
                {
                    Finished(false);
                }
                return;
            }

            if (Config.Attribute("logging") != null)
            {
                bool.TryParse(Config.Attribute("logging").Value, out _logging);
            }

            if (Config.Element("connection") != null && Config.Element("connection").Attribute("value") != null)
            {
                _connstr = Config.Element("connection").Attribute("value").Value;
            }

            if (!string.IsNullOrEmpty(filepath))
            {
                _filename = Path.GetFileName(filepath);
                _filepath = Path.GetDirectoryName(filepath);

                var checkPath = string.Format("{0}\\Invalid", _filepath);
                if (!Directory.Exists(checkPath))
                {
                    Directory.CreateDirectory(checkPath);
                }

                checkPath = string.Format("{0}\\backup", _filepath);
                if (!Directory.Exists(checkPath))
                {
                    Directory.CreateDirectory(checkPath);
                }
            }


            //Initialisation
            _running = true;
            _tokenSource = new CancellationTokenSource();
            _rowErrorCount = 0;
            _rowprocessedCount = 0;
            _rowSkippedCount = 0;
            _sw.Reset();
            _globalDS = new DataSet();
            _backedup = false;
            _fileLogId = -1;
            _fileState = 1;

            _pipeCount = Convert.ToInt32(_config.Element("pipe").Attribute("threads").Value);

            int maxQueue = 50000;
            if (_config.Element("pipe").Attribute("queuesize") != null)
            {
                maxQueue = Convert.ToInt32(Config.Element("pipe").Attribute("queuesize").Value);
            }

            _inputQueue = new BlockingCollection<Hashtable>(maxQueue);

            CancellationToken token = _tokenSource.Token;
            Task[] ETLtasks = new Task[PipeCount + 1];


            _logger.Log(string.Format("Process : Started (Pipes = {0} : Logging = {1})", _pipeCount, _logging), MessageLevel.Action);

            try
            {
                IReader Loader = null;
                string readerName = "";
                string readerVersion = string.Empty;
                DateTime fileDate = DateTime.Now;

                // Always StartLogging to get FL_Id
                if (_logging)
                {
                    StartLogging();
                }

                foreach (var tran in _config.Descendants("transformation"))
                {
                    tran.Add(new XAttribute("fileid", _fileLogId));
                }

                try
                {
                    if (Config.Element("reader") != null && Config.Element("reader").Attribute("name") != null)
                    {
                        readerName = Config.Element("reader").Attribute("name").Value;

                        if (Config.Element("reader").Attribute("version") != null)
                        {
                            readerVersion = Config.Element("reader").Attribute("version").Value;
                        }
                    }
                    else
                    {
                        throw new Exception("Reader name missing from Config.");
                    }

                    var catalog = new DirectoryCatalog("Engine");
                    var container = new CompositionContainer(catalog);

                    ReaderFactory LdrFactory = new ReaderFactory();
                    container.ComposeParts(LdrFactory);

                    Loader = LdrFactory.CreateReader(readerName, readerVersion);

                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Unable to create Loader {0} : {1}", readerName, ex.Message));
                }

                _isfile = Loader is IFileReader;


                if (_isfile)
                {
                    await FileOperations.WaitWhileFileInUse(filepath);

                    ((IFileReader)Loader).Filename = filepath;
                }

                RunStoredProcs("preprocess", false);

                globalLookupTable = SetupGlobalVar();

                Loader.initialise(_config, _logger);

                if (_isfile)
                {
                    string backupFilePath = null;
                    int backupVersion = 0;

                    do
                    {
                        backupVersion += 1;
                        backupFilePath = string.Format("{0}\\Backup\\{1}_{3:000}{2}", _filepath, Path.GetFileNameWithoutExtension(_filename), Path.GetExtension(_filename), backupVersion);
                    } while ((File.Exists(backupFilePath)));

                    if (backupVersion > 1)
                    {
                        var prevBackupFilePath = string.Format("{0}\\Backup\\{1}_{3:000}{2}", _filepath, Path.GetFileNameWithoutExtension(_filename), Path.GetExtension(_filename), backupVersion - 1);
                        _logger.Log("Process : Validating File", MessageLevel.Action);

                        await Task.Factory.StartNew(() =>
                        {
                            ValidationReprocessStatus status = ((IFileReader)Loader).validateReprocess(prevBackupFilePath, _rowsProcessedTable);
                            switch (status)
                            {
                                case ValidationReprocessStatus.UNCHANGED:
                                    backupFilePath = prevBackupFilePath;

                                    _backedup = true;
                                    _logger.Log("Process : File Unchanged", MessageLevel.Action);
                                    break;
                                case ValidationReprocessStatus.ERRORSALTERED:
                                    _logger.Log("Process : File Errors Modified", MessageLevel.Action);

                                    _backedup = true;
                                    File.Copy(filepath, backupFilePath, true);
                                    _logger.Log(string.Format("Process : File Backed Up {0}", backupFilePath), MessageLevel.Action);
                                    break;
                                case ValidationReprocessStatus.INCOMPATIBLE:
                                    throw new Exception("Processed File MisMatch");
                            }
                        });
                    }
                    else
                    {
                        _backedup = true;

                        _logger.Log(string.Format("Process : File Backing Up {0}", backupFilePath), MessageLevel.Action);
                        await Task.Factory.StartNew(() =>
                        {
                            File.Copy(filepath, backupFilePath, true);
                        });
                        _logger.Log(string.Format("Process : File Backed Up {0}", backupFilePath), MessageLevel.Action);
                    }

                    if (_fileState != 1 && _fileState != 4)
                    {
                        throw new Exception(string.Format("File is not Set to 1 In-Process or 4 Re-Process ({0})", _fileState));
                    }

                    if (_startRow > 0)
                    {
                        string msg = "";
                        switch (_fileState)
                        {
                            case 1:
                                msg = "Resuming Processing from Crash";
                                break;
                            case 4:
                                msg = "Reprocessing File";
                                break;
                        }
                        _logger.Log(string.Format("Process : {1} ({0:#,##0} Rows Processed)", _startRow, msg), MessageLevel.Critical);
                    }


                    var reg = new Regex(_filePattern);

                    var filePatMatch = reg.Match(_filename);


                    if (filePatMatch.Success)
                    {
                        string[] names = reg.GetGroupNames();

                        foreach (string name in names)
                        {
                            if (name.ToUpper() == "FILEDATE")
                            {
                                try
                                {
                                    if (filePatMatch.Success)
                                    {
                                        fileDate = DateTime.ParseExact(filePatMatch.Groups.Item(name).Value, _fileDateFormat, null);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception(string.Format("Error Processing FileDate : {0}", ex.Message));
                                }
                            }
                            else
                            {
                                DataRow newRow = globalLookupTable.NewRow();
                                newRow["Lookup"] = name.ToUpper();
                                newRow["Result"] = filePatMatch.Groups.Item[name].Value;
                                globalLookupTable.Rows.Add(newRow);
                            }
                        }
                    }

                    try
                    {
                        if (Regex.IsMatch(_filePattern, "\\?\\<filedate\\>", RegexOptions.IgnoreCase))
                        {
                            dynamic fileDateMatch = Regex.Match(_filename, _filePattern);

                            if (fileDateMatch.Success)
                            {
                                fileDate = DateTime.ParseExact(fileDateMatch.Groups("filedate").Value, _fileDateFormat, null);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format("Error Processing FileDate : {0}", ex.Message));
                    }
                }

                _sw.Start();

                var rowlogger = new RowLogger();

                rowlogger.Begin(_logging, 1, _fileLogId, _connstr);

                ETLtasks[0] = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        if (_isfile)
                        {
                            ((IFileReader)Loader).load(_inputQueue, ref _rowErrorCount, token, _logger, _startRow, _fileState, _rowsProcessedTable, _logging, rowlogger.LogRow, ref _rowSkippedCount);
                        }
                        else
                        {
                            Loader.load(_inputQueue, ref _rowErrorCount, token, _logger, rowlogger.LogRow);
                        }

                    }
                    catch (Exception ex)
                    {
                        _inputQueue.CompleteAdding();

                        throw new Exception(string.Format("Loader : {0}", ex.Message));
                    }
                }, TaskCreationOptions.LongRunning);


                for (var i = 1; i <= PipeCount; i++)
                {
                    var pipeno = i;
                    ETLtasks[i] = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            var tpipe = new TransformationPipe(_config, GlobalDS, _logger, pipeno, fileDate, TransformationOutput);
                            try
                            {
                                tpipe.Load(_inputQueue, ref _rowErrorCount, ref _activePipeCount, ref _rowprocessedCount, token, rowlogger.LogRow);
                            }
                            finally
                            {
                                tpipe.Dispose();
                                tpipe = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            _tokenSource.Cancel();

                            throw new Exception(string.Format("Pipe {0} : {1}", pipeno, ex.Message));
                        }
                    }, TaskCreationOptions.LongRunning);

                }

                Task allETL = Task.WhenAll(ETLtasks);

                if (Started != null)
                {
                    Started();
                }

                bool success = false;
                string errorMsg = string.Empty;

                try
                {
                    await allETL;

                    success = true;
                    rowlogger.Complete();

                    rowlogger = null;
                }
                catch (AggregateException ex)
                {
                    foreach (var v in ex.Flatten().InnerExceptions)
                    {
                        if (!(v is TaskCanceledException))
                        {
                            errorMsg = "Process : " + v.Message;
                            if (v.InnerException != null)
                            {
                                errorMsg = errorMsg + ":" + v.InnerException.Message;
                            }

                            _logger.Log(errorMsg, MessageLevel.Critical);
                        }
                    }
                }

                RunStoredProcs("postprocess", false);


                _logger.Log(string.Format("Process : Loaded {0:0.0} Seconds", _sw.Elapsed.TotalSeconds), MessageLevel.Info);
                FinishProcess(success, errorMsg);

            }
            catch (Exception ex)
            {
                _logger.Log(ex.Message, MessageLevel.Critical);

                FinishProcess(false, ex.Message);
            }

            SqlConnection.ClearAllPools();
        }

        public void Cancel()
        {
            if (_tokenSource != null)
            {
                _tokenSource.Cancel();
            }
        }
        #endregion
    }
}
