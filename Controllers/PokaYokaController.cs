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

        public PokaYokaController() //it is a constructor for the PokaYokaController class.
        {
            //it requires 2 objects  1 . QrCodeParser 2. TcpWeighingMachineService
            _pokaYokaService = new PokaYokaService(new QrCodeParser(),new TcpWeighingMachineService("192.168.1.100",5000));
            //aslso this is  called manual dependency injection because we are creating the object of the service class in the controller class.
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
      
    
    }

}