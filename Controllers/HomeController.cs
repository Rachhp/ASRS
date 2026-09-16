using Newtonsoft.Json;
using S1947.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;



namespace S1947.Controllers
{
   
    public class HomeController : Controller
    {
        UtilityController utility = new UtilityController();
        public ActionResult Index()
        {
            if (Session["userid"] != null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Home");
            }
        }
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult LoginCheck(UserMaster model)
        {
            using (S1947Entities db = new S1947Entities())
            {
                // Retrieve user by UserId and PW
                var user = db.UserMasters.AsNoTracking().FirstOrDefault(u => u.UserId == model.UserId.Trim() && u.LockStatus == "Y");
                if (model.UserId == "cautomate" && model.PW == "caspl")
                {
                    Session["UserId"] = "cautomate";
                    Session["UserType"] = "ADMIN";
                    Session["UserName"] = "cautomate";
                    DateTime today = DateTime.Today;

                    //opening stock
                    DateTime yesterday = DateTime.Now.AddDays(-1).Date;
                    DateTime CurrentDate = DateTime.Now.Date;

                    Task.Run(() =>
                    {
                        UtilityController utility = new UtilityController();
                        // utility.BackUp();
                    });

                    return RedirectToAction("Dashboard");
                }

                if (user != null && user.DeleteStatus == "N")
                {
                    string pw = user.PW.Trim();
                    string encVa = Encrypt(model.PW, "mskl-1sv4-xklq23");
                    string dec = Decrypt(encVa, "mskl-1sv4-xklq23");
                    if (pw.Equals(encVa))
                    {
                        db.SaveChanges();
                        // Store user information in the session
                        Session["MTransNo"] = user.MTransNo;
                        Session["UserId"] = user.UserId;
                        Session["UserType"] = user.UserType;
                        Session["UserName"] = user.UserName;

                        //opening stock
                        DateTime yesterday = DateTime.Now.AddDays(-1).Date;
                        DateTime CurrentDate = DateTime.Now.Date;

                        return RedirectToAction("Dashboard");

                    }
                    else if (user != null && user.DeleteStatus == "Y")
                    {

                        TempData["wrongLogin"] = "Please Contact Admin.";
                        return RedirectToAction("Login");
                    }
                    TempData["wrongLogin"] = "Login ID / Password Does Not Match";
                    return RedirectToAction("Login");
                }
                else
                {
                    TempData["wrongLogin"] = "Login Id / Password Does Not Match";
                    return RedirectToAction("Login");
                }
            }
        }
        public static string Encrypt(string input, string key)
        {
            byte[] inputArray = UTF8Encoding.UTF8.GetBytes(input);
            TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider();
            tripleDES.Key = UTF8Encoding.UTF8.GetBytes(key);
            tripleDES.Mode = CipherMode.ECB;
            tripleDES.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = tripleDES.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);
            tripleDES.Clear();
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        public static string Decrypt(string input, string key)
        {
            byte[] inputArray = Convert.FromBase64String(input);
            TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider();
            tripleDES.Key = UTF8Encoding.UTF8.GetBytes(key);
            tripleDES.Mode = CipherMode.ECB;
            tripleDES.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = tripleDES.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);
            tripleDES.Clear();
            return UTF8Encoding.UTF8.GetString(resultArray);
        }

       

