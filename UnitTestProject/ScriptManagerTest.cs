using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;
using BSR;
using System.Management.Automation;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace UnitTestProject
{
    [TestClass]
    public class ScriptManagerTest
    {

        private static void InitScriptManager(string sName, out ScriptManager manager)
        {
            if (Directory.Exists("./logs")) Directory.Delete("./logs", true);
            if (Directory.Exists("./script")) Directory.Delete("./script", true);
            Directory.CreateDirectory("./logs");
            Directory.CreateDirectory("./script");

            var script = @"Param(
    [string]$A,
    #[string[]]$A2,
    [int]$B,
    #[switch]$C,
    $D    
)

Write-Output $A
#Write-Output $A2
Write-Output $B
#Write-Output $C
Write-Output $D
";
            var scriptName = $"{Environment.CurrentDirectory}\\script\\{sName}.ps1";
            File.WriteAllText(scriptName, script);

            manager = new ScriptManager("./script", "./logs");
        }

        [TestMethod]
        public void WriteLogFileAndResult()
        {
            ScriptManager manager;
            string id = null;
            string[] result = null;
            InitScriptManager(System.Reflection.MethodBase.GetCurrentMethod().Name, out manager);

            manager.OnStart += (sender, args) => id = args.Id;
            manager.OnFinish += (sender, args) => result = args.Result;

            var param = new Dictionary<string, object>()
            {
                { "A", "test" },
                { "B", 1 },
                { "D", 2.1 },
            };
            manager.RunScript(System.Reflection.MethodBase.GetCurrentMethod().Name, param)
                .GetAwaiter()
                .GetResult();

            var expectedResult = @"test
1
2.1
";
            while (id == null || result == null)
            {
                Task.Delay(100)
                    .GetAwaiter()
                    .GetResult();
            }

            Assert.IsTrue(File.Exists($"./logs/{id}.log"));
            Assert.IsTrue(File.Exists($"./logs/{id}.result.log"));
            Assert.AreEqual(expectedResult, File.ReadAllText($"./logs/{id}.result.log"));
            Assert.AreEqual(expectedResult, string.Join("\r\n", result) + "\r\n");
        }

        [TestMethod]
        public void ListResults()
        {
            ScriptManager manager;
            InitScriptManager(System.Reflection.MethodBase.GetCurrentMethod().Name, out manager);

            var param = new Dictionary<string, object>()
            {
                { "A", "test" },
                { "B", 1 },
                { "D", 2.1 },
            };

            var count = 5;
            foreach (var index in Enumerable.Range(1, count))
            {
                manager.RunScript(System.Reflection.MethodBase.GetCurrentMethod().Name, param)
                    .GetAwaiter()
                    .GetResult();
            }

            var ids = manager.ListResults();
            Debug.WriteLine(ids);
            Assert.IsTrue(ids.Length == count);
        }

        [TestMethod]
        public void ListResult()
        {
            ScriptManager manager;
            InitScriptManager(System.Reflection.MethodBase.GetCurrentMethod().Name, out manager);

            var param = new Dictionary<string, object>()
            {
                { "A", "test" },
                { "B", 1 },
                { "D", 2.1 },
            };

            var count = 5;
            foreach (var index in Enumerable.Range(1, count))
            {
                manager.RunScript(System.Reflection.MethodBase.GetCurrentMethod().Name, param)
                    .GetAwaiter()
                    .GetResult();
            }

            var ids = manager.ListResults();

            var result = manager.GetResult(ids[0]);

            Assert.AreEqual(@"test
1
2.1
", result);

        }

        [TestMethod]
        public void GetScriptParameters()
            {
            ScriptManager manager;
            InitScriptManager(System.Reflection.MethodBase.GetCurrentMethod().Name, out manager);

            var ret = manager.GetParameters(System.Reflection.MethodBase.GetCurrentMethod().Name);

            Assert.AreEqual(1, ret.Count);
            Assert.AreEqual(3, ret[0].Count);
            Assert.AreEqual("A", ret[0].Keys.First());
            Assert.AreEqual(typeof(string), ret[0].Values.First());
            Assert.AreEqual("B", ret[0].Keys.Skip(1).First());
            Assert.AreEqual(typeof(int), ret[0].Values.Skip(1).First());
            Assert.AreEqual("D", ret[0].Keys.Skip(2).First());
            Assert.AreEqual(typeof(string), ret[0].Values.Skip(2).First());
        }
    }
}
