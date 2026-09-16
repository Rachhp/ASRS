using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace S1947.Models
{
    public class WcsTaskRequest
    {
        public string SerialCode { get; set; }
        public string TrayCode { get; set; }
        public string method { get; set; }

        // WCS outbound position
        public int Layer { get; set; }
        //public int From_Layer { get; set; }
        public int From_Column { get; set; }
        public int From_Row { get; set; }
        public int From_OrderRow { get; set; }

        // Priority / sequence
        public int SortCode { get; set; }
        public long SerialNum { get; set; }

        // Optional
        public int StockSize { get; set; }
        public int TraySize { get; set; }

        // Warehouse / destination
        public string from_warehouse { get; set; }
        public string to_channel { get; set; }

        //public WcsExtraInfo extraInfo { get; set; }
    }

    //public class WcsExtraInfo
    //{
    //    public string station_no { get; set; }
    //}


    public class WcsApiResponse
    {
        public int code { get; set; }
        public bool status { get; set; }
        public string message { get; set; }
        public bool data { get; set; }
    }


}