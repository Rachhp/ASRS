using S1947.Models;
using S1947.Models.PokaYoka;
using S1947.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace S1947.Services
{
    public class QrCodeParser : IQrCodeParser
    {
        // This method parses the QR code and returns a PokaYokaViewModel object.
        public PokaYokaViewModel Parse(string qrCode)
        {
            if (string.IsNullOrWhiteSpace(qrCode))
                throw new Exception("QR code is empty.");

            //if (qrCode.Length != 78)
            //    throw new Exception("Invalid QR code. Expected 78 characters.");
            
            var data = qrCode.Split(';');
            if (data.Length < 11)
                throw new Exception("Invalid QR format");


            var model = new PokaYokaViewModel();
            model.QRCode = qrCode;

            // Pallet number (index 5)
            model.UniqueId= data[4].Trim();
            model.PalletNumber = data[5].Trim();

            // Quantity (index 6)
            int quantity;
            if (!int.TryParse(data[6].Trim(), out quantity))
                throw new Exception("Invalid quantity in QR.");

            model.ReceivedQuantity = quantity;
            model.Model = data[7].Trim();
            model.FGCode = data[9].Trim();

            decimal weight;
            if (!decimal.TryParse(data[10].Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out weight))
                throw new Exception("Invalid weight in QR.");

            model.ReceivedWeight = weight;

            model.QRScanned = true;
            return model;
        }
    }

}