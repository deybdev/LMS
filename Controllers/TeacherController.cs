using LMS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Dynamic;
using System.Globalization;
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

        // GET: Teacher/ViewMaterial
        public ActionResult ViewMaterial(int? id, int? materialId)
        {
            try
            {
                // Validate session
                if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
                {
                    TempData["ErrorMessage"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Home");
                }

                if (!id.HasValue || !materialId.HasValue)
                {
                    TempData["ErrorMessage"] = "Invalid course or material ID.";
                    return RedirectToAction("Material", "Teacher", new { id = id });
                }

                int teacherId = Convert.ToInt32(Session["Id"]);

                // Get the course section
                var assignment = db.TeacherCourseSections
                    .Include(tcs => tcs.Course)
                    .Include(tcs => tcs.Section)
                    .Include(tcs => tcs.Section.Program)
                    .FirstOrDefault(tcs => tcs.CourseId == id.Value && tcs.TeacherId == teacherId);

                if (assignment == null)
                {
                    TempData["ErrorMessage"] = "Course section not found or you don't have access.";
                    return RedirectToAction("Material", "Teacher", new { id = id });
                }

                // Get the material with files
                var material = db.Materials
                    .Include(m => m.MaterialFiles)
                    .Include(m => m.TeacherCourseSection)
                    .FirstOrDefault(m => m.Id == materialId.Value && m.TeacherCourseSectionId == assignment.Id);

                if (material == null)
                {
                    TempData["ErrorMessage"] = "Material not found or you don't have access.";
                    return RedirectToAction("Material", "Teacher", new { id = id });
                }

                // Verify teacher owns this material
                if (material.TeacherCourseSection.TeacherId != teacherId)
                {
                    TempData["ErrorMessage"] = "You don't have access to this material.";
                    return RedirectToAction("Material", "Teacher", new { id = id });
                }

                var course = assignment.Course;

                // Set ViewBag data
                ViewBag.Material = material;
                ViewBag.Course = course;
                ViewBag.SectionName = assignment.Section.Program.ProgramCode + "-" +
                                      assignment.Section.YearLevel +
                                      assignment.Section.SectionName;
                ViewBag.CourseId = id.Value;
                ViewBag.ActiveTab = "Material";

                return View("~/Views/Teacher/Course/ViewMaterial.cshtml", course);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ViewMaterial Error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading the material.";
                return RedirectToAction("Material", "Teacher", new { id = id });
            }
        }

        // GET: Teacher/Gradebook
        public ActionResult Gradebook(int? id)
        {
            return LoadCourseTab(id, "Gradebook");
        }

        // GET: Teacher/Classlist
        public ActionResult ClassList(int? id)
        {
            return LoadCourseTab(id, "Classlist");
        }

        public ActionResult Classwork(int? id)
        {
            var result = LoadCourseTab(id, "Classwork");

            if (id.HasValue)
            {
                int teacherId = Convert.ToInt32(Session["Id"]);

                int teacherCourseSectionId = db.TeacherCourseSections
                    .Where(tcs => tcs.CourseId == id.Value && tcs.TeacherId == teacherId)
                    .Select(tcs => tcs.Id)
                    .FirstOrDefault();

                // Get classwork for this section (teachers see all classwork including scheduled)
                var classworks = db.Classworks
                    .Where(c => c.TeacherCourseSectionId == teacherCourseSectionId)
                    .OrderByDescending(c => c.DateCreated)
                    .ToList()
                    .Select(c =>
                    {
                        dynamic item = new ExpandoObject();
                        item.Id = c.Id;
                        item.Title = c.Title;
                        item.ClassworkType = c.ClassworkType;
                        item.Points = c.Points;
                        item.Deadline = c.Deadline;
                        item.SubmittedCount = c.ClassworkSubmissions.Count(s => s.Status == "Submitted" || s.Status == "Graded");
                        item.TotalStudents = c.ClassworkSubmissions.Count();
                        item.IsScheduled = c.IsScheduled;
                        item.ScheduledPublishDate = c.ScheduledPublishDate;
                        item.IsPublished = !c.IsScheduled || (c.ScheduledPublishDate.HasValue && c.ScheduledPublishDate.Value <= DateTime.Now);
                        return item;
                    })
                    .ToList();

                ViewBag.Classworks = classworks;
            }

            return result;
        }

        public ActionResult CreateClasswork (int? id)
        {
            return LoadCourseTab(id, "CreateClasswork");
        }

        public ActionResult Announcement (int? id)
        {
            var result = LoadCourseTab(id, "Announcement");

            if (id.HasValue)
            {
                int teacherId = Convert.ToInt32(Session["Id"]);

                int teacherCourseSectionId = db.TeacherCourseSections
                    .Where(tcs => tcs.CourseId == id.Value && tcs.TeacherId == teacherId)
                    .Select(tcs => tcs.Id)
                    .FirstOrDefault();

                // Get announcements for this section with author information
                var announcements = db.Announcements
                    .Where(a => a.TeacherCourseSectionId == teacherCourseSectionId && a.IsActive)
                    .Include(a => a.Comments.Select(c => c.User))
                    .Include(a => a.CreatedBy)
                    .OrderByDescending(a => a.PostedAt)
                    .ToList()
                    .Select(a =>
                    {
                        dynamic item = new ExpandoObject();
                        item.Id = a.Id;
                        item.Content = a.Content;
                        item.PostedAt = a.PostedAt;
                        item.CommentsCount = a.Comments.Count(c => c.ParentCommentId == null);
                        item.CreatedByUserId = a.CreatedByUserId;
                        item.CreatedByName = a.CreatedBy.FirstName + " " + a.CreatedBy.LastName;
                        item.CreatedByInitials = a.CreatedBy.FirstName.Substring(0, 1) + a.CreatedBy.LastName.Substring(0, 1);
                        item.CreatedByRole = a.CreatedBy.Role;
                        return item;
                    })
                    .ToList();

                ViewBag.Announcements = announcements;
            }

            return result;
        }
        public ActionResult CreateAnnouncement (int? id)
        {
            return LoadCourseTab(id, "CreateAnnouncement");
        }

        // GET: View Announcement Detail
        public ActionResult ViewAnnouncement(int? id, int? announcementId)
        {
            try
            {
                // Validate session
                if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
                {
                    TempData["ErrorMessage"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Home");
                }

                if (!id.HasValue || !announcementId.HasValue)
                {
                    TempData["ErrorMessage"] = "Invalid parameters.";
                    return RedirectToAction("Announcement", "Teacher", new { id = id });
                }

                int teacherId = Convert.ToInt32(Session["Id"]);

                // Get the teacher course section
                var teacherCourseSection = db.TeacherCourseSections
                    .Include(tcs => tcs.Course)
                    .Include(tcs => tcs.Section)
                    .Include(tcs => tcs.Section.Program)
                    .FirstOrDefault(tcs => tcs.CourseId == id.Value && tcs.TeacherId == teacherId);

                if (teacherCourseSection == null)
                {
                    TempData["ErrorMessage"] = "Course section not found or you don't have access.";
                    return RedirectToAction("Announcement", "Teacher", new { id = id });
                }

                // Get the announcement with comments
                var announcement = db.Announcements
                    .Include(a => a.Comments.Select(c => c.User))
                    .Include(a => a.TeacherCourseSection.Teacher)
                    .FirstOrDefault(a => a.Id == announcementId.Value && 
                                       a.TeacherCourseSectionId == teacherCourseSection.Id && 
                                       a.IsActive);

                if (announcement == null)
                {
                    TempData["ErrorMessage"] = "Announcement not found.";
                    return RedirectToAction("Announcement", "Teacher", new { id = id });
                }

                // Prepare announcement data
                dynamic announcementData = new ExpandoObject();
                announcementData.Id = announcement.Id;
                announcementData.Content = announcement.Content;
                announcementData.PostedAt = announcement.PostedAt;
                announcementData.AuthorName = announcement.TeacherCourseSection.Teacher.FirstName + " " + 
                                              announcement.TeacherCourseSection.Teacher.LastName;

                // Get root comments (comments without parent)
                var rootComments = announcement.Comments
                    .Where(c => c.ParentCommentId == null)
                    .OrderBy(c => c.CreatedAt)
                    .ToList();

                // Prepare comments with replies
                var commentsData = rootComments.Select(c =>
                {
                    dynamic comment = new ExpandoObject();
                    comment.Id = c.Id;
                    comment.Comment = c.Comment;
                    comment.CreatedAt = c.CreatedAt;
                    comment.UserId = c.UserId;
                    comment.UserName = c.User.FirstName + " " + c.User.LastName;

                    // Get replies for this comment
                    var replies = announcement.Comments
                        .Where(r => r.ParentCommentId == c.Id)
                        .OrderBy(r => r.CreatedAt)
                        .Select(r =>
                        {
                            dynamic reply = new ExpandoObject();
                            reply.Id = r.Id;
                            reply.Comment = r.Comment;
                            reply.CreatedAt = r.CreatedAt;
                            reply.UserId = r.UserId;
                            reply.UserName = r.User.FirstName + " " + r.User.LastName;
                            return reply;
                        })
                        .ToList();

                    comment.Replies = replies;
                    return comment;
                }).ToList();

                ViewBag.Announcement = announcementData;
                ViewBag.Comments = commentsData;
                ViewBag.CourseId = id.Value;
                ViewBag.SectionName = teacherCourseSection.Section.Program.ProgramCode + "-" +
                                     teacherCourseSection.Section.YearLevel +
                                     teacherCourseSection.Section.SectionName;
                ViewBag.TeacherCourseSectionId = teacherCourseSection.Id;

                return View("~/Views/Teacher/Course/ViewAnnouncement.cshtml", teacherCourseSection.Course);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ViewAnnouncement Error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading the announcement.";
                return RedirectToAction("Announcement", "Teacher", new { id = id });
            }
        }

        // GET: Edit Announcement
        public ActionResult EditAnnouncement(int? id, int? announcementId)
        {
            try
            {
                // Validate session
                if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
                {
                    TempData["ErrorMessage"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Home");
                }

                if (!id.HasValue || !announcementId.HasValue)
                {
                    TempData["ErrorMessage"] = "Invalid course or announcement ID.";
                    return RedirectToAction("Announcement", "Teacher", new { id = id });
                }

                int teacherId = Convert.ToInt32(Session["Id"]);

                // Verify the teacher owns this course section
                var teacherCourseSection = db.TeacherCourseSections
                    .FirstOrDefault(tcs => tcs.CourseId == id.Value && tcs.TeacherId == teacherId);

                if (teacherCourseSection == null)
                {
                    TempData["ErrorMessage"] = "Course section not found or you don't have access.";
                    return RedirectToAction("Announcement", "Teacher", new { id = id });
                }

                // Get the announcement
                var announcement = db.Announcements
                    .FirstOrDefault(a => a.Id == announcementId.Value && 
                                       a.TeacherCourseSectionId == teacherCourseSection.Id &&
                                       a.IsActive);

                if (announcement == null)
                {
                    TempData["ErrorMessage"] = "Announcement not found or you don't have access.";
                    return RedirectToAction("Announcement", "Teacher", new { id = id });
                }

                // Load course tab
                var result = LoadCourseTab(id, "EditAnnouncement");

                // Set ViewBag properties for the form
                ViewBag.AnnouncementId = announcement.Id;
                ViewBag.AnnouncementContent = announcement.Content;

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EditAnnouncement GET Error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading the announcement: " + ex.Message;
                return RedirectToAction("Announcement", "Teacher", new { id = id });
            }
        }

        // GET: Get Announcement (for AJAX)
        [HttpGet]
        public ActionResult GetAnnouncement(int id)
        {
            try
            {
                if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
                {
                    return Json(new { success = false, message = "Session expired." }, JsonRequestBehavior.AllowGet);
                }

                int teacherId = Convert.ToInt32(Session["Id"]);

                var announcement = db.Announcements
                    .Include(a => a.TeacherCourseSection)
                    .FirstOrDefault(a => a.Id == id && a.IsActive);

                if (announcement == null)
                {
                    return Json(new { success = false, message = "Announcement not found." }, JsonRequestBehavior.AllowGet);
                }

                // Verify the teacher owns this announcement
                if (announcement.TeacherCourseSection.TeacherId != teacherId)
                {
                    return Json(new { success = false, message = "You don't have access to this announcement." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = announcement.Id,
                        content = announcement.Content
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAnnouncement Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred." }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Create Announcement
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)] // Allow HTML content
        public ActionResult CreateAnnouncement(int TeacherCourseId, int TeacherCourseSectionId, string content)
        {
            try
            {
                // Validate session
                if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                int teacherId = Convert.ToInt32(Session["Id"]);

                // Verify the teacher owns this course section
                var teacherCourseSection = db.TeacherCourseSections
                    .FirstOrDefault(tcs => tcs.Id == TeacherCourseSectionId && tcs.TeacherId == teacherId);

                if (teacherCourseSection == null)
                {
                    return Json(new { success = false, message = "Course section not found or you don't have access." });
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    return Json(new { success = false, message = "Content is required." });
                }

                // Create the announcement
                var announcement = new Announcement
                {
                    TeacherCourseSectionId = TeacherCourseSectionId,
                    CreatedByUserId = teacherId,
                    Content = content, // HTML content from rich text editor
                    PostedAt = DateTime.Now,
                    IsActive = true
                };

                db.Announcements.Add(announcement);
                db.SaveChanges();

                return Json(new { success = true, message = "Announcement posted successfully!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateAnnouncement Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while posting the announcement: " + ex.Message });
            }
        }

        // POST: Update Announcement
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)] // Allow HTML content
        public ActionResult UpdateAnnouncement(int AnnouncementId, int TeacherCourseId, int TeacherCourseSectionId, string content)
        {
            try
            {
                // Validate session
                if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                int teacherId = Convert.ToInt32(Session["Id"]);

                // Verify the teacher owns this course section
                var teacherCourseSection = db.TeacherCourseSections
                    .FirstOrDefault(tcs => tcs.Id == TeacherCourseSectionId && tcs.TeacherId == teacherId);

                if (teacherCourseSection == null)
                {
                    return Json(new { success = false, message = "Course section not found or you don't have access." });
                }

                // Get the existing announcement
                var announcement = db.Announcements
                    .FirstOrDefault(a => a.Id == AnnouncementId && 
                                       a.TeacherCourseSectionId == TeacherCourseSectionId &&
                                       a.IsActive);

                if (announcement == null)
                {
                    return Json(new { success = false, message = "Announcement not found or you don't have access." });
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    return Json(new { success = false, message = "Content is required." });
                }

                // Update the announcement
                announcement.Content = content;
                announcement.PostedAt = DateTime.Now; // Update timestamp to reflect edit

                db.SaveChanges();

                return Json(new { success = true, message = "Announcement updated successfully!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateAnnouncement Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while updating the announcement: " + ex.Message });
            }
        }


        // GET: Teacher/GetEnrolledStudents
        [HttpGet]
        public JsonResult GetEnrolledStudents(int teacherCourseSectionId)
        {
            try
            {
                if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
                {
                    return Json(new { success = false, message = "Unauthorized access" }, JsonRequestBehavior.AllowGet);
                }

                int teacherId = Convert.ToInt32(Session["Id"]);

                // Verify the teacher owns this course section
                var teacherCourseSection = db.TeacherCourseSections
                    .FirstOrDefault(tcs => tcs.Id == teacherCourseSectionId && tcs.TeacherId == teacherId);

                if (teacherCourseSection == null)
                {
                    return Json(new { success = false, message = "You don't have access to this course section" }, JsonRequestBehavior.AllowGet);
                }

                // Get enrolled students
                var students = db.StudentCourses
                    .Where(sc => sc.CourseId == teacherCourseSection.CourseId && 
                                 sc.SectionId == teacherCourseSection.SectionId)
                    .Include(sc => sc.Student)
                    .OrderBy(sc => sc.Student.LastName)
                    .ThenBy(sc => sc.Student.FirstName)
                    .Select(sc => new
                    {
                        id = sc.Student.Id,
                        studentId = sc.Student.UserID,
                        name = sc.Student.FirstName + " " + sc.Student.LastName,
                        email = sc.Student.Email,
                        grade = "-", // You can add actual grade logic later
                        status = sc.Status ?? "Active"
                    })
                    .ToList();

                return Json(new { success = true, students }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetEnrolledStudents Error: {ex.Message}");
                return Json(new { success = false, message = "Error loading students: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetAttendance(int teacherCourseSectionId, string date)
        {
            try
            {
                if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
                {
                    return Json(new { success = false, message = "Unauthorized access" }, JsonRequestBehavior.AllowGet);
                }

                int teacherId = Convert.ToInt32(Session["Id"]);

                var teacherCourseSection = db.TeacherCourseSections
                    .FirstOrDefault(tcs => tcs.Id == teacherCourseSectionId && tcs.TeacherId == teacherId);

                if (teacherCourseSection == null)
                {
                    return Json(new { success = false, message = "You don't have access to this course section" }, JsonRequestBehavior.AllowGet);
                }

                var attendanceDate = ParseAttendanceDate(date);

                var students = db.StudentCourses
                    .Where(sc => sc.CourseId == teacherCourseSection.CourseId &&
                                 sc.SectionId == teacherCourseSection.SectionId)
                    .Include(sc => sc.Student)
                    .OrderBy(sc => sc.Student.LastName)
                    .ThenBy(sc => sc.Student.FirstName)
                    .Select(sc => new
                    {
                        id = sc.Student.Id,
                        studentId = sc.Student.UserID,
                        name = sc.Student.FirstName + " " + sc.Student.LastName,
                        email = sc.Student.Email
                    })
                    .ToList();

                var attendanceRecords = db.AttendanceRecords
                    .Where(ar => ar.TeacherCourseSectionId == teacherCourseSection.Id &&
                                 DbFunctions.TruncateTime(ar.AttendanceDate) == attendanceDate)
                    .ToList();

                var responseStudents = students.Select(s =>
                {
                    var record = attendanceRecords.FirstOrDefault(ar => ar.StudentId == s.id);
                    return new
                    {
                        s.id,
                        s.studentId,
                        s.name,
                        s.email,
                        status = record?.Status ?? "Unmarked",
                        lateMinutes = record?.LateMinutes,
                        markedAt = record?.MarkedAt
                    };
                }).ToList();

                return Json(new { success = true, students = responseStudents }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAttendance Error: {ex.Message}");
                return Json(new { success = false, message = "Error loading attendance: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult SaveAttendance(SaveAttendanceRequest request)
        {
            try
            {
                if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
                {
                    return Json(new { success = false, message = "Unauthorized access" });
                }

                if (request == null || request.TeacherCourseSectionId <= 0)
                {
                    return Json(new { success = false, message = "Invalid attendance payload." });
                }

                int teacherId = Convert.ToInt32(Session["Id"]);

                var teacherCourseSection = db.TeacherCourseSections
                    .FirstOrDefault(tcs => tcs.Id == request.TeacherCourseSectionId && tcs.TeacherId == teacherId);

                if (teacherCourseSection == null)
                {
                    return Json(new { success = false, message = "You don't have access to this course section" });
                }

                var attendanceDate = ParseAttendanceDate(request.AttendanceDate);

                var enrolledStudentIds = db.StudentCourses
                    .Where(sc => sc.CourseId == teacherCourseSection.CourseId &&
                                 sc.SectionId == teacherCourseSection.SectionId)
                    .Select(sc => sc.StudentId)
                    .ToList();

                if (!enrolledStudentIds.Any())
                {
                    return Json(new { success = false, message = "There are no students enrolled in this section." });
                }

                var incomingRecords = (request.Records ?? new List<AttendanceStudentRecord>())
                    .Where(r => enrolledStudentIds.Contains(r.StudentId))
                    .GroupBy(r => r.StudentId)
                    .Select(g => g.Last())
                    .ToList();

                if (!incomingRecords.Any())
                {
                    return Json(new { success = false, message = "No valid attendance records were provided." });
                }

                var existingRecords = db.AttendanceRecords
                    .Where(ar => ar.TeacherCourseSectionId == teacherCourseSection.Id &&
                                 DbFunctions.TruncateTime(ar.AttendanceDate) == attendanceDate)
                    .ToList();

                foreach (var record in incomingRecords)
                {
                    var normalizedStatus = NormalizeAttendanceStatus(record.Status);
                    var existing = existingRecords.FirstOrDefault(ar => ar.StudentId == record.StudentId);

                    if (string.IsNullOrEmpty(normalizedStatus))
                    {
                        if (existing != null)
                        {
                            db.AttendanceRecords.Remove(existing);
                        }
                        continue;
                    }

                    if (existing == null)
                    {
                        db.AttendanceRecords.Add(new AttendanceRecord
                        {
                            TeacherCourseSectionId = teacherCourseSection.Id,
                            StudentId = record.StudentId,
                            AttendanceDate = attendanceDate,
                            Status = normalizedStatus,
                            LateMinutes = normalizedStatus == "Late" ? record.LateMinutes : null,
                            MarkedAt = DateTime.Now
                        });
                    }
                    else
                    {
                        existing.Status = normalizedStatus;
                        existing.LateMinutes = normalizedStatus == "Late" ? record.LateMinutes : null;
                        existing.MarkedAt = DateTime.Now;
                    }
                }

                db.SaveChanges();

                var presentCount = incomingRecords.Count(r => NormalizeAttendanceStatus(r.Status) == "Present");
                var absentCount = incomingRecords.Count(r => NormalizeAttendanceStatus(r.Status) == "Absent");
                var lateCount = incomingRecords.Count(r => NormalizeAttendanceStatus(r.Status) == "Late");

                return Json(new
                {
                    success = true,
                    message = "Attendance saved successfully.",
                    presentCount,
                    absentCount,
                    lateCount
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveAttendance Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while saving attendance: " + ex.Message });
            }
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
                System.Diagnostics.Debug.WriteLine($"DeleteMaterial Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while deleting the material: " + ex.Message });
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

        // POST: Create Classwork
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateClasswork(int TeacherCourseId, string title, string classworkType, 
            DateTime? deadline, int? points, string description, HttpPostedFileBase[] files, 
            string questionsJson, string submissionMode, string publishMode, DateTime? scheduledPublishDate, 
            bool noDueDate = false)
        {
            try
            {
                // Validate session
                if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
                {
                    TempData["ErrorMessage"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Home");
                }

                int teacherId = Convert.ToInt32(Session["Id"]);

                // Find the TeacherCourseSection record
                var teacherCourseSection = db.TeacherCourseSections
                    .FirstOrDefault(tcs => tcs.CourseId == TeacherCourseId && tcs.TeacherId == teacherId);

                if (teacherCourseSection == null)
                {
                    TempData["ErrorMessage"] = "Course section not found or you don't have access.";
                    return RedirectToAction("Classwork", "Teacher", new { id = TeacherCourseId });
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(title))
                {
                    TempData["ErrorMessage"] = "Title is required.";
                    return RedirectToAction("CreateClasswork", "Teacher", new { id = TeacherCourseId });
                }

                if (string.IsNullOrWhiteSpace(classworkType))
                {
                    TempData["ErrorMessage"] = "Classwork type is required.";
                    return RedirectToAction("CreateClasswork", "Teacher", new { id = TeacherCourseId });
                }

                if (string.IsNullOrWhiteSpace(submissionMode))
                {
                    TempData["ErrorMessage"] = "Submission mode is required.";
                    return RedirectToAction("CreateClasswork", "Teacher", new { id = TeacherCourseId });
                }

                // Validate publish mode and scheduled date
                bool isScheduled = false;
                DateTime? finalScheduledDate = null;
                
                if (publishMode == "scheduled")
                {
                    if (!scheduledPublishDate.HasValue)
                    {
                        TempData["ErrorMessage"] = "Scheduled publish date is required when scheduling classwork.";
                        return RedirectToAction("CreateClasswork", "Teacher", new { id = TeacherCourseId });
                    }

                    if (scheduledPublishDate.Value <= DateTime.Now)
                    {
                        TempData["ErrorMessage"] = "Scheduled publish date must be in the future.";
                        return RedirectToAction("CreateClasswork", "Teacher", new { id = TeacherCourseId });
                    }

                    isScheduled = true;
                    finalScheduledDate = scheduledPublishDate.Value;
                }

                // Handle deadline - if noDueDate is checked, set to null
                DateTime? finalDeadline = null;
                if (!noDueDate && deadline.HasValue)
                {
                    if (deadline.Value < DateTime.Now)
                    {
                        TempData["ErrorMessage"] = "Deadline must be in the future.";
                        return RedirectToAction("CreateClasswork", "Teacher", new { id = TeacherCourseId });
                    }
                    finalDeadline = deadline.Value;
                }

                int finalPoints = 0;

                // Validate based on submission mode
                if (submissionMode == "manual")
                {
                    // For manual mode, validate questions
                    if (string.IsNullOrWhiteSpace(questionsJson) || questionsJson == "[]")
                    {
                        TempData["ErrorMessage"] = "At least one question is required for manual submission mode.";
                        return RedirectToAction("CreateClasswork", "Teacher", new { id = TeacherCourseId });
                    }

                    // Calculate points from questions JSON
                    try
                    {
                        var questionsArray = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(questionsJson);
                        foreach (var question in questionsArray)
                        {
                            finalPoints += (int)question.points;
                        }
                    }
                    catch
                    {
                        finalPoints = points ?? 0;
                    }
                }
                else if (submissionMode == "file")
                {
                    // For file mode, validate points
                    if (!points.HasValue || points.Value <= 0)
                    {
                        TempData["ErrorMessage"] = "Points must be greater than zero for file submission mode.";
                        return RedirectToAction("CreateClasswork", "Teacher", new { id = TeacherCourseId });
                    }
                    finalPoints = points.Value;
                    questionsJson = null; // Clear questions for file mode
                }

                // Create the classwork record
                var classwork = new Classwork
                {
                    TeacherCourseSectionId = teacherCourseSection.Id,
                    Title = title,
                    ClassworkType = classworkType,
                    Description = description ?? string.Empty,
                    Deadline = finalDeadline,
                    Points = finalPoints,
                    QuestionsJson = questionsJson, // Store questions JSON for manual mode only
                    DateCreated = DateTime.Now,
                    IsActive = true,
                    IsScheduled = isScheduled,
                    ScheduledPublishDate = finalScheduledDate
                };

                db.Classworks.Add(classwork);
                db.SaveChanges();

                // Handle file uploads if any (instructional materials)
                if (files != null && files.Length > 0 && files[0] != null)
                {
                    var uploadFolder = Server.MapPath("~/Uploads/Classwork/");
                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);

                    foreach (var file in files)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            var fileName = Path.GetFileName(file.FileName);
                            var uniqueFileName = $"{DateTime.Now.Ticks}_{fileName}";
                            var filePath = Path.Combine(uploadFolder, uniqueFileName);

                            // Save the physical file
                            file.SaveAs(filePath);

                            // Create database record for the file
                            var fileRecord = new ClassworkFile
                            {
                                ClassworkId = classwork.Id,
                                FileName = fileName,
                                FilePath = "/Uploads/Classwork/" + uniqueFileName,
                                SizeInMB = Math.Round((decimal)file.ContentLength / 1024 / 1024, 2),
                                UploadedAt = DateTime.Now
                            };

                            db.ClassworkFiles.Add(fileRecord);
                        }
                    }

                    db.SaveChanges();
                }

                // Create submission records for all enrolled students
                var enrolledStudents = db.StudentCourses
                    .Where(sc => sc.CourseId == teacherCourseSection.CourseId && 
                                 sc.SectionId == teacherCourseSection.SectionId)
                    .Select(sc => sc.StudentId)
                    .ToList();

                foreach (var studentId in enrolledStudents)
                {
                    var submission = new ClassworkSubmission
                    {
                        ClassworkId = classwork.Id,
                        StudentId = studentId,
                        Status = "Not Submitted"
                    };
                    db.ClassworkSubmissions.Add(submission);
                }

                db.SaveChanges();

                string modeText = submissionMode == "manual" ? "with questions" : "with file submission";
                string scheduleText = isScheduled ? $" and scheduled for {finalScheduledDate.Value:MMM dd, yyyy h:mm tt}" : "";
                TempData["SuccessMessage"] = $"{classworkType} '{title}' created successfully {modeText}{scheduleText} for {enrolledStudents.Count} students!";
                return RedirectToAction("Classwork", "Teacher", new { id = TeacherCourseId });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateClasswork Error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while creating the classwork: " + ex.Message;
                return RedirectToAction("CreateClasswork", "Teacher", new { id = TeacherCourseId });
            }
        }

        // GET: Edit Classwork
        public ActionResult EditClasswork(int? id, int? classworkId)
        {
            try
            {
                // Validate session
                if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
                {
                    TempData["ErrorMessage"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Home");
                }

                if (!id.HasValue || !classworkId.HasValue)
                {
                    TempData["ErrorMessage"] = "Invalid course or classwork ID.";
                    return RedirectToAction("Classwork", "Teacher", new { id = id });
                }

                int teacherId = Convert.ToInt32(Session["Id"]);

                // Verify the teacher owns this course section
                var teacherCourseSection = db.TeacherCourseSections
                    .FirstOrDefault(tcs => tcs.CourseId == id.Value && tcs.TeacherId == teacherId);

                if (teacherCourseSection == null)
                {
                    TempData["ErrorMessage"] = "Course section not found or you don't have access.";
                    return RedirectToAction("Classwork", "Teacher", new { id = id });
                }

                // Get the classwork
                var classwork = db.Classworks
                    .Include(c => c.ClassworkFiles)
                    .FirstOrDefault(c => c.Id == classworkId.Value && c.TeacherCourseSectionId == teacherCourseSection.Id);

                if (classwork == null)
                {
                    TempData["ErrorMessage"] = "Classwork not found or you don't have access.";
                    return RedirectToAction("Classwork", "Teacher", new { id = id });
                }

                // Load course tab
                var result = LoadCourseTab(id, "EditClasswork");

                // Set ViewBag properties for the form
                ViewBag.ClassworkId = classwork.Id;
                ViewBag.ClassworkTitle = classwork.Title;
                ViewBag.ClassworkType = classwork.ClassworkType;
                ViewBag.ClassworkDescription = classwork.Description;
                ViewBag.ClassworkPoints = classwork.Points;
                ViewBag.ClassworkQuestionsJson = classwork.QuestionsJson ?? "";
                
                // Scheduling properties
                ViewBag.IsScheduled = classwork.IsScheduled;
                if (classwork.IsScheduled && classwork.ScheduledPublishDate.HasValue)
                {
                    ViewBag.ScheduledPublishDate = classwork.ScheduledPublishDate.Value.ToString("yyyy-MM-ddTHH:mm");
                }
                else
                {
                    ViewBag.ScheduledPublishDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm");
                }
                
                // Check if deadline is null (no due date)
                bool hasNoDueDate = !classwork.Deadline.HasValue;
                ViewBag.HasNoDueDate = hasNoDueDate;
                
                // Format deadline for HTML5 datetime-local input (yyyy-MM-ddTHH:mm)
                // If no due date, set to empty or current date
                if (hasNoDueDate)
                {
                    ViewBag.ClassworkDeadline = DateTime.Now.ToString("yyyy-MM-ddTHH:mm");
                }
                else
                {
                    ViewBag.ClassworkDeadline = classwork.Deadline.Value.ToString("yyyy-MM-ddTHH:mm");
                }

                // Get existing files
                var existingFiles = classwork.ClassworkFiles
                    .Select(f => new
                    {
                        f.Id,
                        f.FileName,
                        f.FilePath,
                        f.SizeInMB
                    })
                    .ToList()
                    .Select(f =>
                    {
                        dynamic item = new ExpandoObject();
                        item.Id = f.Id;
                        item.FileName = f.FileName;
                        item.FilePath = f.FilePath;
                        item.SizeInMB = f.SizeInMB;
                        return item;
                    })
                    .ToList();

                ViewBag.ExistingFiles = existingFiles;

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EditClasswork GET Error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading the classwork: " + ex.Message;
                return RedirectToAction("Classwork", "Teacher", new { id = id });
            }
        }

        // POST: Delete Classwork
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteClasswork(int id)
        {
            try
            {
                if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                int teacherId = Convert.ToInt32(Session["Id"]);

                var classwork = db.Classworks
                    .Include(c => c.ClassworkFiles)
                    .Include(c => c.ClassworkSubmissions.Select(s => s.SubmissionFiles))
                    .Include(c => c.TeacherCourseSection)
                    .FirstOrDefault(c => c.Id == id);

                if (classwork == null)
                {
                    return Json(new { success = false, message = "Classwork not found." });
                }

                // Verify the teacher owns this classwork's section
                if (classwork.TeacherCourseSection.TeacherId != teacherId)
                {
                    return Json(new { success = false, message = "You don't have access to delete this classwork." });
                }

                // Delete physical classwork files
                foreach (var file in classwork.ClassworkFiles.ToList())
                {
                    var path = Server.MapPath(file.FilePath);
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                }

                // Delete physical submission files
                foreach (var submission in classwork.ClassworkSubmissions.ToList())
                {
                    foreach (var submissionFile in submission.SubmissionFiles.ToList())
                    {
                        var path = Server.MapPath(submissionFile.FilePath);
                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.Delete(path);
                        }
                    }
                }

                // Remove classwork (cascade delete will handle related records)
                db.Classworks.Remove(classwork);
                db.SaveChanges();

                return Json(new { success = true, message = "Classwork deleted successfully." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteClasswork Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while deleting the classwork: " + ex.Message });
            }
        }

        // POST: Delete Activity (alias for DeleteClasswork)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteActivity(int id)
        {
            return DeleteClasswork(id);
        }

        // POST: Edit Classwork
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditClasswork(int ClassworkId, int TeacherCourseId, string title, string classworkType,
            DateTime? deadline, int? points, string description, HttpPostedFileBase[] files, string filesToDelete,
            string questionsJson, string submissionMode, string publishMode, DateTime? scheduledPublishDate, 
            bool noDueDate = false)
        {
            try
            {
                // Validate session
                if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
                {
                    TempData["ErrorMessage"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Home");
                }

                int teacherId = Convert.ToInt32(Session["Id"]);

                // Find the TeacherCourseSection record
                var teacherCourseSection = db.TeacherCourseSections
                    .FirstOrDefault(tcs => tcs.CourseId == TeacherCourseId && tcs.TeacherId == teacherId);

                if (teacherCourseSection == null)
                {
                    TempData["ErrorMessage"] = "Course section not found or you don't have access.";
                    return RedirectToAction("Classwork", "Teacher", new { id = TeacherCourseId });
                }

                // Get the existing classwork
                var classwork = db.Classworks
                    .Include(c => c.ClassworkFiles)
                    .FirstOrDefault(c => c.Id == ClassworkId && c.TeacherCourseSectionId == teacherCourseSection.Id);

                if (classwork == null)
                {
                    TempData["ErrorMessage"] = "Classwork not found or you don't have access.";
                    return RedirectToAction("Classwork", "Teacher", new { id = TeacherCourseId });
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(title))
                {
                    TempData["ErrorMessage"] = "Title is required.";
                    return RedirectToAction("EditClasswork", "Teacher", new { id = TeacherCourseId, classworkId = ClassworkId });
                }

                if (string.IsNullOrWhiteSpace(classworkType))
                {
                    TempData["ErrorMessage"] = "Classwork type is required.";
                    return RedirectToAction("EditClasswork", "Teacher", new { id = TeacherCourseId, classworkId = ClassworkId });
                }

                if (string.IsNullOrWhiteSpace(submissionMode))
                {
                    TempData["ErrorMessage"] = "Submission mode is required.";
                    return RedirectToAction("EditClasswork", "Teacher", new { id = TeacherCourseId, classworkId = ClassworkId });
                }

                // Validate publish mode and scheduled date
                bool isScheduled = false;
                DateTime? finalScheduledDate = null;
                
                if (publishMode == "scheduled")
                {
                    if (!scheduledPublishDate.HasValue)
                    {
                        TempData["ErrorMessage"] = "Scheduled publish date is required when scheduling classwork.";
                        return RedirectToAction("EditClasswork", "Teacher", new { id = TeacherCourseId, classworkId = ClassworkId });
                    }

                    if (scheduledPublishDate.Value <= DateTime.Now)
                    {
                        TempData["ErrorMessage"] = "Scheduled publish date must be in the future.";
                        return RedirectToAction("EditClasswork", "Teacher", new { id = TeacherCourseId, classworkId = ClassworkId });
                    }

                    isScheduled = true;
                    finalScheduledDate = scheduledPublishDate.Value;
                }

                // Handle deadline - if noDueDate is checked, set to null
                DateTime? finalDeadline = null;
                if (!noDueDate && deadline.HasValue)
                {
                    finalDeadline = deadline.Value;
                }

                int finalPoints = 0;

                // Validate based on submission mode
                if (submissionMode == "manual")
                {
                    // For manual mode, validate questions
                    if (string.IsNullOrWhiteSpace(questionsJson) || questionsJson == "[]")
                    {
                        TempData["ErrorMessage"] = "At least one question is required for manual submission mode.";
                        return RedirectToAction("EditClasswork", "Teacher", new { id = TeacherCourseId, classworkId = ClassworkId });
                    }

                    // Calculate points from questions JSON
                    try
                    {
                        var questionsArray = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(questionsJson);
                        foreach (var question in questionsArray)
                        {
                            finalPoints += (int)question.points;
                        }
                    }
                    catch
                    {
                        finalPoints = points ?? 0;
                    }
                }
                else if (submissionMode == "file")
                {
                    // For file mode, validate points
                    if (!points.HasValue || points.Value <= 0)
                    {
                        TempData["ErrorMessage"] = "Points must be greater than zero for file submission mode.";
                        return RedirectToAction("EditClasswork", "Teacher", new { id = TeacherCourseId, classworkId = ClassworkId });
                    }
                    finalPoints = points.Value;
                    questionsJson = null; // Clear questions for file mode
                }

                // Update the classwork record
                classwork.Title = title;
                classwork.ClassworkType = classworkType;
                classwork.Description = description ?? string.Empty;
                classwork.Deadline = finalDeadline;
                classwork.Points = finalPoints;
                classwork.QuestionsJson = questionsJson; // Update questions
                classwork.IsScheduled = isScheduled;
                classwork.ScheduledPublishDate = finalScheduledDate;

                // Delete files if requested
                if (!string.IsNullOrEmpty(filesToDelete))
                {
                    var fileIdsToDelete = filesToDelete.Split(',').Select(int.Parse).ToList();
                    foreach (var fileId in fileIdsToDelete)
                    {
                        var fileToDelete = db.ClassworkFiles.FirstOrDefault(f => f.Id == fileId && f.ClassworkId == ClassworkId);
                        if (fileToDelete != null)
                        {
                            // Delete physical file
                            var filePath = Server.MapPath(fileToDelete.FilePath);
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                            // Delete database record
                            db.ClassworkFiles.Remove(fileToDelete);
                        }
                    }
                }

                // Handle new file uploads if any
                if (files != null && files.Length > 0 && files[0] != null)
                {
                    var uploadFolder = Server.MapPath("~/Uploads/Classwork/");
                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);

                    foreach (var file in files)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            var fileName = Path.GetFileName(file.FileName);
                            var uniqueFileName = $"{DateTime.Now.Ticks}_{fileName}";
                            var filePath = Path.Combine(uploadFolder, uniqueFileName);
                            file.SaveAs(filePath);

                            // Create database record for the file
                            var fileRecord = new ClassworkFile
                            {
                                ClassworkId = classwork.Id,
                                FileName = fileName,
                                FilePath = "/Uploads/Classwork/" + uniqueFileName,
                                SizeInMB = Math.Round((decimal)file.ContentLength / 1024 / 1024, 2),
                                UploadedAt = DateTime.Now
                            };

                            db.ClassworkFiles.Add(fileRecord);
                        }
                    }
                }

                db.SaveChanges();

                string modeText = submissionMode == "manual" ? "with questions" : "with file submission";
                string scheduleText = isScheduled ? $" and scheduled for {finalScheduledDate.Value:MMM dd, yyyy h:mm tt}" : "";
                TempData["SuccessMessage"] = $"Classwork '{title}' updated successfully {modeText}{scheduleText}!";
                return RedirectToAction("Classwork", "Teacher", new { id = TeacherCourseId });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EditClasswork POST Error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while updating the classwork: " + ex.Message;
                return RedirectToAction("EditClasswork", "Teacher", new { id = TeacherCourseId, classworkId = ClassworkId });
            }
        }

        // GET: Get Classwork List for a Course
        [HttpGet]
        public JsonResult GetClassworkList(int teacherCourseSectionId)
        {
            try
            {
                if (Session["Id"] == null || (string)Session["Role"] != "Teacher")
                {
                    return Json(new { success = false, message = "Unauthorized access" }, JsonRequestBehavior.AllowGet);
                }

                int teacherId = Convert.ToInt32(Session["Id"]);

                // Verify the teacher owns this course section
                var teacherCourseSection = db.TeacherCourseSections
                    .FirstOrDefault(tcs => tcs.Id == teacherCourseSectionId && tcs.TeacherId == teacherId);

                if (teacherCourseSection == null)
                {
                    return Json(new { success = false, message = "You don't have access to this course section" }, JsonRequestBehavior.AllowGet);
                }

                // Get all classwork for this section
                var classworks = db.Classworks
                    .Where(c => c.TeacherCourseSectionId == teacherCourseSectionId)
                    .OrderByDescending(c => c.DateCreated)
                    .Select(c => new
                    {
                        id = c.Id,
                        title = c.Title,
                        type = c.ClassworkType,
                        deadline = c.Deadline,
                        points = c.Points,
                        isActive = c.IsActive,
                        submittedCount = c.ClassworkSubmissions.Count(s => s.Status == "Submitted" || s.Status == "Graded"),
                        totalStudents = c.ClassworkSubmissions.Count()
                    })
                    .ToList();

                return Json(new { success = true, classworks }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetClassworkList Error: {ex.Message}");
                return Json(new { success = false, message = "Error loading classwork: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Delete Announcement
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAnnouncement(int id)
        {
            try
            {
                if (Session["Id"] == null)
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                int userId = Convert.ToInt32(Session["Id"]);
                string userRole = Session["Role"] as string;

                var announcement = db.Announcements
                    .Include(a => a.TeacherCourseSection)
                    .Include(a => a.Comments)
                    .FirstOrDefault(a => a.Id == id);

                if (announcement == null)
                {
                    return Json(new { success = false, message = "Announcement not found." });
                }

                // Teachers can delete any announcement in their course
                // Students can only delete their own announcements
                bool canDelete = false;
                
                if (userRole == "Teacher")
                {
                    // Teacher can delete any announcement in their course section
                    canDelete = announcement.TeacherCourseSection.TeacherId == userId;
                }
                else if (userRole == "Student")
                {
                    // Student can only delete their own announcement
                    canDelete = announcement.CreatedByUserId == userId;
                }

                if (!canDelete)
                {
                    return Json(new { success = false, message = "You don't have access to delete this announcement." });
                }

                // Soft delete - just mark as inactive
                announcement.IsActive = false;
                db.SaveChanges();

                return Json(new { success = true, message = "Announcement deleted successfully." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteAnnouncement Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while deleting the announcement: " + ex.Message });
            }
        }

        // POST: Add Comment to Announcement
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddComment(int announcementId, string comment)
        {
            try
            {
                if (Session["Id"] == null)
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                int userId = Convert.ToInt32(Session["Id"]);

                // Verify announcement exists
                var announcement = db.Announcements.Find(announcementId);
                if (announcement == null || !announcement.IsActive)
                {
                    return Json(new { success = false, message = "Announcement not found." });
                }

                // Validate comment
                if (string.IsNullOrWhiteSpace(comment))
                {
                    return Json(new { success = false, message = "Comment cannot be empty." });
                }

                // Add comment
                var newComment = new AnnouncementComment
                {
                    AnnouncementId = announcementId,
                    UserId = userId,
                    Comment = comment.Trim(),
                    ParentCommentId = null,
                    CreatedAt = DateTime.Now
                };

                db.AnnouncementComments.Add(newComment);
                db.SaveChanges();

                return Json(new { success = true, message = "Comment added successfully." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddComment Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while adding the comment." });
            }
        }

        // POST: Add Reply to Comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddReply(int announcementId, int parentCommentId, string comment)
        {
            try
            {
                if (Session["Id"] == null)
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                int userId = Convert.ToInt32(Session["Id"]);

                // Verify parent comment exists
                var parentComment = db.AnnouncementComments
                    .Include(c => c.Announcement)
                    .FirstOrDefault(c => c.Id == parentCommentId);

                if (parentComment == null)
                {
                    return Json(new { success = false, message = "Parent comment not found." });
                }

                // Verify announcement
                if (parentComment.AnnouncementId != announcementId || !parentComment.Announcement.IsActive)
                {
                    return Json(new { success = false, message = "Invalid announcement." });
                }

                // Validate comment
                if (string.IsNullOrWhiteSpace(comment))
                {
                    return Json(new { success = false, message = "Reply cannot be empty." });
                }

                // Add reply
                var newReply = new AnnouncementComment
                {
                    AnnouncementId = announcementId,
                    UserId = userId,
                    Comment = comment.Trim(),
                    ParentCommentId = parentCommentId,
                    CreatedAt = DateTime.Now
                };

                db.AnnouncementComments.Add(newReply);
                db.SaveChanges();

                return Json(new { success = true, message = "Reply added successfully." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddReply Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while adding the reply." });
            }
        }

        private string NormalizeAttendanceStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return null;
            }

            var normalized = status.Trim().ToLowerInvariant();

            if (normalized == "present") return "Present";
            if (normalized == "absent") return "Absent";
            if (normalized == "late") return "Late";

            return null;
        }

        private DateTime ParseAttendanceDate(string dateValue)
        {
            if (string.IsNullOrWhiteSpace(dateValue))
            {
                return DateTime.Today;
            }

            if (DateTime.TryParseExact(dateValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime exactDate))
            {
                return exactDate.Date;
            }

            if (DateTime.TryParse(dateValue, out DateTime parsedDate))
            {
                return parsedDate.Date;
            }

            return DateTime.Today;
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