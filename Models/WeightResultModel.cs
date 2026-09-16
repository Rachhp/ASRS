using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace S1947.Models.PokaYoka
{
    public class WeightResultModel
    {
        public bool Success { get; set; }

        public decimal Weight { get; set; }

        public string RawResponse { get; set; }

        public string ErrorMessage { get; set; }

        public DateTime ReadTime { get; set; }
       
    }
}