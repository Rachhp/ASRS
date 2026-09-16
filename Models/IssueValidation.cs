using System;

namespace S1947.Models
{
    public class IssueValidation
    {
        public int MTransNo { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialDescription { get; set; }
        public Nullable<int> MaterialID { get; set; }
        public string PackageType { get; set; }
        public string ReturnType { get; set; }
        public string MachineCode { get; set; }
        public string LineNos { get; set; }
        public string MachineDescription { get; set; }
        public Nullable<int> MachineID { get; set; }
        public string MaterialBarcode { get; set; }
        public Nullable<int> MaterialQty { get; set; }
        public Nullable<int> AvailableQty { get; set; }
        public Nullable<int> BinBarcode { get; set; }
        public Nullable<int> CarrierNo { get; set; }
        public Nullable<int> CompartmentNo { get; set; }
        public Nullable<int> DoorNo { get; set; }
        public string Status { get; set; }
        public string Remarks { get; set; }
        public Nullable<System.DateTime> CreatedOn { get; set; }
        public Nullable<int> CreatedBy { get; set; }

    }
}