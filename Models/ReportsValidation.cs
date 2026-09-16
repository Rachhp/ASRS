using System;

namespace S1947.Models
{
    public class ReportsValidation
    {

        public string ItemCode { get; set; }
        public string MachineCode { get; set; }

        public string ItemStatus { get; set; }

        public string ReportName { get; set; }
        public string ReportUrl { get; set; }
        public string UserType { get; set; }
        public string UserName { get; set; }
        public string ReportTital { get; set; }
        public Nullable<System.DateTime> FromDate { get; set; }
        public Nullable<System.DateTime> ToDate { get; set; }
        public Nullable<System.DateTime> DocDate { get; set; }
        public string Operation { get; set; }
        public string PartDesc { get; set; }

        public Nullable<int> Drawer { get; set; }


    }
}