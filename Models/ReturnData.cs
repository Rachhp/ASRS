using System;

namespace S1947.Models
{



    public class ReturnData

    {
        public int MTransNo { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialDescription { get; set; }
        public string PackageType { get; set; }
        public string MaterialBarcode { get; set; }
        public string MachineCode { get; set; }
        public string LineNo { get; set; }
        public string MachineDescription { get; set; }
        public Nullable<int> ReturnQty { get; set; }
        public Nullable<int> IssuedQty { get; set; }
        public Nullable<int> BinBarcode { get; set; }
        public Nullable<int> CarrierNo { get; set; }
        public Nullable<int> CompartmentNo { get; set; }
        public Nullable<int> BinNo { get; set; }
        public string Status { get; set; }
        public string Action { get; set; }
        public string Remarks { get; set; }
        public Nullable<System.DateTime> CreatedOn { get; set; }
        public Nullable<int> CreatedBy { get; set; }
        public Nullable<decimal> Weight { get; set; }
        public Nullable<int> ProcessId { get; set; }
    }




}
