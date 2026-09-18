using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace S1947.Models.PokaYoka
{
    public class PokaYokaViewModel
    {
        // DB transaction link
        public long TransactionId { get; set; }

        // QR
        public string QRCode { get; set; }

        public string Model { get; set; }
        public string FGCode { get; set; }
        public string UniqueId { get; set; }
        public string PalletNumber { get; set; }

        // QR received values
        public decimal ReceivedWeight { get; set; }
        public int  ReceivedQuantity { get; set; }

        // Machine values
        public decimal ActualWeight { get; set; }
        public int ActualQuantity { get; set; }

        // Comparison
        public decimal WeightDifference { get; set; }
        public int QuantityDifference { get; set; }

        // Validation checkss
        public int Check1_QtyPass { get; set; }
        public bool Check2_QrWeightPass { get; set; }
        public bool Check3_ExpectedWeightPass { get; set; }
        public string QCStatus { get; set; }

        public bool QRScanned { get; set; }
        public bool WeightFetched { get; set; }

        public string MachineStatus { get; set; }

        public string Message { get; set; }
    }
}