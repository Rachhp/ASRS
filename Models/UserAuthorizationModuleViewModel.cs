using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace S1947.Models
{
    public class UserAuthorizationModuleViewModel
    {
        public int ModuleNo { get; set; }

        public string ModuleType { get; set; }

        public string ModuleName { get; set; }

        public int MSLNo { get; set; }

        public int AuthMTransNo { get; set; }

        public string AuthType { get; set; }

        public bool IsAuthorized { get; set; }
    }
}