using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Xml.Linq;
using Transformation.Loader;
using TransformationCore.Models;

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


            var runner = new TransformationProcess();

            runner.Initialise(config, new System.Threading.CancellationTokenSource(), new ConsoleLogger(), null);

            var processInfo = new XElement("processinfo", new XAttribute("id", Guid.NewGuid().ToString()), new XElement("filename", @"c:\temp\stafftest.csv"));

            runner.Process(processInfo, new GlobalData()).Wait();

            Console.ReadKey();
        }
    }
}
