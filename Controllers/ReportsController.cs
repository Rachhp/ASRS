using S1947.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace S1947.Controllers
{
    public class ReportsController : Controller
    {
        // GET: REPORTS
        public ActionResult Index()
        {
            return View();
        }

        //public ActionResult TransactionReport()
        //{
        //    S1947Entities conn = new S1947Entities();

        //    List<vw_TransactionReport> model = conn.vw_TransactionReport
        //                                           .OrderByDescending(x => x.CreatedOn)
        //                                           .ToList();

        //    return View(model);
        //}

        public ActionResult TransactionReportPdf(DateTime? FromDate,
                                                 DateTime? ToDate,
                                                 string TransactionType,
                                                 string MaterialCode)
        {
            S1947Entities db = new S1947Entities();

            var data = db.vw_TransactionReport.AsQueryable();

            if (FromDate.HasValue)
                data = data.Where(x => x.TransactionDate >= FromDate.Value);

            if (ToDate.HasValue)
                data = data.Where(x => x.TransactionDate <= ToDate.Value);

            if (!string.IsNullOrWhiteSpace(TransactionType))
                data = data.Where(x => x.TransactionType == TransactionType);

            if (!string.IsNullOrWhiteSpace(MaterialCode))
                data = data.Where(x => x.MaterialCode.Contains(MaterialCode));

            return new Rotativa.ViewAsPdf("TransactionReportPDF",
                data.OrderByDescending(x => x.CreatedOn).ToList())
            {
                FileName = "TransactionReport.pdf",

                PageOrientation = Rotativa.Options.Orientation.Landscape,

                PageSize = Rotativa.Options.Size.A4,

                PageMargins = new Rotativa.Options.Margins(10, 10, 10, 10)
            };
        }
        public ActionResult TransactionReport(DateTime? FromDate,DateTime? ToDate,string TransactionType,string MaterialCode)
        {
            S1947Entities db = new S1947Entities();

            var data = db.vw_TransactionReport.AsQueryable();      

            if (FromDate.HasValue)
            {
                data = data.Where(x => x.TransactionDate >= FromDate.Value);
            }
           
            if (ToDate.HasValue)
            {
                data = data.Where(x => x.TransactionDate <= ToDate.Value);
            }

            if (!string.IsNullOrEmpty(TransactionType))
            {
                data = data.Where(x => x.TransactionType == TransactionType);
            }

            if (!string.IsNullOrEmpty(MaterialCode))
            {
                data = data.Where(x => x.MaterialCode.Contains(MaterialCode));
            }

            return View(data.OrderByDescending(x => x.CreatedOn).ToList());
        }
            public ActionResult AgeingReport(string MaterialCode)
            {
                S1947Entities db = new S1947Entities();

                var data = (from lm in db.GoodsExisitings
                            join tl in db.LoadingLogs
                            on lm.LocationName equals tl.LocationName
                            where lm.LocaStatus == "LOADED"
                               && tl.Status == "LOADED"
                               && tl.Action == "LOADOPERATION"
                            select new AgeingReportVM
                            {
                                MaterialCode = lm.FGCode,
                                FGModel = lm.FGModel,
                                Quantity = (int)lm.Quantity,
                                LocationName = lm.LocationName,
                                Status = lm.LocaStatus,
                                LoadedDate = tl.CreatedOn
                            }).ToList();

                if (!string.IsNullOrEmpty(MaterialCode))
                {
                    data = data.Where(x => x.MaterialCode.Contains(MaterialCode)).ToList();
                }

                foreach (var item in data)
                {
                    item.AgeDays = item.LoadedDate.HasValue
                        ? (DateTime.Today - item.LoadedDate.Value.Date).Days
                        : 0;
                }

                return View(data.OrderByDescending(x => x.AgeDays).ToList());
            }


            public ActionResult AgeingReportPdf(string MaterialCode)
            {
                S1947Entities db = new S1947Entities();

                var data = (from lm in db.GoodsExisitings
                            join tl in db.LoadingLogs
                            on lm.LocationName equals tl.LocationName
                            where lm.LocaStatus == "LOADED"
                               && tl.Status == "LOADED"
                               && tl.Action == "LOADOPERATION"
                            select new AgeingReportVM
                            {
                                MaterialCode = lm.FGCode,
                                FGModel = lm.FGModel,
                                Quantity = (int)lm.Quantity,
                                LocationName = lm.LocationName,
                                Status = lm.LocaStatus,
                                LoadedDate = tl.CreatedOn
                            }).ToList();

                if (!string.IsNullOrEmpty(MaterialCode))
                {
                    data = data.Where(x => x.MaterialCode.Contains(MaterialCode)).ToList();
                }

                foreach (var item in data)
                {
                    item.AgeDays = item.LoadedDate.HasValue
                        ? (DateTime.Today - item.LoadedDate.Value.Date).Days
                        : 0;
                }

                return new Rotativa.ViewAsPdf("AgeingReportPDF",
                    data.OrderByDescending(x => x.AgeDays).ToList())
                {
                    FileName = "AgeingReport.pdf",
                    PageOrientation = Rotativa.Options.Orientation.Landscape,
                    PageSize = Rotativa.Options.Size.A4,
                    PageMargins = new Rotativa.Options.Margins(10, 10, 10, 10)
                };
            }

        public ActionResult LiveStockReport(string MaterialCode)
        {
            S1947Entities db = new S1947Entities();

            var data = db.GoodsExisitings   
            .Where(x => x.LocaStatus == "LOADED")
                .Select(x => new LiveStockReportVM
                {
                    MaterialCode = x.FGCode,
                    FGModel = x.FGModel,
                    Quantity = (int)x.Quantity,
                    Level = (int)x.Layer,
                    Cell = (int)x.Col,
                    Depth = (int)x.Row,
                    LocationName = x.LocationName,
                    LoadedDate = x.ModifiedOn,
                    //Status = x.LocaStatus
                }).OrderByDescending(x => x.Level)
            .ToList();

            // Material Code filter
            if (!string.IsNullOrEmpty(MaterialCode))
            {
                data = data
                    .Where(x => x.MaterialCode.Contains(MaterialCode))
                    .ToList();
            }


            // Sort by Level → Cell → Depth
            data = data
                .OrderBy(x => x.Level)
                .ThenBy(x => x.Cell)
                .ThenBy(x => x.Depth)
                .ToList();


            return View(data);
        }

        public ActionResult MaterialMasterReport()
        {
            return View();
        }

        public ActionResult GoodsExisitingReport()
        {
            return View();
        }

        public ActionResult UserMasterReport()
        {
            return View();
        }
    }
}