using S1947.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace S1947.Controllers
{
    public class ASRSController : Controller
    {
        private readonly S1947Entities db = new S1947Entities();

        // =========================================================
        // INDEX
        // =========================================================

        public ActionResult Index()
        {
            var model = new ASRSViewModel
            {
                Levels = Enumerable.Range(1, 8)
                                   .OrderBy(x => x)
                                   .ToList(),

                Columns = Enumerable.Range(1, 44)
                                   .OrderByDescending(x => x)
                                   .ToList()
            };

            return View(model);
        }
        public ActionResult ASRS_3D()
        {


            return View();
        }


        // =========================================================
        // GET ACTIVE ZONES
        // =========================================================


        [HttpGet]
        public JsonResult GetZones()
        {
            try
            {
                var zones = db.GELoadingViews
                    .Where(x =>
                        x.status == "Active" &&
                        x.DeleteStatus == "N" &&
                        x.ZoneCode != null &&
                        x.ZoneCode != "" &&

                // Exclude non-storage locations
                !new[]
                {
    "ROADWAY",
                    "BLOCKED",
                    "ISOLATION",
                    "CHARGE",
                    "ROADWAY","CACHEROADWAY","PARENTROADWAY","EMPTY"
                }.Contains(x.LocaStatus)
                )

                    .GroupBy(x => new
                    {
                        x.ZoneCode,
                        x.ZoneName,
                        x.MovementType,
                        x.ZonePriority
                    })
                    .Select(g => new
                    {
                        ZoneCode = g.Key.ZoneCode,
                        ZoneName = g.Key.ZoneName,
                        MovementType = g.Key.MovementType,
                        Priority = g.Key.ZonePriority,

                        // Actual physical area covered by this zone
                        ColumnFrom = g.Min(x => x.Col),
                        ColumnTo = g.Max(x => x.Col),

                        RowFrom = g.Min(x => x.Row),
                        RowTo = g.Max(x => x.Row),

                        LevelFrom = g.Min(x => x.Layer),
                        LevelTo = g.Max(x => x.Layer),

                        Status = "Active",

                        LocationCount = g.Count()
                    })
                    .OrderBy(x => x.Priority)
                    .ThenBy(x => x.ZoneCode)
                    .ToList();

                return Json(
                    new
                    {
                        success = true,
                        data = zones
                    },
                    JsonRequestBehavior.AllowGet
                );
            }
            catch (Exception ex)
            {
                return Json(
                    new
                    {
                        success = false,
                        message = ex.Message,
                        data = new List<object>()
                    },
                    JsonRequestBehavior.AllowGet
                );
            }
        }

        // =========================================================
        // GET SINGLE ZONE
        // =========================================================

        [HttpGet]
        public JsonResult GetZone(int id)
        {
            try
            {
                var zone = db.ASRSLocationZoneMasters
                    .Where(x => x.Id == id)
                    .Select(x => new
                    {
                        x.Id,
                        x.ZoneCode,
                        x.ZoneName,
                        x.MovementType,
                        x.Priority,
                        x.ColumnFrom,
                        x.ColumnTo,
                        x.RowFrom,
                        x.RowTo,
                        x.LevelFrom,
                        x.LevelTo,
                        x.Status,
                        x.CreatedDate,
                        x.ModifiedDate
                    })
                    .FirstOrDefault();

                if (zone == null)
                {
                    return Json(
                        new
                        {
                            success = false,
                            message = "Zone not found."
                        },
                        JsonRequestBehavior.AllowGet
                    );
                }

                return Json(
                    new
                    {
                        success = true,
                        data = zone
                    },
                    JsonRequestBehavior.AllowGet
                );
            }
            catch (Exception ex)
            {
                return Json(
                    new
                    {
                        success = false,
                        message = ex.Message
                    },
                    JsonRequestBehavior.AllowGet
                );
            }
        }


        // =========================================================
        // UPDATE ZONE MASTER
        // =========================================================

        [HttpPost]
        public JsonResult UpdateZone(ZoneMasterUpdateModel model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid request."
                    });
                }

                if (model.Id <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid Zone Id."
                    });
                }

                if (string.IsNullOrWhiteSpace(model.ZoneCode))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Zone Code is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(model.ZoneName))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Zone Name is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(model.MovementType))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Movement Type is required."
                    });
                }

                var movementType =
                    model.MovementType.Trim().ToUpper();

                if (movementType != "FAST" &&
                    movementType != "MEDIUM" &&
                    movementType != "SLOW")
                {
                    return Json(new
                    {
                        success = false,
                        message = "Movement Type must be FAST, MEDIUM or SLOW."
                    });
                }

                // -------------------------------------------------
                // Validate ranges
                // -------------------------------------------------

                if (model.ColumnFrom < 1 || model.ColumnFrom > 44 ||
                    model.ColumnTo < 1 || model.ColumnTo > 44)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Column range must be between 1 and 44."
                    });
                }

                if (model.RowFrom < 1 ||
                    model.RowTo < 1)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Row range must be greater than 0."
                    });
                }

                if (model.LevelFrom < 1 || model.LevelFrom > 8 ||
                    model.LevelTo < 1 || model.LevelTo > 8)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Level range must be between 1 and 8."
                    });
                }

                if (model.Priority < 1)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Priority must be greater than 0."
                    });
                }

                // -------------------------------------------------
                // Find master
                // -------------------------------------------------

                var zone = db.ASRSLocationZoneMasters
                    .FirstOrDefault(x => x.Id == model.Id);

                if (zone == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Zone not found."
                    });
                }

                // -------------------------------------------------
                // Check duplicate ZoneCode
                // -------------------------------------------------

                var duplicateCode =
                    db.ASRSLocationZoneMasters.Any(x =>
                        x.Id != model.Id &&
                        x.ZoneCode == model.ZoneCode.Trim());

                if (duplicateCode)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Zone Code already exists."
                    });
                }

                // -------------------------------------------------
                // Update master
                // -------------------------------------------------

                zone.ZoneCode =
                    model.ZoneCode.Trim();

                zone.ZoneName =
                    model.ZoneName.Trim();

                zone.MovementType =
                    movementType;

                zone.Priority =
                    model.Priority;

                zone.ColumnFrom =
                    model.ColumnFrom;

                zone.ColumnTo =
                    model.ColumnTo;

                zone.RowFrom =
                    model.RowFrom;

                zone.RowTo =
                    model.RowTo;

                zone.LevelFrom =
                    model.LevelFrom;

                zone.LevelTo =
                    model.LevelTo;

                zone.Status =
                    string.IsNullOrWhiteSpace(model.Status)
                        ? "Active"
                        : model.Status.Trim();

                zone.ModifiedDate =
                    DateTime.Now;

                db.SaveChanges();

                // -------------------------------------------------
                // Return updated record
                // -------------------------------------------------

                var updatedZone = db.ASRSLocationZoneMasters
                    .Where(x => x.Id == zone.Id)
                    .Select(x => new
                    {
                        x.Id,
                        x.ZoneCode,
                        x.ZoneName,
                        x.MovementType,
                        x.Priority,
                        x.ColumnFrom,
                        x.ColumnTo,
                        x.RowFrom,
                        x.RowTo,
                        x.LevelFrom,
                        x.LevelTo,
                        x.Status,
                        x.CreatedDate,
                        x.ModifiedDate
                    })
                    .FirstOrDefault();

                return Json(new
                {
                    success = true,
                    message = "Zone Master updated successfully.",
                    data = updatedZone
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


        // =========================================================
        // GET LEVEL
        // =========================================================

        [HttpGet]
        public JsonResult GetLevel(int level)
        {
            try
            {
                var data = db.GoodsExisitings
                    .Where(x =>
                        x.Layer == level &&
                        x.DeleteStatus == "N")
                    .Select(x => new ASRSPosition
                    {
                        LocationNo = x.LocationName,
                        Level = x.Layer,
                        Column = x.Col,
                        Position = x.Row,
                        Status = x.LocaStatus,
                        PalletNo = x.PalletNo,
                        Qty = x.Quantity ?? 0,
                        PalletQr = x.PalletQr
                    })
                    .OrderBy(x => x.Column)
                    .ThenBy(x => x.Position)
                    .ToList();

                return Json(new
                {
                    success = true,
                    level = level,
                    data = data
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    data = new List<ASRSPosition>()
                }, JsonRequestBehavior.AllowGet);
            }
        }


        // =========================================================
        // GET POSITIONS
        // =========================================================

        [HttpGet]
        public JsonResult GetPositions(int level, int column)
        {
            try
            {
                var data = db.GoodsExisitings
                    .Where(x =>
                        x.Layer == level &&
                        x.Col == column &&
                        x.DeleteStatus == "N")
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
                    //.OrderBy(x => x.Position) check and uncomnt later
                    .OrderByDescending(x => x.Position)
                    .ToList();

                return Json(new
                {
                    success = true,
                    data = data
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    data = new List<ASRSPosition>()
                }, JsonRequestBehavior.AllowGet);
            }
        }


        // =========================================================
        // LOADING SIMULATION
        // =========================================================

        public JsonResult GetLoadingSimulation()
        {
            using (var conn = new S1947Entities())
            {
                var data = conn.Database.SqlQuery<GELoadingView>(@"
                    SELECT
                        MTransNo,
                        LocationName,
                        Layer,
                        Row,
                        Col,
                        FGModel,
                        FGDesc,
                        FGCode,
                        PalletQr,
                        PalletNo,
                        Quantity,
                        LocaStatus,
                        Cat,
                        sequence
                    FROM dbo.GELoadingView
                    ORDER BY sequence
                ").ToList();

                return Json(
                    data,
                    JsonRequestBehavior.AllowGet
                );
            }
        }


        // =========================================================
        // SUMMARY
        // =========================================================

        [HttpGet]
        public JsonResult GetSummary(int level)
        {
            try
            {
                var statuses = db.GoodsExisitings
                    .Where(x =>
                        x.Layer == level &&
                        x.DeleteStatus == "N")
                    .Select(x => x.LocaStatus)
                    .ToList();

                int empty = statuses.Count(x =>
                    !string.IsNullOrWhiteSpace(x) &&
                    x.Trim().ToUpper() == "EMPTY");

                int loaded = statuses.Count(x =>
                    !string.IsNullOrWhiteSpace(x) &&
                    x.Trim().ToUpper() == "LOADED");

                int available = statuses.Count(x =>
                    !string.IsNullOrWhiteSpace(x) &&
                    x.Trim().ToUpper() == "AVAILABLE");

                int blocked = statuses.Count(x =>
                    !string.IsNullOrWhiteSpace(x) &&
                    x.Trim().ToUpper() == "BLOCKED");

                int unknown = statuses.Count(x =>
                    string.IsNullOrWhiteSpace(x) ||
                    (
                        x.Trim().ToUpper() != "EMPTY" &&
                        x.Trim().ToUpper() != "LOADED" &&
                        x.Trim().ToUpper() != "AVAILABLE" &&
                        x.Trim().ToUpper() != "BLOCKED"
                    ));

                int total = statuses.Count;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        total = total,
                        empty = empty,
                        loaded = loaded,
                        available = available,
                        blocked = blocked,
                        unknown = unknown
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return new JsonResult
                {
                    Data = new
                    {
                        success = false,
                        message = ex.Message
                    },
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
        }

        // =========================================================
        // GET ALL ASRS 3D DATA
        // =========================================================

        [HttpGet]
        public JsonResult Get3DData()
        {
            try
            {
                var data = db.GoodsExisitings
                    .Where(x =>
                        x.DeleteStatus == "N" &&
                        x.Layer >= 1 &&
                        x.Layer <= 8 &&
                        x.Col >= 1 &&
                        x.Col <= 44 &&
                        x.Row >= 1 &&
                        x.Row <= 18)
                    .Select(x => new
                    {
                        LocationNo = x.LocationName,

                        Level = x.Layer,

                        Column = x.Col,

                        Row = x.Row,

                        Status = x.LocaStatus,

                        PalletNo = x.PalletNo,

                        Qty = x.Quantity ?? 0
                    })
                    .OrderBy(x => x.Level)
                    .ThenBy(x => x.Column)
                    .ThenBy(x => x.Row)
                    .ToList();

                return Json(new
                {
                    success = true,
                    data = data
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    data = new List<object>()
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult Get3DMovementData()
        {
            try
            {
                var data = db.GELoadingViews
                    .Select(x => new
                    {
                        LocationNo = x.LocationName,
                        Level = x.Layer,
                        Column = x.Col,
                        Row = x.Row,
                        Status = x.LocaStatus
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    data = data
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    data = new object[0]
                }, JsonRequestBehavior.AllowGet);
            }
        }


        // =========================================================
        // DISPOSE
        // =========================================================

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }


    // =============================================================
    // ZONE UPDATE MODEL
    // =============================================================

    public class ZoneMasterUpdateModel
    {
        public int Id { get; set; }

        public string ZoneCode { get; set; }

        public string ZoneName { get; set; }

        public string MovementType { get; set; }

        public int Priority { get; set; }

        public int ColumnFrom { get; set; }

        public int ColumnTo { get; set; }

        public int RowFrom { get; set; }

        public int RowTo { get; set; }

        public int LevelFrom { get; set; }

        public int LevelTo { get; set; }

        public string Status { get; set; }
    }
}