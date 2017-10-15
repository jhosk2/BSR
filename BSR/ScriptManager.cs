using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace BSR
{
    public class ScriptManager
    {
        private readonly string _logPath;
        private readonly string _scriptPath;

        public Dictionary<string, string> ScriptList { get; private set; }
        public ScriptManager(string scriptPath, string logPath)
        {
            _logPath = logPath;
            _scriptPath = scriptPath;
            if (!Directory.Exists(_scriptPath)) Directory.CreateDirectory(_scriptPath);
            if (!Directory.Exists(_logPath)) Directory.CreateDirectory(_logPath);

            ReloadScripts();
        }

        public void ReloadScripts()
        {
            ScriptList = Directory.GetFiles(_scriptPath, "*.ps1", SearchOption.AllDirectories)
                .ToDictionary(e=>Path.GetFileNameWithoutExtension(e), e=>e);
        }

        public async Task RunScript(string name, Dictionary<string,object> parameters)
        {
            var executerFileName = ScriptList[name];
            var executer = new PSExecutor(executerFileName);

            var id = $"{name}.{Guid.NewGuid()}.{DateTime.Now.Ticks}";
            
            using (var logWriter = File.AppendText($"{_logPath}/{id}.log"))
            {
                executer.OnStdErrorData += async (sender, args) => await OnErrorEventHandler(logWriter, id, args);
                executer.OnStdOutData += async (sender, args) => await OnUpdateEventHandler(logWriter, id, args);

                await OnScriptStartedEventHandler(logWriter, id);

                var ret = await executer.Run(parameters);

                await OnScriptFinishedEventHandler(logWriter, id, ret);
            }
        }

        private async Task OnScriptStartedEventHandler(StreamWriter writer, string id)
        {
            await writer.WriteLineAsync($"[{DateTime.Now}] Script Started, Id: {id}");
            OnStart?.Invoke(this, new ScriptStartedEventArgs()
            {
                Id = id
            });
        }

        private async Task OnErrorEventHandler(StreamWriter writer, string id, ErrorRecord args)
        {
            await OnUpdateEventHandler(writer, id, args.Exception.Message);

            using (var errorWriter = File.AppendText($"{_logPath}/{id}.error.log"))
            {
                errorWriter.WriteLine(args);
            }

            OnUpdate?.Invoke(this, new ScriptUpdatedEventArgs()
            {
                Id = id,
                Exception = args.Exception.InnerException ?? args.Exception
            });
        }

        private async Task OnUpdateEventHandler(StreamWriter writer, string id, string args)
        {
            await writer.WriteLineAsync($"[{DateTime.Now}] {args}");
            OnUpdate?.Invoke(this, new ScriptUpdatedEventArgs()
            {
                Id = id,
                Updated = args
            });
        }

        private async Task OnScriptFinishedEventHandler(StreamWriter writer, string id, string[] result)
        {
            await writer.WriteLineAsync($"[{DateTime.Now}] Script Ended, Id: {id}");

            using (var resultWriter = File.AppendText($"{_logPath}/{id}.result.log"))
            {
                foreach (var line in result)
                {
                    resultWriter.WriteLine(line);
                }
            }

            OnFinish?.Invoke(this, new ScriptFinishedEventArgs()
            {
                Id = id,
                Result = result
            });
        }

        public string[] ListResults()
        {
            return Directory.GetFiles(_logPath, "*.result.log")
                .Select(Path.GetFileNameWithoutExtension)
                .Select(e=>e.Replace(".result", ""))
                .ToArray();
        }

        public string GetResult(string id)
        {
            return File.ReadAllText($"{ _logPath}/{id}.result.log");
        }

        public List<Dictionary<string, Type>> GetParameters(string name)
        {
            var executerFileName = ScriptList[name];
            var executer = new PSExecutor(executerFileName);

            return executer.Parameters;
        }

        public event EventHandler<ScriptStartedEventArgs> OnStart;
        
        public event EventHandler<ScriptUpdatedEventArgs> OnUpdate;
        
        public event EventHandler<ScriptFinishedEventArgs> OnFinish;
    }

    public class ScriptStartedEventArgs : EventArgs
    {
        public string Id { get; set; }
    }

    public class ScriptUpdatedEventArgs : EventArgs
    {
        public string Id { get; set; }
        public string Updated { get; set; }
        public Exception Exception { get; set; }
    }

    public class ScriptFinishedEventArgs : EventArgs
    {
        public string Id { get; set; }
        public string[] Result { get; set; }
    }
}
