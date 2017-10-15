using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;
using BSR;
using System.Management.Automation;
using System.Collections.Generic;

namespace UnitTestProject
{
    [TestClass]
    public class PSExecutorTest
    {
        [TestMethod]
        public void PrintHelloWorld()
        {
            var script = "Write-Output \"Hello, World\"";
            var scriptName = $"{Environment.CurrentDirectory}\\{System.Reflection.MethodBase.GetCurrentMethod().Name}.ps1";
            File.WriteAllText(scriptName, script);
            var lib = new PSExecutor(scriptName);
            var ret = lib.Run(new System.Collections.Generic.Dictionary<string, object>())
                .GetAwaiter()
                .GetResult();

            foreach (var item in ret)
            {
                Assert.AreEqual("Hello, World", item.ToString());
            }
        }

        [TestMethod]
        public void PrintParameters()
        {
            var script = @"Param(
    [string]$A,
    [string[]]$A2,
    [int]$B,
    [switch]$C,
    $D    
)

Write-Host $A
";
            var scriptName = $"{Environment.CurrentDirectory}\\{System.Reflection.MethodBase.GetCurrentMethod().Name}.ps1";
            File.WriteAllText(scriptName, script);
            var lib = new PSExecutor(scriptName);
            var ret = lib.Parameters;

            foreach (var item in ret)
            {
                Assert.IsTrue(item.Keys.Count == 5);
                Assert.AreEqual(typeof(string), item["A"]);
                Assert.AreEqual(typeof(string[]), item["A2"]);
                Assert.AreEqual(typeof(int), item["B"]);
                Assert.AreEqual(typeof(bool), item["C"]);
                Assert.AreEqual(typeof(string), item["D"]);
            }
        }

        [TestMethod]
        public void ExecuteParameters()
        {
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
            var scriptName = $"{Environment.CurrentDirectory}\\{System.Reflection.MethodBase.GetCurrentMethod().Name}.ps1";
            File.WriteAllText(scriptName, script);
            var lib = new PSExecutor(scriptName);
            var ret = lib.Run(new Dictionary<string, object>()
            {
                { "A", "Hello, World" },
                //{ "A2", new []{ "1", "2", "3"}},
                { "B", 1 },
                //{ "C", true },
                { "D", "DDD" }
            })
                .GetAwaiter()
                .GetResult();

            Assert.AreEqual("Hello, World", ret[0]);
            Assert.AreEqual("1", ret[1]);
            Assert.AreEqual("DDD", ret[2]);
        }

        [TestMethod]
        public void ErrorDetect()
        {
            var script = @"Write-Output ""$(1/0)""
throw [System.IO.FileNotFoundException] ""$file not found.""
Start -Sleep -s 1
Write-Output 'exit'
";
            var scriptName = $"{Environment.CurrentDirectory}\\{System.Reflection.MethodBase.GetCurrentMethod().Name}.ps1";
            File.WriteAllText(scriptName, script);
            var lib = new PSExecutor(scriptName);
            int count = 0;
            var errors = new List<ErrorRecord>();
            lib.OnStdErrorData += (s, a) =>
            {
                count++;
                errors.Add(a);
            };

            var ret = lib.Run()
                .GetAwaiter()
                .GetResult();

            Assert.IsTrue(count ==  2);
            Assert.IsTrue(errors[0].Exception.InnerException is DivideByZeroException);
            Assert.IsTrue(errors[1].Exception.InnerException is FileNotFoundException);
        }

        [TestMethod]
        public void InvalidScriptRunThrowInvalidOperationException()
        {
            var script = @"Param(
    [string]$A
    [strias] $D
";
            var scriptName = $"{Environment.CurrentDirectory}\\{System.Reflection.MethodBase.GetCurrentMethod().Name}.ps1";
            File.WriteAllText(scriptName, script);
            var lib = new PSExecutor(scriptName);

            Assert.ThrowsException<InvalidOperationException>(()=> {
                lib.Run()
                    .GetAwaiter()
                    .GetResult();
            });
        }
    }
}
