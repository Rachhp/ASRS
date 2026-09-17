using S1947.Models.PokaYoka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace S1947.Services.Interfaces
{
    public interface IPokaYokaService
    {
        PokaYokaViewModel ScanQr(string qrCode);

        //PokaYokaViewModel FetchWeight(PokaYokaViewModel model);
        PokaYokaViewModel FetchWeight(long transactionId);
    }
}