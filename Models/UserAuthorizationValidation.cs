using System;

namespace S1947.Models
{
    public class UserAuthorizationValidation
    {
        public int MTransNo { get; set; }
        public int UserNo { get; set; }
        public int ModuleNo { get; set; }
        public string AuthType { get; set; }
        public string Department { get; set; }
        public string UserType { get; set; }
        public string Dept { get; set; }
        public string ModuleType { get; set; }
        public Nullable<int> BranchNo { get; set; }
    }
}