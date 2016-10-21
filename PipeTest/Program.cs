using System;
using System.Xml.Linq;
using Transformation.Loader;

namespace PipeTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = XElement.Parse(@"<loadscript>
                                            <globalvar name=""connection"" value=""Server=WORK_PC\SQLEXPRESS;Database=TestLoad;Trusted_Connection=True;"" valuetype=""string"" />
                                            <reader name=""CSVReader"" delimeter=""|"">
                                                <fields>
                                                    <field name=""personnel number"" type=""int""/>
                                                    <field name=""surname"" type=""string""/>
                                                </fields>
                                            </reader>
                                            <pipe pipes=""1"">
                                                <transformation name=""sqlbulkloader"" tablename=""test"">
                                                    <columns>
                                                        <column name=""personnel number"" type=""int""/>
                                                        <column name=""surname"" type=""string""/>
                                                    </columns>
                                                </transformation>
                                            </pipe>
                                         </loadscript>
                                        ");


            var runner = new LoadProcess();

            runner.Initialise(config, new System.Threading.CancellationTokenSource(), new ConsoleLogger(), null);

            var processInfo = new XElement("processinfo");

            //@"c:\temp\stafftest.csv"

            runner.Process(processInfo);

            Console.ReadKey();
        }
    }
}
