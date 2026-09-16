using S1947.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace S1947.Controllers
{
    public class MaterialMasterController : Controller
    {
        // GET: MaterialMaster
        //For common use
        S1947Entities conn = new S1947Entities();
        MaterialMasterValidation material = new MaterialMasterValidation();
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult AddEdit(int MTransNo = 0)
        {
            ViewBag.LockStatus = new List<SelectListItem>(){
                new SelectListItem {Text="Inactive", Value="N", Selected=false},
                new SelectListItem {Text="Active", Value="Y", Selected=false},
                };

            ViewBag.Type = new List<SelectListItem>(){
                new SelectListItem {Text="Runner", Value="A", Selected=false},
                new SelectListItem {Text="Medium", Value="B", Selected=false},
                new SelectListItem {Text="Slower", Value="C", Selected=false},
                };

            if (MTransNo > 0)
            {
                var details = conn.PartMasters.FirstOrDefault(x => x.MTransNo == MTransNo);

                if (details != null)
                {
                    material.MTransNo = details.MTransNo;
                    material.FGCode = details.FGCode;
                    material.FGName = details.FGModel;
                    material.FGDesc = details.FGDesc;
                    material.Type = details.Type.Trim();
                    material.LockStatus = details.LockStatus;
                }

                return View(material);
            }
            else
            {
                return View(material);
            }

        }
        public ActionResult SaveData(MaterialMasterValidation data)
        {
            var lockstatus = Convert.ToString(data.LockStatus);
            var craetedon = DateTime.Now;
            var ModifiedOn = DateTime.Now;
            var CreatedBy = Session["MTransNo"];
            var ModifiedBy = Session["MTransNo"];


            if (data.MTransNo > 0)
            {
                var Material = conn.PartMasters
                                   .FirstOrDefault(x => x.MTransNo == data.MTransNo);

                if (Material != null)
                {
                    Material.FGCode = data.FGCode;
                    Material.FGModel = data.FGName;
                    Material.FGDesc = data.FGName;
                    Material.Type = data.Type.Trim();
                    Material.LockStatus = data.LockStatus;
                    Material.ModifiedOn = DateTime.Now;
                    Material.ModifiedBy = ModifiedBy != null
                        ? Convert.ToInt16(ModifiedBy)
                        : (short?)null;


                    conn.SaveChanges();

                    return Json(new
                    {
                        success = true,
                        message = "✅ Material Updated Successfully!"
                    });
                }

                return Json(new
                {
                    success = false,
                    message = "Material not found."
                });
            }
            else
            {
                var Material = new PartMaster
                {
                    FGCode = data.FGCode,
                    FGModel = data.FGName,
                    FGDesc = data.FGName,
                    Type = data.Type.Trim(),
                    LockStatus = data.LockStatus,
                    DeleteStatus = "N",
                    CreatedOn = DateTime.Now,
                    ModifiedOn = DateTime.Now,
                    CreatedBy = CreatedBy != null ? Convert.ToInt16(CreatedBy) : (short?)null,
                    ModifiedBy = ModifiedBy != null ? Convert.ToInt16(ModifiedBy) : (short?)null
                };

                conn.PartMasters.Add(Material);
                conn.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "✅ Material Added Successfully!"
                });
            }

          
        }
        public JsonResult isCodeExists(string MaterialCode)
        {
            return Json(!conn.PartMasters.Any(x => x.FGCode == MaterialCode), JsonRequestBehavior.AllowGet);
        }
        public JsonResult isNameExists(string MaterialName)
        {
            MaterialName = MaterialName.Trim();
            return Json(!conn.PartMasters.Any(x => x.FGModel == MaterialName), JsonRequestBehavior.AllowGet);
        }
    }
}