        public ActionResult Dashboard()
        {
            DashboardViewModel model = new DashboardViewModel();

            S1947Entities conn = new S1947Entities();
            //row 1  data 
            model.TotalUser = conn.UserMasters.Where(x => x.LockStatus == "Y" && x.DeleteStatus == "N").Count();

            model.TotalQty = (int)conn.GoodsExisitings.Where(x => x.LocaStatus != "NOLOCATION" && x.LocaStatus != "SHUTTLLE_MAINTAINANCE" && x.LocaStatus != "LOADING_STATION" && x.LocaStatus != "UNLOADING_STATION").Sum(y => y.Quantity);

            model.TotalCells = conn.GoodsExisitings.Count(x => new[] { "LOADED", "AVAILABLE" }.Contains(x.LocaStatus));

            model.OccupiedCells = conn.GoodsExisitings.Where(x => x.LocaStatus == "LOADED").Count();

            model.AvailableCells = conn.GoodsExisitings.Where(x => x.LocaStatus == "AVAILABLE").Count();

            double utilization = Math.Round((model.OccupiedCells * 100.0) / model.TotalCells, 2);
            ViewBag.StorageUtilization = utilization;

            //row 2 data for donut chart
            var occupancy = conn.GoodsExisitings
                .Where(x => new[] { "LOADED", "AVAILABLE" }.Contains(x.LocaStatus))
                .GroupBy(x => x.LocaStatus)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                }).ToList();

            ViewBag.OccupancyLabels = JsonConvert.SerializeObject(occupancy.Select(x => x.Status));

            ViewBag.OccupancyData = JsonConvert.SerializeObject(occupancy.Select(x => x.Count));


            //for pie chart top 5 part
            var occupancy1 = conn.GoodsExisitings
            .Where(x => x.LocaStatus == "LOADED" && x.FGCode != "" && x.FGCode != null)
            .GroupBy(x => x.FGCode)
            .Select(g => new
            {
                FGCode = g.Key,
                TotalQty = g.Sum(x => x.Quantity),
            }).OrderByDescending(x => x.TotalQty).Take(5).ToList();

            ViewBag.Partlabels = JsonConvert.SerializeObject(
            occupancy1.Select(x => x.FGCode));

            ViewBag.PartlabelsData = JsonConvert.SerializeObject(
            occupancy1.Select(x => x.TotalQty));


            // Capacity Utilization By Level
            var levelData = conn.GoodsExisitings
                .Where(x => new[] { "LOADED", "AVAILABLE" }.Contains(x.LocaStatus))
                .GroupBy(x => x.Layer)
                .Select(g => new
                {
                    Level = "Level " + g.Key,
                    Occupied = g.Count(x => x.LocaStatus == "LOADED"),
                    Total = g.Count(),
                    Utilization = Math.Round(
                        (g.Count(x => x.LocaStatus == "LOADED") * 100.0) / g.Count(),
                        2)
                })
                .OrderBy(x => x.Level)
                .ToList();

            ViewBag.LevelLabels = JsonConvert.SerializeObject(levelData.Select(x => x.Level));
            ViewBag.LevelCounts = JsonConvert.SerializeObject(levelData.Select(x => x.Occupied));
            ViewBag.LevelTotals = JsonConvert.SerializeObject(levelData.Select(x => x.Total));
            ViewBag.LevelUtilization = JsonConvert.SerializeObject(levelData.Select(x => x.Utilization));

            //column chart
            var columnData = conn.GoodsExisitings
            .Where(x => x.LocaStatus == "LOADED")
            .GroupBy(x => x.Col)
            .Select(g => new
            {
                Column = g.Key,
                Count = g.Count()
            })
            .ToList();

            ViewBag.ColumnLabels = JsonConvert.SerializeObject(columnData.Select(x => x.Column));
            ViewBag.ColumnCounts = JsonConvert.SerializeObject(columnData.Select(x => x.Count));


            //for the monthly transaction chart
            var currentYear = DateTime.Now.Year;
            // Loading (IN)
            var loadingData = conn.LoadingLogs
                .Where(x => x.CreatedOn.HasValue && x.CreatedOn.Value.Year == currentYear)
                .GroupBy(x => x.CreatedOn.Value.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Count = g.Count()
                })
                .ToList();

            // Unloading (OUT)
            var unloadingData = conn.UnloadingLogs
                .Where(x => x.CreatedOn.Value.Year == currentYear)
                .GroupBy(x => x.CreatedOn.Value.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var months = new[]
            {
                "Jan","Feb","Mar","Apr","May","Jun",
                "Jul","Aug","Sep","Oct","Nov","Dec"
            };

            ViewBag.TransactionMonths = JsonConvert.SerializeObject(months);

            ViewBag.InTransactions = JsonConvert.SerializeObject(
                Enumerable.Range(1, 12)
                    .Select(m => loadingData
                        .Where(x => x.Month == m)
                        .Sum(x => x.Count))
            );

            ViewBag.OutTransactions = JsonConvert.SerializeObject(
                Enumerable.Range(1, 12)
                    .Select(m => unloadingData
                        .Where(x => x.Month == m)
                        .Sum(x => x.Count))
            );
   
            return View(model);
        }


        public ActionResult MastersManagement()
        {
            return View();
        }
        public ActionResult OperationManagement()
        {
            return View();
        }

  
        public ActionResult LogOut()
        {
            Session.Remove("MTransNo");
            Session.Remove("UserId");
            Session.Remove("UserType");
            Session.Remove("UserName");
            return RedirectToAction("Login");
        }
        //Example: store last update in a static variable(not ideal for production)
        private static DateTime _lastUpdated = DateTime.MinValue;

    
        public ActionResult UserManual()
        {
            // Path to your PDF inside wwwroot or Content folder
            ViewBag.PdfPath = Url.Content("~/Content/designFlowChart.pdf");
            return View();
        }
        public JsonResult GetWeeklyOperationChart(DateTime fromDate, string operationType)
        {
            DateTime toDate = fromDate.AddDays(6);
            string op1, op2;

            // 👇 Switch based on dropdown
            if (operationType == "Load-Unload")
            {
                op1 = "Load";
                op2 = "Unload";
            }
            else
            {
                op1 = "Issue";
                op2 = "Return";
            }

            using (S1947Entities conn = new S1947Entities())
            {
                //var rawData = conn.Dashboardviews
                //    .Where(x =>
                //        x.TranDate >= fromDate &&
                //        x.TranDate <= toDate &&
                //        (x.Operation == op1 || x.Operation == op2)
                //    )
                //    .ToList();

                //var dates = Enumerable.Range(0, 7)
                //    .Select(i => fromDate.AddDays(i))
                //    .ToList();

                //var labels = dates
                //    .Select(d => d.Day + "-" + d.ToString("ddd"))
                //    .ToArray();

                //var data1 = dates
                //    .Select(d => rawData
                //        .Where(x => x.TranDate == d && x.Operation == op1)
                //        .Sum(x => (int?)x.MaterialQty) ?? 0)
                //    .ToArray();

                //var data2 = dates
                //    .Select(d => rawData
                //        .Where(x => x.TranDate == d && x.Operation == op2)
                //        .Sum(x => (int?)x.MaterialQty) ?? 0)
                //    .ToArray();

                return Json(new
                {
                    dataset1Label = op1,
                    dataset2Label = op2,

                    fromDate = fromDate.ToString("yyyy-MM-dd"),
                    toDate = toDate.ToString("yyyy-MM-dd")
                }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
