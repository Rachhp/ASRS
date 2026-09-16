using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace S1947.Models
{
    public class LoadingTempStatusViewModel
    {
        public string UniqueId { get; set; }

        public string FGModel { get; set; }

        public string FGModelDesc { get; set; }

        public string FGcode { get; set; }

        public string Shift { get; set; }

        public int? PalletSrNo { get; set; }

        public int? PackingLineNo { get; set; }

        public int? Quantity { get; set; }

        public decimal? WeightTheo { get; set; }

        public string Date { get; set; }

        public string Time { get; set; }

        public string LocationName { get; set; }

        public int? TaskID { get; set; }

        public string StatusCode { get; set; }

        public string StatusMessage { get; set; }
    }
}