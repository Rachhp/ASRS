using S1947.Models;
using System.Linq;
using System.Web.Mvc;

namespace S1947.Controllers
{
    public class LoadController : Controller
    {
        S1947Entities db = new S1947Entities();

        public ActionResult AddEdit(int MTransNo = 0)
        {
            var dataList = db.LoadingTemps
                             .OrderBy(x => x.CreatedOn)
                             .ToList();

            ViewBag.AndonData = dataList;
            return View();
        }


        [HttpGet]
        public JsonResult GetLoadingData()
        {
            var data = db.LoadingTemps
                .OrderBy(x => x.CreatedOn)
                .Select(x => new
                {
                    x.UniqueId,
                    x.FGModel,
                    x.FGModelDesc,
                    x.FGcode,
                    x.Shift,
                    x.PalletSrNo,
                    x.PackingLineNo,
                    x.Quantity,
                    x.WeightTheo,
                    x.Date,
                    x.Time,
                    x.LocationName,

                    // Task ID from LoadingTemp
                    SerialCode = x.SerialCode,

                    // Get latest status from TaskReport
                    Status = db.TaskReports
                        .Where(t => t.SerialCode == x.SerialCode)
                        .OrderByDescending(t => t.ID)
                        .Select(t => t.StautusMessage)
                        .FirstOrDefault()

                })
                .ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
        }
    }
}
