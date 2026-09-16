using S1947.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
namespace S1947.Controllers
{
    public class ASRSController : Controller
    {
        private readonly S1947Entities db = new S1947Entities();
        // GET: ASRS
        public ActionResult Index()
        {
            ASRSViewModel model = new ASRSViewModel();
            model.Levels = Enumerable.Range(1, 8)
                                     .OrderByDescending(x => x)
                                     .ToList();
            model.Columns = Enumerable.Range(1, 42)
                                      .ToList();
            return View(model);
        }
        

        public JsonResult GetPositions(int level, int column)
         {
            var data = db.GoodsExisitings
                .Where(x => x.Layer == level
                         && x.Col == column
                         && x.DeleteStatus == "N")
                .Select(x => new ASRSPosition
                {
                    LocationNo = x.LocationName,
                    Level = x.Layer,
                    Column = x.Col,
                    Position = x.Row,
                    Status = x.LocaStatus,
                    PalletNo = x.PalletNo,
                    Qty = x.Quantity ?? 0
                })
                .OrderBy(x => x.Position)
                .ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
         }

    }
}
