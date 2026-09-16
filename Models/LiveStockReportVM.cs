using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace S1947.Models
{
    public class LiveStockReportVM
    {
        public string MaterialCode { get; set; }
        public string FGModel { get; set; }

        public int Quantity { get; set; }

        public int Level { get; set; }
        public int Cell { get; set; }
        public int Depth { get; set; }

        public string LocationName { get; set; }

        public DateTime? LoadedDate { get; set; }

        public string Status { get; set; }
    }
}