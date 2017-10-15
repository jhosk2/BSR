using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BSR;
using System.IO;

namespace BSRFront.Controllers
{
    [Route("api/[controller]")]
    public class ScriptManagerController : Controller
    {
        private readonly ScriptManager manager = null;

        public ScriptManagerController()
        {
            var path = Path.GetTempPath() + "scripts";
            manager = new ScriptManager(path, $"{path}\\logs");
        }

        [HttpGet("[action]")]
        public IEnumerable<string> ScriptList()
        {
            return manager.ScriptList.Select(e => e.Key);
        }

        [HttpGet("[action]")]
        public IEnumerable<string> ScriptDetail(string scriptName)
        {
            return null;
        }
    }
}
