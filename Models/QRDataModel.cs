using System;

namespace S1947.Models
{
    public class QRDataModel
    {
        public DateTime Date { get; set; }
        public string Time { get; set; }
        public string Shift { get; set; }
        public int PackingLineNo { get; set; }
        public string UniqueId { get; set; }
        public int PalletSrNo { get; set; }
        public int Quantity { get; set; }
        public string Model { get; set; }
        public string ModelDesc { get; set; }
        public string FGcode { get; set; }
        public decimal WeightTheo { get; set; }
        public string ScannedQr { get; set; }


    }
}