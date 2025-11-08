using LMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace LMS.Controllers
{
    public class DepartmentController : Controller
    {
        private readonly LMSContext db = new LMSContext();

        // ✅ CREATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Create(Department model)
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
            db.Departments.Add(model);
            db.SaveChanges();

            return Json(new
            {
                success = true,
                message = "Department created successfully.",
                data = new
                {
                    id = model.Id,
                    departmentName = model.DepartmentName,
                    departmentCode = model.DepartmentCode,
                    dateCreated = model.DateCreated.ToString("yyyy-MM-dd HH:mm tt")
                }
            });
        }


        // ✅ GET BY ID (for Edit Modal)
        [HttpGet]
        public JsonResult GetById(int id)
        {
            var dept = db.Departments.Find(id);

            if (dept == null)
            {
                return Json(new { success = false, message = "Department not found." },
                    JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    id = dept.Id,
                    departmentName = dept.DepartmentName,
                    departmentCode = dept.DepartmentCode,
                    dateCreated = dept.DateCreated.ToString("yyyy-MM-dd HH:mm tt")


                }
            }, JsonRequestBehavior.AllowGet);
        }


        // ✅ EDIT / UPDATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Edit(Department model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                     .SelectMany(v => v.Errors)
                     .Select(e => e.ErrorMessage);

                return Json(new { success = false, message = string.Join(", ", errors) });
            }

            var dept = db.Departments.Find(model.Id);

            if (dept == null)
            {
                return Json(new { success = false, message = "Department not found." });
            }

            dept.DepartmentName = model.DepartmentName;
            dept.DepartmentCode = model.DepartmentCode;
            db.SaveChanges();

            return Json(new
            {
                success = true,
                message = "Department updated successfully.",
                data = new
                {
                    id = dept.Id,
                    departmentName = dept.DepartmentName,
                    departmentCode = dept.DepartmentCode,
                    dateCreated = model.DateCreated.ToString("yyyy-MM-dd HH:mm tt")

                }
            });
        }


        // ✅ DELETE
        [HttpPost]
        public JsonResult Delete(int id)
        {
            var dept = db.Departments.Find(id);

            if (dept == null)
            {
                return Json(new { success = false, message = "Department not found." });
            }

            db.Departments.Remove(dept);
            db.SaveChanges();

            return Json(new
            {
                success = true,
                message = $"{dept.DepartmentCode} deleted successfully.",
                data = new { id = id }
            });
        }

        // ✅ GET ALL DEPARTMENTS
        [HttpGet]
        public List<Department> GetAll()
        {
            return db.Departments
                .OrderBy(d => d.DepartmentName)
                .ToList();
        }


    }
}
