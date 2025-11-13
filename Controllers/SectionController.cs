using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LMS.Models;

namespace LMS.Controllers
{
    public class SectionController : Controller
    {
        private readonly LMSContext db = new LMSContext();

        // ✅ CHECK IF SECTION EXISTS (for real-time validation)
        [HttpGet]
        public JsonResult CheckSectionExists(int programId, int yearLevel, string sectionName, int? excludeId = null)
        {
            try
            {
                if (programId == 0 || yearLevel == 0 || string.IsNullOrWhiteSpace(sectionName))
                {
                    return Json(new { exists = false }, JsonRequestBehavior.AllowGet);
                }

                var query = db.Sections
                    .Where(s => s.ProgramId == programId && 
                                s.YearLevel == yearLevel && 
                                s.SectionName.Trim().ToLower() == sectionName.Trim().ToLower());

                // Exclude current section when editing
                if (excludeId.HasValue && excludeId.Value > 0)
                {
                    query = query.Where(s => s.Id != excludeId.Value);
                }

                bool exists = query.Any();

                if (exists)
                {
                    var program = db.Programs.Find(programId);
                    string yearSuffix = GetOrdinalSuffix(yearLevel);
                    string message = $"Section '{sectionName}' already exists for {program?.ProgramCode} - {yearLevel}{yearSuffix} Year";
                    
                    return Json(new { exists = true, message = message }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { exists = false }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { exists = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // ✅ CREATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Create(Section model)
        {
            ModelState.Remove("Id");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                return Json(new { success = false, message = string.Join(", ", errors) });
            }

            // Enhanced validation: Check if section already exists for this program and year level
            var existingSection = db.Sections
                .FirstOrDefault(s => s.ProgramId == model.ProgramId && 
                                     s.YearLevel == model.YearLevel && 
                                     s.SectionName.Trim().ToLower() == model.SectionName.Trim().ToLower());

            if (existingSection != null)
            {
                var program = db.Programs
                    .Include(p => p.Department)
                    .FirstOrDefault(p => p.Id == model.ProgramId);
                
                string yearSuffix = GetOrdinalSuffix(model.YearLevel);
                string errorMessage = $"Section '{model.SectionName}' already exists for {program?.ProgramCode} - {model.YearLevel}{yearSuffix} Year.";
                
                return Json(new { success = false, message = errorMessage });
            }

            model.DateCreated = DateTime.Now;
            db.Sections.Add(model);
            db.SaveChanges();

            // Load the program to get program details
            var programData = db.Programs
                .Include(p => p.Department)
                .FirstOrDefault(p => p.Id == model.ProgramId);

            string yearSuffixResult = GetOrdinalSuffix(model.YearLevel);

            return Json(new
            {
                success = true,
                message = "Section created successfully.",
                data = new
                {
                    id = model.Id,
                    sectionName = model.SectionName,
                    programId = model.ProgramId,
                    programCode = programData?.ProgramCode ?? "",
                    programName = programData?.ProgramName ?? "",
                    yearLevel = model.YearLevel,
                    yearLevelDisplay = $"{model.YearLevel}{yearSuffixResult} Year",
                    studentCount = 0,
                    dateCreated = model.DateCreated.ToString("MM/dd/yyyy"),
                    timeCreated = model.DateCreated.ToString("hh:mm tt")
                }
            });
        }

        // ✅ GET BY ID (for Edit Modal)
        [HttpGet]
        public JsonResult GetById(int id)
        {
            try
            {
                var section = db.Sections
                    .Include(s => s.Program)
                    .Include(s => s.Program.Department)
                    .FirstOrDefault(s => s.Id == id);

                if (section == null)
                {
                    return Json(new { success = false, message = "Section not found." },
                        JsonRequestBehavior.AllowGet);
                }

                string yearSuffix = GetOrdinalSuffix(section.YearLevel);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = section.Id,
                        sectionName = section.SectionName,
                        programId = section.ProgramId,
                        departmentId = section.Program?.DepartmentId ?? 0,
                        yearLevel = section.YearLevel,
                        yearLevelDisplay = $"{section.YearLevel}{yearSuffix} Year",
                        programCode = section.Program?.ProgramCode ?? "",
                        dateCreated = section.DateCreated.ToString("MM/dd/yyyy HH:mm tt")
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading section: " + ex.Message },
                    JsonRequestBehavior.AllowGet);
            }
        }

        // ✅ EDIT / UPDATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Edit(Section model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                     .SelectMany(v => v.Errors)
                     .Select(e => e.ErrorMessage);

                return Json(new { success = false, message = string.Join(", ", errors) });
            }

            var section = db.Sections.Find(model.Id);

            if (section == null)
            {
                return Json(new { success = false, message = "Section not found." });
            }

            // Enhanced validation: Check if another section with the same name exists for the same program/year
            var duplicate = db.Sections
                .FirstOrDefault(s => s.Id != model.Id && 
                                     s.ProgramId == model.ProgramId && 
                                     s.YearLevel == model.YearLevel && 
                                     s.SectionName.Trim().ToLower() == model.SectionName.Trim().ToLower());

            if (duplicate != null)
            {
                var program = db.Programs
                    .Include(p => p.Department)
                    .FirstOrDefault(p => p.Id == model.ProgramId);
                
                string yearSuffix = GetOrdinalSuffix(model.YearLevel);
                string errorMessage = $"Section '{model.SectionName}' already exists for {program?.ProgramCode} - {model.YearLevel}{yearSuffix} Year. Please choose a different section name.";
                
                return Json(new { success = false, message = errorMessage });
            }

            // ✅ UPDATE FIELDS
            section.SectionName = model.SectionName;
            section.ProgramId = model.ProgramId;
            section.YearLevel = model.YearLevel;

            db.SaveChanges();

            // Reload with program
            db.Entry(section).Reference(s => s.Program).Load();
            db.Entry(section.Program).Reference(p => p.Department).Load();

            string yearSuffixResult = GetOrdinalSuffix(section.YearLevel);

            return Json(new
            {
                success = true,
                message = "Section updated successfully.",
                data = new
                {
                    id = section.Id,
                    sectionName = section.SectionName,
                    programId = section.ProgramId,
                    programCode = section.Program?.ProgramCode ?? "",
                    programName = section.Program?.ProgramName ?? "",
                    yearLevel = section.YearLevel,
                    yearLevelDisplay = $"{section.YearLevel}{yearSuffixResult} Year",
                    studentCount = 0, // This would need to be calculated from actual student data
                    dateCreated = section.DateCreated.ToString("MM/dd/yyyy"),
                    timeCreated = section.DateCreated.ToString("hh:mm tt")
                }
            });
        }

        // ✅ DELETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
                var section = db.Sections
                    .Include(s => s.Program)
                    .FirstOrDefault(s => s.Id == id);

                if (section == null)
                {
                    return Json(new { success = false, message = "Section not found!" });
                }

                string sectionName = $"{section.Program?.ProgramCode} {section.YearLevel}-{section.SectionName}";

                db.Sections.Remove(section);
                db.SaveChanges();

                return Json(new { success = true, message = $"Section {sectionName} deleted successfully!" });
        }

        // ✅ GET SECTIONS BY PROGRAM
        [HttpGet]
        public JsonResult GetSectionsByProgram(int programId, int? yearLevel = null)
        {
            try
            {
                var query = db.Sections
                    .Where(s => s.ProgramId == programId);

                if (yearLevel.HasValue)
                {
                    query = query.Where(s => s.YearLevel == yearLevel.Value);
                }

                var sections = query
                    .Select(s => new
                    {
                        id = s.Id,
                        sectionName = s.SectionName,
                        yearLevel = s.YearLevel,
                        fullName = s.SectionName + " - " + s.YearLevel + GetOrdinalSuffix(s.YearLevel) + " Year"
                    })
                    .ToList();

                return Json(sections, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading sections: " + ex.Message },
                    JsonRequestBehavior.AllowGet);
            }
        }

        // Helper method to get ordinal suffix
        private string GetOrdinalSuffix(int number)
        {
            if (number % 100 >= 11 && number % 100 <= 13)
                return "th";

            switch (number % 10)
            {
                case 1: return "st";
                case 2: return "nd";
                case 3: return "rd";
                default: return "th";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}