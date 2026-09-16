using S1947.Models.PokaYoka;
using S1947.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace S1947.Services.Interfaces
{
        public interface IQrCodeParser
        {
            PokaYokaViewModel Parse(string qrCode);// Method to parse the QR code and return a PokaYokaViewModel
        }

}