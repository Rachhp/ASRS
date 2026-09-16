using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace S1947.Models
{
    public class TransactionReportModel
    {

        public int MTransNo { get; set; }
        public string TransactionType { get; set; }

        public DateTime? TransactionDate { get; set; }
        public TimeSpan? TransactionTime { get; set; }

        public string Shift { get; set; }

        public string MaterialCode { get; set; }
        public string FGModel { get; set; }
        public string FGModelDesc { get; set; }

        public decimal? Quantity { get; set; }

        public int? LocationId { get; set; }
        public string LocationName { get; set; }

        public string PalletQR { get; set; }

        public string OperatorName { get; set; }
        public string ShiftSupervisor { get; set; }

        public string InvoiceNo { get; set; }
        public string CustomerName { get; set; }
        public string VehicleNumber { get; set; }
        public string TransporterName { get; set; }

        public string Status { get; set; }
        public string Action { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }

    }
}