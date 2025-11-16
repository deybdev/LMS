using LMS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LMS.Controllers
{
    public class TeacherController : Controller
    {
        private readonly LMSContext db = new LMSContext();

        // GET: Teacher/Index
        public ActionResult Index()
        {
            var model = new AdminDashboardViewModel
            {
                AuditLogs = db.AuditLogs.OrderByDescending(a => a.Timestamp).Take(10).ToList(),
                UpcomingEvents = db.Events.Where(e => e.StartDate >= DateTime.Now).OrderBy(e => e.StartDate).Take(5).ToList(),
                TotalStudents = db.Users.Count(u => u.Role == "Student"),
                TotalTeachers = db.Users.Count(u => u.Role == "Teacher"),
                TotalCourses = db.Courses.Count()
            };

            return View(model);
        }

        // GET: Teacher/Course
        public ActionResult Course()
        {
            // Validate session
            if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
            {
                TempData["ErrorMessage"] = "Session expired. Please log in again.";
                return RedirectToAction("Login", "Home");
            }

            int teacherId = Convert.ToInt32(Session["Id"]);

            var assignments = db.TeacherCourseSections
                .Where(tcs => tcs.TeacherId == teacherId)
                .Include(tcs => tcs.Course)
                .Include(tcs => tcs.Section.Program)
                .ToList()
                .Select(tcs =>
                {
                    dynamic item = new ExpandoObject();
                    item.CourseId = tcs.CourseId;
                    item.CourseCode = tcs.Course.CourseCode;
                    item.CourseName = tcs.Course.CourseTitle;
                    item.SectionName = $"{tcs.Section.YearLevel}{tcs.Section.SectionName}";
                    item.SectionId = tcs.SectionId;
                    item.Students = db.StudentCourses.Count(sc => sc.CourseId == tcs.CourseId && sc.SectionId == tcs.SectionId);
                    item.Day = "Mon-Fri";
                    item.Time = "8:00 AM - 10:00 AM";
                    item.SchoolYear = $"{DateTime.Now.Year} - {DateTime.Now.Year + 1}";
                    return item;
                })
                .ToList();

            return View(assignments);
        }

        // GET: Teacher/Material
        public ActionResult Material(int? id)
        {
            return LoadCourseTab(id, "Material");
        }

        // GET: Teacher/Gradebook
        public ActionResult Gradebook(int? id)
        {
            return LoadCourseTab(id, "Gradebook");
        }

        // POST: Upload Material
        [HttpPost]
        public ActionResult UploadMaterial(int teacherCourseSectionId, string materialTitle, string materialType, string materialDescription, HttpPostedFileBase[] materialFile)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"UploadMaterial called - TeacherCourseSectionId: {teacherCourseSectionId}, Title: {materialTitle}");
                System.Diagnostics.Debug.WriteLine($"File count: {materialFile?.Length ?? 0}");

                if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
                    return Json(new { success = false, message = "Unauthorized access." });

                int teacherId = Convert.ToInt32(Session["Id"]);

                // Verify the teacher owns this course section
                var teacherCourseSection = db.TeacherCourseSections
                    .FirstOrDefault(tcs => tcs.Id == teacherCourseSectionId && tcs.TeacherId == teacherId);

                if (teacherCourseSection == null)
                    return Json(new { success = false, message = "You don't have access to this course section." });

                // Validate required fields
                if (string.IsNullOrWhiteSpace(materialTitle))
                    return Json(new { success = false, message = "Material title is required." });

                if (string.IsNullOrWhiteSpace(materialType))
                    return Json(new { success = false, message = "Material type is required." });

                if (materialFile == null || materialFile.Length == 0 || materialFile[0] == null)
                    return Json(new { success = false, message = "Please select at least one file to upload." });

                // Create the material record with section-specific link
                var material = new Material
                {
                    TeacherCourseSectionId = teacherCourseSectionId,
                    Title = materialTitle,
                    Type = materialType,
                    Description = materialDescription ?? string.Empty,
                    UploadedAt = DateTime.Now
                };
                db.Materials.Add(material);
                db.SaveChanges();

                // Create upload folder if it doesn't exist
                var uploadFolder = Server.MapPath("~/Uploads/Materials/");
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                // Process each uploaded file
                int filesUploaded = 0;
                foreach (var file in materialFile)
                {
                    if (file != null && file.ContentLength > 0)
                    {
                        // Generate unique filename to avoid conflicts
                        var fileName = Path.GetFileName(file.FileName);
                        var uniqueFileName = $"{DateTime.Now.Ticks}_{fileName}";
                        var filePath = Path.Combine(uploadFolder, uniqueFileName);

                        // Save the physical file
                        file.SaveAs(filePath);

                        // Create database record for the file
                        var fileRecord = new MaterialFile
                        {
                            MaterialId = material.Id,
                            FileName = fileName,
                            FilePath = "/Uploads/Materials/" + uniqueFileName,
                            SizeInMB = Math.Round((decimal)file.ContentLength / 1024 / 1024, 2)
                        };
                        db.MaterialFiles.Add(fileRecord);
                        filesUploaded++;
                    }
                }

                db.SaveChanges();

                return Json(new { 
                    success = true, 
                    message = $"Material uploaded successfully with {filesUploaded} file(s) for this section." 
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in UploadMaterial: " + ex.ToString());
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        // GET: Edit Material (returns JSON for AJAX)
        [HttpGet]
        public ActionResult GetMaterial(int id)
        {
            try
            {
                if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
                {
                    return Json(new { success = false, message = "Session expired." }, JsonRequestBehavior.AllowGet);
                }

                int teacherId = Convert.ToInt32(Session["Id"]);

                var material = db.Materials
                    .Include(m => m.MaterialFiles)
                    .Include(m => m.TeacherCourseSection)
                    .FirstOrDefault(m => m.Id == id);

                if (material == null)
                {
                    return Json(new { success = false, message = "Material not found." }, JsonRequestBehavior.AllowGet);
                }

                // Verify the teacher owns this material's section
                if (material.TeacherCourseSection.TeacherId != teacherId)
                {
                    return Json(new { success = false, message = "You don't have access to this material." }, JsonRequestBehavior.AllowGet);
                }

                var files = material.MaterialFiles.Select(f => new
                {
                    id = f.Id,
                    fileName = f.FileName,
                    filePath = f.FilePath,
                    sizeInMB = f.SizeInMB
                }).ToList();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = material.Id,
                        title = material.Title,
                        type = material.Type,
                        description = material.Description,
                        files = files
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return Json(new { success = false, message = "An error occurred." }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Update Material
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateMaterial(int id, string materialTitle, string materialType,
            string materialDescription, HttpPostedFileBase[] materialFile, string filesToDelete)
        {
            try
            {
                if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                var material = db.Materials.Include(m => m.MaterialFiles).FirstOrDefault(m => m.Id == id);
                if (material == null)
                {
                    return Json(new { success = false, message = "Material not found." });
                }

                // Update basic info
                material.Title = materialTitle;
                material.Type = materialType;
                material.Description = materialDescription ?? string.Empty;

                // Delete selected files
                if (!string.IsNullOrEmpty(filesToDelete))
                {
                    var fileIdsToDelete = filesToDelete.Split(',').Select(int.Parse).ToList();
                    foreach (var fileId in fileIdsToDelete)
                    {
                        var file = material.MaterialFiles.FirstOrDefault(f => f.Id == fileId);
                        if (file != null)
                        {
                            var path = Server.MapPath(file.FilePath);
                            if (System.IO.File.Exists(path)) 
                                System.IO.File.Delete(path);
                            db.MaterialFiles.Remove(file);
                        }
                    }
                }

                // Add new files
                if (materialFile != null && materialFile.Length > 0 && materialFile[0] != null)
                {
                    var uploadFolder = Server.MapPath("~/Uploads/Materials/");
                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);

                    foreach (var file in materialFile)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            var fileName = Path.GetFileName(file.FileName);
                            var uniqueFileName = $"{DateTime.Now.Ticks}_{fileName}";
                            var filePath = Path.Combine(uploadFolder, uniqueFileName);
                            file.SaveAs(filePath);

                            db.MaterialFiles.Add(new MaterialFile
                            {
                                MaterialId = material.Id,
                                FileName = fileName,
                                FilePath = "/Uploads/Materials/" + uniqueFileName,
                                SizeInMB = Math.Round((decimal)file.ContentLength / 1024 / 1024, 2)
                            });
                        }
                    }
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Material updated successfully." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return Json(new { success = false, message = "An error occurred while updating the material." });
            }
        }

        // POST: Delete Material
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteMaterial(int id)
        {
            try
            {
                if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                int teacherId = Convert.ToInt32(Session["Id"]);

                var material = db.Materials
                    .Include(m => m.MaterialFiles)
                    .Include(m => m.TeacherCourseSection)
                    .FirstOrDefault(m => m.Id == id);

                if (material == null)
                {
                    return Json(new { success = false, message = "Material not found." });
                }

                // Verify the teacher owns this material's section
                if (material.TeacherCourseSection.TeacherId != teacherId)
                {
                    return Json(new { success = false, message = "You don't have access to delete this material." });
                }

                // Delete physical files
                foreach (var file in material.MaterialFiles.ToList())
                {
                    var path = Server.MapPath(file.FilePath);
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                    db.MaterialFiles.Remove(file);
                }

                db.Materials.Remove(material);
                db.SaveChanges();

                return Json(new { success = true, message = "Material deleted successfully." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return Json(new { success = false, message = "An error occurred while deleting the material." });
            }
        }

        // Search Students
        [HttpGet]
        public JsonResult SearchStudents(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new { success = true, students = new object[0] }, JsonRequestBehavior.AllowGet);
            }

            var students = db.Users
                .Where(s => s.Role == "Student" &&
                           (s.FirstName.Contains(query) ||
                            s.LastName.Contains(query) ||
                            s.Email.Contains(query) ||
                            s.UserID.Contains(query)))
                .Select(s => new
                {
                    id = s.Id,
                    name = s.FirstName + " " + s.LastName,
                    studentId = s.UserID,
                    email = s.Email,
                })
                .ToList();

            return Json(new { success = true, students }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Notification()
        {
            return View();
        }

        public new ActionResult Profile()
        {
            return View();
        }

        // Private helper method to load course tabs
        private ActionResult LoadCourseTab(int? id, string viewName)
        {
            if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
            {
                TempData["ErrorMessage"] = "Session expired. Please log in again.";
                return RedirectToAction("Login", "Home");
            }

            if (!id.HasValue)
            {
                TempData["ErrorMessage"] = "Invalid course ID.";
                return RedirectToAction("Course", "Teacher");
            }

            int teacherId = Convert.ToInt32(Session["Id"]);

            var assignment = db.TeacherCourseSections
                .Include(tcs => tcs.Course)
                .Include(tcs => tcs.Section)
                .Include(tcs => tcs.Section.Program)
                .FirstOrDefault(tcs => tcs.CourseId == id.Value && tcs.TeacherId == teacherId);

            if (assignment == null)
            {
                TempData["ErrorMessage"] = "Course not found or you don't have access to this course.";
                return RedirectToAction("Course", "Teacher");
            }

            var course = assignment.Course;

            // Filter materials to only show those for this specific section
            var sectionMaterials = db.Materials
                .Include(m => m.MaterialFiles)
                .Where(m => m.TeacherCourseSectionId == assignment.Id)
                .OrderByDescending(m => m.UploadedAt)
                .ToList();

            // Pass materials through ViewBag instead of assigning to course
            ViewBag.Materials = sectionMaterials;

            // Common ViewBag data
            ViewBag.SectionName = assignment.Section.Program.ProgramCode + "-" +
                                  assignment.Section.YearLevel +
                                  assignment.Section.SectionName;
            ViewBag.Semester = assignment.Semester;
            ViewBag.TeacherCourseSectionId = assignment.Id;
            ViewBag.CourseId = id.Value;

            // Return the specific tab view
            return View($"~/Views/Teacher/Course/{viewName}.cshtml", course);
        }
    }
}