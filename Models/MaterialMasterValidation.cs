using System;

namespace S1947.Models
{
    public class MaterialMasterValidation
    {
        public int MTransNo { get; set; }
        public string FGCode { get; set; }
        public string FGName { get; set; }
        public string FGDesc { get; set; }
        public string Type { get; set; }
        public string LockStatus { get; set; }
        public Nullable<short> CreatedBy { get; set; }
        public Nullable<System.DateTime> CreatedOn { get; set; }
        public Nullable<short> ModifiedBy { get; set; }
        public Nullable<System.DateTime> ModifiedOn { get; set; }
        public string DeleteStatus { get; set; }
    }
}
