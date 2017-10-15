using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace BSR
{
    public class PSExecutor
    {
        public string Name => Path.GetFileNameWithoutExtension(_path);  
        public List<Dictionary<string, Type>> Parameters
        {
            get
            {
                if (_parameters == null)
                {
                    _parameters = LoadParameters();
                }
                return _parameters;
            }
        }

        public bool IsScriptCorrect { get; private set; }
        public IEnumerable<string> ParseErrors { get; private set; }

        private string _path;
        private string _scriptText;

        public PSExecutor(string path)
        {
            _path = path;
            _scriptText = File.ReadAllText(path);
            IsScriptCorrect = TestScriptCorrect(out var errors);
            ParseErrors = errors;
        }

        private readonly Dictionary<string, object> EmptyParameters = new Dictionary<string, object>();
        private List<Dictionary<string, Type>> _parameters;

        public event EventHandler<string> OnStdOutData;
        public event EventHandler<ErrorRecord> OnStdErrorData;

        private async Task<IEnumerable<PSObject>> RunInternal(string scriptText, Dictionary<string, object> parameters = null)
        {
            return await Task.Run(() =>
            {
                using (var instance = PowerShell.Create())
                {
                    Console.WriteLine($"Script running: {_path}, params : {parameters}");

                    instance.AddScript(scriptText);

                    if (parameters != null) instance.AddParameters(parameters);

                    var outputCollection = new PSDataCollection<PSObject>();
                    outputCollection.DataAdded += (sender, args) =>
                    {
                        var line = ((PSDataCollection<PSObject>)sender)[args.Index].BaseObject.ToString();
                        Debug.WriteLine(line);
                        OnStdOutData?.Invoke(this, line);
                    };

                    instance.Streams.Error.DataAdded += (sender, args) =>
                    {
                        var exception = ((PSDataCollection<ErrorRecord>)sender)[args.Index];
                        Debug.WriteLine(exception);
                        OnStdErrorData?.Invoke(this, exception);
                    };

                    IAsyncResult result = instance.BeginInvoke<PSObject, PSObject>(null, outputCollection);

                    try
                    {
                        instance.EndInvoke(result);
                    }
                    catch(Exception e)
                    {
                        OnStdErrorData?.Invoke(this, new ErrorRecord(e, null, ErrorCategory.FromStdErr, null));
                    }

                    Console.WriteLine("Execution has stopped. The pipeline state: " + instance.InvocationStateInfo.State);

                    return outputCollection;
                }
            });
        }

        private Type ConvertPowershellTypeToPrimitiveType(Type t)
        {
            if (t == typeof(SwitchParameter))
            {
                return typeof(bool);
            }

            if (t == typeof(Object))
            {
                return typeof(string);
            }

            return t;
        }

        private List<Dictionary<string, Type>> LoadParameters()
        {
            if (!IsScriptCorrect) return new List<Dictionary<string, Type>>();

            return RunInternal($"(Get-Command \"{_path}\").ParameterSets")
                .GetAwaiter()
                .GetResult()
                .Select(e => (CommandParameterSetInfo)e.BaseObject)
                .Select(e => e.Parameters.ToDictionary(x => x.Name, x => ConvertPowershellTypeToPrimitiveType(x.ParameterType)))
                .ToList();
        }

        public bool TestScriptCorrect(out IEnumerable<string> errors)
        {
            var psParser = PSParser.Tokenize(_scriptText, out var psErrors);

            errors = psErrors.Select(e => $"[{e.Token}] {e.Message}");
            return !psErrors.Any();
        }
        
        public async Task<string[]> Run(Dictionary<string, object> parameters = null)
        {
            if (!IsScriptCorrect) throw new InvalidOperationException();

            return (await RunInternal(_scriptText, parameters))
                .Select(e=>$"{e}")
                .ToArray();
        }

    }
}
