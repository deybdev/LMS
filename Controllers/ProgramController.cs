using LMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;

namespace LMS.Controllers
{
    public class ProgramController : Controller
    {
        private readonly LMSContext db = new LMSContext();

        // ✅ CREATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Create(Program model)
        {
            ModelState.Remove("Id");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                return Json(new { success = false, message = string.Join(", ", errors) });
            }

            model.DateCreated = DateTime.Now;
            db.Programs.Add(model);
            db.SaveChanges();

            // Load the department to get the department code
            var department = db.Departments.Find(model.DepartmentId);

            return Json(new
            {
                success = true,
                message = "Program created successfully.",
                data = new
                {
                    id = model.Id,
                    programName = model.ProgramName,
                    programCode = model.ProgramCode,
                    programDuration = model.ProgramDuration,
                    departmentId = model.DepartmentId,
                    departmentCode = department?.DepartmentCode,
                    dateCreated = model.DateCreated.ToString("yyyy-MM-dd HH:mm tt")
                }
            });
        }

        // ✅ GET BY ID (for Edit Modal)
        [HttpGet]
        public JsonResult GetById(int id)
        {
            try
            {
                var prog = db.Programs
                    .Include(p => p.Department)
                    .FirstOrDefault(p => p.Id == id);

                if (prog == null)
                {
                    return Json(new { success = false, message = "Program not found." },
                        JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = prog.Id,
                        programName = prog.ProgramName,
                        programCode = prog.ProgramCode,
                        programDuration = prog.ProgramDuration,
                        departmentId = prog.DepartmentId,
                        departmentCode = prog.Department?.DepartmentCode ?? "",
                        dateCreated = prog.DateCreated.ToString("yyyy-MM-dd HH:mm tt")
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading program: " + ex.Message },
                    JsonRequestBehavior.AllowGet);
            }
        }

        // ✅ EDIT / UPDATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Edit(Program model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                     .SelectMany(v => v.Errors)
                     .Select(e => e.ErrorMessage);

                return Json(new { success = false, message = string.Join(", ", errors) });
            }

            var prog = db.Programs.Find(model.Id);

            if (prog == null)
            {
                return Json(new { success = false, message = "Program not found." });
            }

            // ✅ UPDATE FIELDS
            prog.ProgramName = model.ProgramName;
            prog.ProgramCode = model.ProgramCode;
            prog.ProgramDuration = model.ProgramDuration;

            // ✅ NEW — UPDATE DEPARTMENT
            prog.DepartmentId = model.DepartmentId;

            db.SaveChanges();

            // Reload with department
            db.Entry(prog).Reference(p => p.Department).Load();

            return Json(new
            {
                success = true,
                message = "Program updated successfully.",
                data = new
                {
                    id = prog.Id,
                    programName = prog.ProgramName,
                    programCode = prog.ProgramCode,
                    programDuration = prog.ProgramDuration,
                    departmentId = prog.DepartmentId,
                    departmentCode = prog.Department?.DepartmentCode,
                    dateCreated = prog.DateCreated.ToString("yyyy-MM-dd hh:mm tt")
                }
            });
        }

        // ✅ DELETE
        [HttpPost]
        public JsonResult Delete(int id)
        {
            var prog = db.Programs.Find(id);

            if (prog == null)
            {
                return Json(new { success = false, message = "Program not found." });
            }

            db.Programs.Remove(prog);
            db.SaveChanges();

            return Json(new
            {
                success = true,
                message = $"{prog.ProgramName} deleted successfully.",
                data = new { id = id }
            });
        }
    }
}