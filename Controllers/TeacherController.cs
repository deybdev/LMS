using LMS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;

namespace LMS.Controllers
{
    public class TeacherController : Controller
    {
        private readonly LMSContext db = new LMSContext();

        public class StudentDto
        {
            public int id { get; set; }
            public string studentId { get; set; }
            public string name { get; set; }
        }


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
            if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
            {
                TempData["ErrorMessage"] = "Session expired. Please log in again.";
                return RedirectToAction("Login", "Home");
            }

            int teacherId = Convert.ToInt32(Session["Id"]);

            // Get all teacher courses
            var courses = db.Courses
                .Where(c => c.TeacherId == teacherId)
                .ToList();

            // Store student counts in a dictionary
            var studentCounts = db.CourseUsers
                .GroupBy(cu => cu.CourseId)
                .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.StudentCounts = studentCounts;

            return View(courses);
        }


        public ActionResult ViewCourse(int? id)
        {
            if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
            {
                TempData["ErrorMessage"] = "Session expired. Please log in again.";
                return RedirectToAction("Login", "Home");
            }

            int teacherId = Convert.ToInt32(Session["Id"]);

            var course = db.Courses
                .Include(c => c.Materials.Select(m => m.MaterialFiles))
                .FirstOrDefault(c => c.Id == id && c.TeacherId == teacherId);


            return View(course);
        }


        // GET: /Teacher/CreateCourse
        public ActionResult CreateCourse()
        {
            return View();
        }

        // POST: Create Course
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCourse(Course model, string SelectedStudentsJson)
        {
            // Ensure teacher logged in
            if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
            {
                return RedirectToAction("Login", "Home");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the form fields.";
                return View(model);
            }
                int teacherId = Convert.ToInt32(Session["Id"]);

                // Save course first
                model.TeacherId = teacherId;
                model.DateCreated = DateTime.Now;
                db.Courses.Add(model);
                db.SaveChanges();

                if (!string.IsNullOrWhiteSpace(SelectedStudentsJson))
                {
                    var students = Newtonsoft.Json.JsonConvert.DeserializeObject<List<StudentDto>>(SelectedStudentsJson);

                    if (students != null && students.Count > 0)
                    {
                        foreach (var s in students)
                        {
                            // guard against duplicates
                            var exists = db.CourseUsers.FirstOrDefault(cu => cu.CourseId == model.Id && cu.StudentId == s.id);
                            if (exists == null)
                            {
                                var cu = new CourseUser
                                {
                                    CourseId = model.Id,
                                    StudentId = s.id,
                                    DateAdded = DateTime.Now
                                };
                                db.CourseUsers.Add(cu);
                                System.Diagnostics.Debug.WriteLine($"Adding CourseUser CourseId={model.Id} StudentId={s.id}");
                            }
                        }

                        db.SaveChanges();
                    }
                    
                }

                TempData["SuccessMessage"] = "Course created successfully!";
                return RedirectToAction("Course");
            
        }

        // POST: Upload Material
        [HttpPost]
        public ActionResult UploadMaterial(int courseId, string materialTitle, string materialType, string materialDescription, IEnumerable<HttpPostedFileBase> materialFile)
        {
            try
            {

                System.Diagnostics.Debug.WriteLine($"UploadMaterial called - CourseId: {courseId}, Title: {materialTitle}");
                System.Diagnostics.Debug.WriteLine($"File count: {materialFile?.Count() ?? 0}");

                if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
                    return Json(new { success = false, message = "Unauthorized access." });

                if (materialFile == null || !materialFile.Any())
                    return Json(new { success = false, message = "Please select at least one file." });

                var material = new Material
                {
                    CourseId = courseId,
                    Title = materialTitle,
                    Type = materialType,
                    Description = materialDescription,
                    UploadedAt = DateTime.Now
                };
                db.Materials.Add(material);
                db.SaveChanges();

                var uploadFolder = Server.MapPath("~/Uploads/Materials/");
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                foreach (var file in materialFile)
                {
                    if (file != null && file.ContentLength > 0)
                    {
                        var fileName = Path.GetFileName(file.FileName);
                        var path = Path.Combine(uploadFolder, fileName);

                        file.SaveAs(path);

                        db.MaterialFiles.Add(new MaterialFile
                        {
                            MaterialId = material.Id,
                            FileName = fileName,
                            FilePath = "/Uploads/Materials/" + fileName,
                            SizeInMB = Math.Round((decimal)file.ContentLength / 1024 / 1024, 2)
                        });
                    }
                }

                db.SaveChanges();

                return Json(new { success = true, message = "Material uploaded successfully." });
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

                var material = db.Materials.Include(m => m.MaterialFiles)
                    .FirstOrDefault(m => m.Id == id);

                if (material == null)
                {
                    return Json(new { success = false, message = "Material not found." }, JsonRequestBehavior.AllowGet);
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
            string materialDescription, IEnumerable<HttpPostedFileBase> newFiles, string filesToDelete)
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
                material.Description = materialDescription;

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
                            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                            db.MaterialFiles.Remove(file);
                        }
                    }
                }

                // Add new files
                if (newFiles != null && newFiles.Any())
                {
                    var uploadFolder = Server.MapPath("~/Uploads/Materials/");
                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);

                    foreach (var file in newFiles)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            var fileName = Path.GetFileName(file.FileName);
                            var path = Path.Combine(uploadFolder, fileName);
                            file.SaveAs(path);

                            db.MaterialFiles.Add(new MaterialFile
                            {
                                MaterialId = material.Id,
                                FileName = fileName,
                                FilePath = "/Uploads/Materials/" + fileName,
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

                var material = db.Materials.Include(m => m.MaterialFiles).FirstOrDefault(m => m.Id == id);
                if (material == null)
                {
                    return Json(new { success = false, message = "Material not found." });
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

        // ✅ Search Students
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
                    department = s.Department ?? "N/A"
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
    }
}
