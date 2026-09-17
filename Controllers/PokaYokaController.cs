using S1947.Models.PokaYoka;
using S1947.Services;
using S1947.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace S1947.Controllers
{
    public class PokaYokaController : Controller
    {
        // GET:  PokaYoka
        private readonly IPokaYokaService _pokaYokaService;

        public PokaYokaController()
        {
            _pokaYokaService =new PokaYokaService(new QrCodeParser(),
                new TcpWeighingMachineService("192.168.1.100",5000));
        }

        [HttpGet]
        public ActionResult Index()
        {
            var model = new PokaYokaViewModel();

            return View(model);
        }

        [HttpPost]
        public JsonResult ScanQr(string qrCode)
        {
            try
            {
                var result = _pokaYokaService.ScanQr(qrCode);

                return Json(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        [HttpPost]
        public JsonResult FetchWeight(long transactionId)
        {
            try
            {
                var result = _pokaYokaService.FetchWeight(transactionId);

                return Json(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        //[HttpPost]
        //public JsonResult FetchWeight(FormCollection form)
        //{
        //    try
        //    {
        //        var model = new PokaYokaViewModel
        //        {
        //            QRCode = form["QRCode"],
        //            Model = form["Model"],
        //            FGCode = form["FGCode"],
        //            PalletNumber = form["PalletNumber"],
        //            QCStatus = form["QCStatus"],
        //            MachineStatus = form["MachineStatus"],
        //            Message = form["Message"]
        //        };

        //        // Safe parse decimals with invariant culture
        //        decimal receivedWeight;
        //        decimal.TryParse(form["ReceivedWeight"],
        //            System.Globalization.NumberStyles.Any,
        //            System.Globalization.CultureInfo.InvariantCulture,
        //            out receivedWeight);
        //        model.ReceivedWeight = receivedWeight;

        //        decimal receivedQuantity;
        //        decimal.TryParse(form["ReceivedQuantity"],
        //            System.Globalization.NumberStyles.Any,
        //            System.Globalization.CultureInfo.InvariantCulture,
        //            out receivedQuantity);
        //        model.ReceivedQuantity = receivedQuantity;

        //        decimal actualWeight;
        //        decimal.TryParse(form["ActualWeight"],
        //            System.Globalization.NumberStyles.Any,
        //            System.Globalization.CultureInfo.InvariantCulture,
        //            out actualWeight);
        //        model.ActualWeight = actualWeight;

        //        decimal actualQuantity;
        //        decimal.TryParse(form["ActualQuantity"],
        //            System.Globalization.NumberStyles.Any,
        //            System.Globalization.CultureInfo.InvariantCulture,
        //            out actualQuantity);
        //        model.ActualQuantity = actualQuantity;

        //        // Safe parse booleans
        //        bool qrScanned;
        //        bool.TryParse(form["QRScanned"], out qrScanned);
        //        model.QRScanned = qrScanned;

        //        bool weightFetched;
        //        bool.TryParse(form["WeightFetched"], out weightFetched);
        //        model.WeightFetched = weightFetched;

        //        if (!ModelState.IsValid)
        //        {
        //            var errors = string.Join("; ",
        //                ModelState.Values
        //                    .SelectMany(v => v.Errors)
        //                    .Select(e => e.ErrorMessage));
        //            return Json(new { success = false, message = "Binding failed: " + errors });
        //        }

        //        var result = _pokaYokaService.FetchWeight(model);

        //        return Json(new
        //        {
        //            success = true,
        //            data = result
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new
        //        {
        //            success = false,
        //            message = ex.Message
        //        });
        //    }
        //}
    
    }

}