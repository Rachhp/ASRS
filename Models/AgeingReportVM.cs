using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace S1947.Models
{
    public class AgeingReportVM
    {
        public string MaterialCode { get; set; }
        public string FGModel { get; set; }
        public string LocationName { get; set; }
        public int Quantity { get; set; }

        public DateTime? LoadedDate { get; set; }

        public int AgeDays { get; set; }

        public string Status { get; set; }
    }
}
