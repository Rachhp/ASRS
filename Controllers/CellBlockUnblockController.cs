using S1947.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace S1947.Controllers
{
    public class CellBlockUnblockController : Controller
    {
        S1947Entities conn = new S1947Entities();
        public ActionResult CellBlockUnblock()
        {
            LoadingSheetModel model = new LoadingSheetModel();

            var locations = conn.GoodsExisitings
                                .Where(x => x.DeleteStatus == "N")
                                .AsQueryable();
            var blockLocations = locations
                .GroupBy(x => new
                {
                    x.Layer,
                    x.Col
                })
                .Where(g =>                 
                    g.Any(x => x.LocaStatus == "AVAILABLE")
                    && !g.Any(x =>
                        x.LocaStatus == "LOADED"
                        || x.PalletQr != null
                        || x.PalletNo != null
                        || x.Quantity > 0
                    )
                    && !g.Any(x => x.LocaStatus == "BLOCKED")
                )
                .Select(g => new
                {
                    CellNo = g.Key.Layer + "," + g.Key.Col,
                    DisplayName ="Level " + g.Key.Layer + ", Col " + g.Key.Col
                })
                .OrderBy(x => x.CellNo)
                .ToList();


            ViewBag.LocationList = new SelectList(
                blockLocations,
                "CellNo",
                "DisplayName"
            );

            var unblockLocations = locations
                .GroupBy(x => new
                {
                    x.Layer,
                    x.Col
                })
                .Where(g =>
                    g.Any(x => x.LocaStatus == "BLOCKED")
                )
                .Select(g => new
                {
                    CellNo = g.Key.Layer + "," + g.Key.Col,

                    DisplayName =
                        "Level " + g.Key.Layer +
                        ", Col " + g.Key.Col
                })
                .OrderBy(x => x.CellNo)
                .ToList();


            ViewBag.LocationList1 = new SelectList(
                unblockLocations,
                "CellNo",
                "DisplayName"
            );


            return View(model);
        }


        [HttpPost]
        public JsonResult BlockCell(string locationId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(locationId))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Please select a location."
                    });
                }

                var parts = locationId.Split(',');
                if (parts.Length != 2)
                {
                    return Json(new
                    {
                       success = false,
                       message = "Invalid location format."
                    });
                }


                int layer;
                int col;


                if (!int.TryParse(parts[0], out layer) ||
                    !int.TryParse(parts[1], out col))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid layer or column."
                    });
                }


                // ----------------------------------------------------
                // Get complete cell
                // ----------------------------------------------------

                var cellRows = conn.GoodsExisitings
                    .Where(x =>
                        x.Layer == layer &&
                        x.Col == col &&
                        x.DeleteStatus == "N")
                    .ToList();


                if (!cellRows.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Location not found."
                    });
                }


                // ----------------------------------------------------
                // Check whether anything is loaded
                //
                // If ANY row in this Layer + Col has:
                //
                // LocaStatus = LOADED
                // OR PalletQr exists
                // OR PalletNo exists
                // OR Quantity > 0
                //
                // then blocking is NOT allowed.
                // ----------------------------------------------------

                bool isLoaded = cellRows.Any(x =>
                    x.LocaStatus == "LOADED"
                    || x.PalletQr != null
                    || x.PalletNo != null
                    || x.Quantity > 0
                );


                if (isLoaded)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "Cannot block Level " +
                            layer +
                            ", Col " +
                            col +
                            " because material is loaded."
                    });
                }


                // ----------------------------------------------------
                // Get AVAILABLE rows only
                // ----------------------------------------------------

                var availableRows = cellRows
                    .Where(x => x.LocaStatus == "AVAILABLE")
                    .ToList();


                if (!availableRows.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "Level " +
                            layer +
                            ", Col " +
                            col +
                            " is not available for blocking."
                    });
                }


                // ----------------------------------------------------
                // Block the AVAILABLE rows
                // ----------------------------------------------------

                foreach (var row in availableRows)
                {
                    row.LocaStatus = "BLOCKED";

                    // Replace this with your logged-in user ID
                    row.ModifiedBy = 1;

                    row.ModifiedOn = DateTime.Now;

                    conn.Entry(row).State =
                        System.Data.Entity.EntityState.Modified;
                }


                // ----------------------------------------------------
                // Save
                // ----------------------------------------------------

                conn.SaveChanges();


                return Json(new
                {
                    success = true,
                    message =
                        "Level " +
                        layer +
                        ", Col " +
                        col +
                        " blocked successfully."
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


        // ============================================================
        // UNBLOCK CELL
        // ============================================================

        [HttpPost]
        public JsonResult UnblockCell(string locationId)
        {
            try
            {
                // ----------------------------------------------------
                // Validate location
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(locationId))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Please select a location."
                    });
                }


                // ----------------------------------------------------
                // locationId:
                //
                // 2,1
                // 2,2
                //
                // First = Layer
                // Second = Col
                // ----------------------------------------------------

                var parts = locationId.Split(',');


                if (parts.Length != 2)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid location format."
                    });
                }


                int layer;
                int col;


                if (!int.TryParse(parts[0], out layer) ||
                    !int.TryParse(parts[1], out col))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid layer or column."
                    });
                }


                // ----------------------------------------------------
                // Get complete cell
                // ----------------------------------------------------

                var cellRows = conn.GoodsExisitings
                    .Where(x =>
                        x.Layer == layer &&
                        x.Col == col &&
                        x.DeleteStatus == "N")
                    .ToList();


                if (!cellRows.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Location not found."
                    });
                }


                // ----------------------------------------------------
                // Get BLOCKED rows
                // ----------------------------------------------------

                var blockedRows = cellRows
                    .Where(x => x.LocaStatus == "BLOCKED")
                    .ToList();


                if (!blockedRows.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "Level " +
                            layer +
                            ", Col " +
                            col +
                            " is not blocked."
                    });
                }


                // ----------------------------------------------------
                // Unblock
                // ----------------------------------------------------

                foreach (var row in blockedRows)
                {
                    row.LocaStatus = "AVAILABLE";

                    // Replace this with your logged-in user ID
                    row.ModifiedBy = 1;

                    row.ModifiedOn = DateTime.Now;

                    conn.Entry(row).State =
                        System.Data.Entity.EntityState.Modified;
                }


                // ----------------------------------------------------
                // Save
                // ----------------------------------------------------

                conn.SaveChanges();


                return Json(new
                {
                    success = true,
                    message =
                        "Level " +layer +
                        ", Col " + col +
                        " unblocked successfully."
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
