using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LMS.Models;

namespace LMS.Controllers
{
    public class StudentController : Controller
    {
        private readonly LMSContext db = new LMSContext();
        
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Course()
        {
            // Validate session
            if (Session["Id"] == null || (string)Session["Role"] != "Student")
            {
                TempData["ErrorMessage"] = "Session expired. Please log in again.";
                return RedirectToAction("Login", "Home");
            }

            int studentId = Convert.ToInt32(Session["Id"]);

            // Get student's enrolled courses with all necessary information
            var studentCourses = db.StudentCourses
                .Where(sc => sc.StudentId == studentId)
                .Include(sc => sc.Course)
                .Include(sc => sc.Section)
                .Include(sc => sc.Section.Program)
                .ToList()
                .Select(sc =>
                {
                    // Get semester from curriculum
                    var semester = GetSemesterFromCurriculum(sc.Section.ProgramId, sc.CourseId, sc.Section.YearLevel);

                    // Get teacher assignment for this course section
                    var teacherAssignment = db.TeacherCourseSections
                        .Include(tcs => tcs.Teacher)
                        .FirstOrDefault(tcs => tcs.CourseId == sc.CourseId &&
                                             tcs.SectionId == sc.SectionId &&
                                             tcs.Semester == semester);

                    dynamic item = new ExpandoObject();
                    item.CourseId = sc.CourseId;
                    item.CourseCode = sc.Course.CourseCode;
                    item.CourseName = sc.Course.CourseTitle;
                    item.CourseProf = teacherAssignment != null 
                        ? $"{teacherAssignment.Teacher.FirstName} {teacherAssignment.Teacher.LastName}"
                        : "Not Assigned";
                    item.SectionName = $"{sc.Section.Program.ProgramCode} {sc.Section.YearLevel}{sc.Section.SectionName}";
                    item.Day = sc.Day ?? "Not Set";
                    
                    // Format time display
                    if (sc.TimeFrom.HasValue && sc.TimeTo.HasValue)
                    {
                        item.Time = $"{sc.TimeFrom.Value:hh:mm tt} - {sc.TimeTo.Value:hh:mm tt}";
                    }
                    else
                    {
                        item.Time = "Not Set";
                    }
                    
                    item.SchoolYear = $"{DateTime.Now.Year} - {DateTime.Now.Year + 1}";
                    item.Status = sc.Status ?? "Ongoing";
                    
                    return item;
                })
                .ToList();

            return View(studentCourses);
        }

        // GET: Student/Material
        public ActionResult Material(int? id)
        {
            return LoadCourseTab(id, "Material");
        }

        // GET: Student/ViewMaterial
        public ActionResult ViewMaterial(int? id, int? materialId)
        {
            try
            {
                // Validate session
                if (Session["Id"] == null || (string)Session["Role"] != "Student")
                {
                    TempData["ErrorMessage"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Home");
                }

                if (!id.HasValue || !materialId.HasValue)
                {
                    TempData["ErrorMessage"] = "Invalid course or material ID.";
                    return RedirectToAction("Material", "Student", new { id = id });
                }

                int studentId = Convert.ToInt32(Session["Id"]);

                // Get student's enrollment for this course
                var studentCourse = db.StudentCourses
                    .Include(sc => sc.Course)
                    .Include(sc => sc.Section)
                    .Include(sc => sc.Section.Program)
                    .FirstOrDefault(sc => sc.StudentId == studentId && sc.CourseId == id.Value);

                if (studentCourse == null)
                {
                    TempData["ErrorMessage"] = "You are not enrolled in this course.";
                    return RedirectToAction("Material", "Student", new { id = id });
                }

                // Get semester from curriculum
                var semester = GetSemesterFromCurriculum(studentCourse.Section.ProgramId, id.Value, studentCourse.Section.YearLevel);

                // Get teacher assignment
                var teacherAssignment = db.TeacherCourseSections
                    .Include(tcs => tcs.Teacher)
                    .Include(tcs => tcs.Section)
                    .Include(tcs => tcs.Section.Program)
                    .FirstOrDefault(tcs => tcs.CourseId == id.Value &&
                                         tcs.SectionId == studentCourse.SectionId &&
                                         tcs.Semester == semester);

                if (teacherAssignment == null)
                {
                    TempData["ErrorMessage"] = "No teacher assigned to this course section.";
                    return RedirectToAction("Material", "Student", new { id = id });
                }

                // Get the material with files
                var material = db.Materials
                    .Include(m => m.MaterialFiles)
                    .Include(m => m.TeacherCourseSection)
                    .FirstOrDefault(m => m.Id == materialId.Value && m.TeacherCourseSectionId == teacherAssignment.Id);

                if (material == null)
                {
                    TempData["ErrorMessage"] = "Material not found or you don't have access.";
                    return RedirectToAction("Material", "Student", new { id = id });
                }

                var course = studentCourse.Course;

                // Set ViewBag data
                ViewBag.Material = material;
                ViewBag.Course = course;
                ViewBag.SectionName = teacherAssignment.Section.Program.ProgramCode + "-" +
                                      teacherAssignment.Section.YearLevel +
                                      teacherAssignment.Section.SectionName;
                ViewBag.CourseId = id.Value;
                ViewBag.ActiveTab = "Material";
                ViewBag.TeacherName = $"{teacherAssignment.Teacher.FirstName} {teacherAssignment.Teacher.LastName}";
                ViewBag.TeacherCourseSectionId = teacherAssignment.Id;

                return View("~/Views/Student/Course/ViewMaterial.cshtml", course);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ViewMaterial Error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading the material.";
                return RedirectToAction("Material", "Student", new { id = id });
            }
        }

        // GET: Student/Classwork
        public ActionResult Classwork(int? id)
        {
            var result = LoadCourseTab(id, "Classwork");

            if (id.HasValue)
            {
                int studentId = Convert.ToInt32(Session["Id"]);

                // Get the student's enrollment to find the teacher course section
                var studentCourse = db.StudentCourses
                    .Include(sc => sc.Section)
                    .FirstOrDefault(sc => sc.StudentId == studentId && sc.CourseId == id.Value);

                if (studentCourse != null)
                {
                    var semester = GetSemesterFromCurriculum(studentCourse.Section.ProgramId, id.Value, studentCourse.Section.YearLevel);

                    var teacherCourseSectionId = db.TeacherCourseSections
                        .Where(tcs => tcs.CourseId == id.Value && 
                                     tcs.SectionId == studentCourse.SectionId && 
                                     tcs.Semester == semester)
                        .Select(tcs => tcs.Id)
                        .FirstOrDefault();

                    if (teacherCourseSectionId > 0)
                    {
                        var now = DateTime.Now;
                        
                        // Get classwork for this section - filter out scheduled classwork that hasn't been published yet
                        // AND filter out manual entries (IsManualEntry = false)
                        var classworks = db.Classworks
                            .Where(c => c.TeacherCourseSectionId == teacherCourseSectionId && c.IsActive)
                            .Where(c => !c.IsScheduled || (c.ScheduledPublishDate.HasValue && c.ScheduledPublishDate.Value <= now))
                            .Where(c => !c.IsManualEntry) // Exclude manual entries from notifications
                            .OrderByDescending(c => c.DateCreated)
                            .ToList()
                            .Select(c =>
                            {
                                // Get student's submission for this classwork
                                var submission = db.ClassworkSubmissions
                                    .FirstOrDefault(s => s.ClassworkId == c.Id && s.StudentId == studentId);

                                dynamic item = new ExpandoObject();
                                item.Id = c.Id;
                                item.Title = c.Title;
                                item.ClassworkType = c.ClassworkType;
                                item.Points = c.Points;
                                item.Deadline = c.Deadline;
                                item.DateCreated = c.DateCreated;
                                item.Description = c.Description;
                                item.SubmissionStatus = submission?.Status ?? "Not Submitted";
                                item.SubmittedAt = submission?.SubmittedAt;
                                item.Grade = submission?.Grade;
                                item.Feedback = submission?.Feedback;
                                item.GradedAt = submission?.GradedAt;
                                return item;
                            })
                            .ToList();

                        ViewBag.Classworks = classworks;
                    }
                }
            }

            return result;
        }

        // GET: Student/Announcement
        public ActionResult Announcement(int? id)
        {
            var result = LoadCourseTab(id, "Announcement");

            if (id.HasValue)
            {
                int studentId = Convert.ToInt32(Session["Id"]);

                // Get the student's enrollment to find the teacher course section
                var studentCourse = db.StudentCourses
                    .Include(sc => sc.Section)
                    .FirstOrDefault(sc => sc.StudentId == studentId && sc.CourseId == id.Value);

                if (studentCourse != null)
                {
                    var semester = GetSemesterFromCurriculum(studentCourse.Section.ProgramId, id.Value, studentCourse.Section.YearLevel);

                    var teacherCourseSectionId = db.TeacherCourseSections
                        .Where(tcs => tcs.CourseId == id.Value && 
                                     tcs.SectionId == studentCourse.SectionId && 
                                     tcs.Semester == semester)
                        .Select(tcs => tcs.Id)
                        .FirstOrDefault();

                    if (teacherCourseSectionId > 0)
                    {
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
                        ViewBag.TeacherCourseSectionId = teacherCourseSectionId;
                    }
                }
            }

            return result;
        }

        // GET: View Announcement Detail
        public ActionResult ViewAnnouncement(int? id, int? announcementId)
        {
            try
            {
                // Validate session
                if (Session["Id"] == null || (string)Session["Role"] != "Student")
                {
                    TempData["ErrorMessage"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Home");
                }

                if (!id.HasValue || !announcementId.HasValue)
                {
                    TempData["ErrorMessage"] = "Invalid parameters.";
                    return RedirectToAction("Announcement", "Student", new { id = id });
                }

                int studentId = Convert.ToInt32(Session["Id"]);

                // Get student's enrollment for this course
                var studentCourse = db.StudentCourses
                    .Include(sc => sc.Course)
                    .Include(sc => sc.Section)
                    .Include(sc => sc.Section.Program)
                    .FirstOrDefault(sc => sc.StudentId == studentId && sc.CourseId == id.Value);

                if (studentCourse == null)
                {
                    TempData["ErrorMessage"] = "You are not enrolled in this course.";
                    return RedirectToAction("Announcement", "Student", new { id = id });
                }

                // Get semester from curriculum
                var semester = GetSemesterFromCurriculum(studentCourse.Section.ProgramId, id.Value, studentCourse.Section.YearLevel);

                // Get teacher assignment for this course section
                var teacherAssignment = db.TeacherCourseSections
                    .Include(tcs => tcs.Teacher)
                    .Include(tcs => tcs.Section)
                    .Include(tcs => tcs.Section.Program)
                    .FirstOrDefault(tcs => tcs.CourseId == id.Value &&
                                         tcs.SectionId == studentCourse.SectionId &&
                                         tcs.Semester == semester);

                if (teacherAssignment == null)
                {
                    TempData["ErrorMessage"] = "No teacher assigned to this course section.";
                    return RedirectToAction("Announcement", "Student", new { id = id });
                }

                // Get the announcement with comments
                var announcement = db.Announcements
                    .Include(a => a.Comments.Select(c => c.User))
                    .Include(a => a.TeacherCourseSection.Teacher)
                    .FirstOrDefault(a => a.Id == announcementId.Value && 
                                       a.TeacherCourseSectionId == teacherAssignment.Id && 
                                       a.IsActive);

                if (announcement == null)
                {
                    TempData["ErrorMessage"] = "Announcement not found.";
                    return RedirectToAction("Announcement", "Student", new { id = id });
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
                ViewBag.SectionName = teacherAssignment.Section.Program.ProgramCode + "-" +
                                     teacherAssignment.Section.YearLevel +
                                     teacherAssignment.Section.SectionName;
                ViewBag.TeacherCourseSectionId = teacherAssignment.Id;
                ViewBag.Semester = semester;

                return View("~/Views/Student/Course/ViewAnnouncement.cshtml", studentCourse.Course);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ViewAnnouncement Error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading the announcement.";
                return RedirectToAction("Announcement", "Student", new { id = id });
            }
        }

        // GET: Create Announcement
        public ActionResult CreateAnnouncement(int? id)
        {
            return LoadCourseTab(id, "CreateAnnouncement");
        }

        // POST: Create Announcement
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)] // Allow HTML content
        public ActionResult CreateAnnouncement(int StudentCourseId, int TeacherCourseSectionId, string content)
        {
            try
            {
                // Validate session
                if (Session["Id"] == null || (string)Session["Role"] != "Student")
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                int studentId = Convert.ToInt32(Session["Id"]);

                // Verify the student is enrolled in this course
                var studentCourse = db.StudentCourses
                    .FirstOrDefault(sc => sc.StudentId == studentId && sc.CourseId == StudentCourseId);

                if (studentCourse == null)
                {
                    return Json(new { success = false, message = "You are not enrolled in this course." });
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    return Json(new { success = false, message = "Content is required." });
                }

                // Create the announcement
                var announcement = new Announcement
                {
                    TeacherCourseSectionId = TeacherCourseSectionId,
                    CreatedByUserId = studentId,
                    Content = content, // HTML content from rich text editor
                    PostedAt = DateTime.Now,
                    IsActive = true
                };

                db.Announcements.Add(announcement);
                db.SaveChanges();

                // Send email notifications to other students and teacher in the course
                LMS.Helpers.StudentNotificationService.NotifyAnnouncementPosted(TeacherCourseSectionId, announcement.Id);

                return Json(new { success = true, message = "Announcement posted successfully!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateAnnouncement Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while posting the announcement: " + ex.Message });
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

        // NEW: Get Notifications for Student
        [HttpGet]
        public JsonResult GetNotifications()
        {
            try
            {
                if (Session["Id"] == null || (string)Session["Role"] != "Student")
                {
                    return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);
                }

                int studentId = Convert.ToInt32(Session["Id"]);
                var now = DateTime.Now;
                var next24 = now.AddHours(24);

                var enrollments = db.StudentCourses
                    .Include(sc => sc.Section)
                    .Include(sc => sc.Section.Program)
                    .Where(sc => sc.StudentId == studentId)
                    .ToList();

                if (!enrollments.Any())
                {
                    return Json(new { success = true, notifications = new object[0] }, JsonRequestBehavior.AllowGet);
                }

                var tcsIds = new List<int>();
                foreach (var sc in enrollments)
                {
                    var semester = GetSemesterFromCurriculum(sc.Section.ProgramId, sc.CourseId, sc.Section.YearLevel);
                    var tcsId = db.TeacherCourseSections
                        .Where(tcs => tcs.CourseId == sc.CourseId && tcs.SectionId == sc.SectionId && tcs.Semester == semester)
                        .Select(tcs => tcs.Id)
                        .FirstOrDefault();
                    if (tcsId > 0) tcsIds.Add(tcsId);
                }

                if (!tcsIds.Any())
                {
                    return Json(new { success = true, notifications = new object[0] }, JsonRequestBehavior.AllowGet);
                }

                var materials = db.Materials
                    .Include(m => m.TeacherCourseSection.Course)
                    .Include(m => m.TeacherCourseSection.Teacher)
                    .Where(m => tcsIds.Contains(m.TeacherCourseSectionId))
                    .OrderByDescending(m => m.UploadedAt)
                    .Take(50)
                    .ToList();

                var classworks = db.Classworks
                    .Include(c => c.TeacherCourseSection.Course)
                    .Where(c => tcsIds.Contains(c.TeacherCourseSectionId) && c.IsActive)
                    .Where(c => !c.IsScheduled || (c.ScheduledPublishDate.HasValue && c.ScheduledPublishDate.Value <= now))
                    .Where(c => !c.IsManualEntry)
                    .OrderByDescending(c => c.DateCreated)
                    .Take(100)
                    .ToList();

                var dueSoonCandidates = classworks
                    .Where(c => c.Deadline.HasValue && c.Deadline.Value > now && c.Deadline.Value <= next24)
                    .Select(c => c.Id)
                    .ToList();

                var myDueSubs = db.ClassworkSubmissions
                    .Where(s => dueSoonCandidates.Contains(s.ClassworkId) && s.StudentId == studentId)
                    .ToList();

                var notifications = new List<object>();

                foreach (var m in materials)
                {
                    var courseId = m.TeacherCourseSection?.CourseId ?? 0;
                    var courseName = m.TeacherCourseSection?.Course?.CourseTitle ?? "Course";
                    var teacherName = m.TeacherCourseSection?.Teacher != null
                        ? m.TeacherCourseSection.Teacher.FirstName + " " + m.TeacherCourseSection.Teacher.LastName
                        : "Teacher";

                    notifications.Add(new
                    {
                        id = $"mat_{m.Id}",
                        type = "material",
                        title = "New Material Posted",
                        message = $"{m.Title} in {courseName} by {teacherName}",
                        date = m.UploadedAt,
                        unread = (now - m.UploadedAt).TotalHours <= 24,
                        url = Url.Action("ViewMaterial", "Student", new { id = courseId, materialId = m.Id })
                    });
                }

                foreach (var c in classworks)
                {
                    var courseId = c.TeacherCourseSection?.CourseId ?? 0;
                    var courseName = c.TeacherCourseSection?.Course?.CourseTitle ?? "Course";

                    notifications.Add(new
                    {
                        id = $"cw_{c.Id}",
                        type = "assignment",
                        title = "New Classwork Posted",
                        message = $"{c.Title} in {courseName}",
                        date = c.DateCreated,
                        unread = (now - c.DateCreated).TotalHours <= 24,
                        url = Url.Action("ViewClasswork", "Student", new { id = courseId, classworkId = c.Id })
                    });
                }

                foreach (var c in classworks.Where(x => x.Deadline.HasValue && x.Deadline.Value > now && x.Deadline.Value <= next24))
                {
                    var sub = myDueSubs.FirstOrDefault(s => s.ClassworkId == c.Id);
                    var notSubmitted = sub == null || string.IsNullOrEmpty(sub.Status) || sub.Status == "Not Submitted";
                    if (!notSubmitted) continue;

                    var hoursLeft = Math.Ceiling((c.Deadline.Value - now).TotalHours);
                    var courseId = c.TeacherCourseSection?.CourseId ?? 0;
                    var courseName = c.TeacherCourseSection?.Course?.CourseTitle ?? "Course";

                    notifications.Add(new
                    {
                        id = $"due_{c.Id}",
                        type = "assignment",
                        title = "Due Soon",
                        message = $"{c.Title} in {courseName} is due in {hoursLeft}h",
                        date = c.Deadline.Value,
                        unread = true,
                        isDeadline = true,
                        url = Url.Action("ViewClasswork", "Student", new { id = courseId, classworkId = c.Id })
                    });
                }

                var finalList = notifications
                    .OrderByDescending(n => ((DateTime)n.GetType().GetProperty("date").GetValue(n, null)))
                    .ToList();

                return Json(new { success = true, notifications = finalList }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetNotifications Error: {ex.Message}");
                return Json(new { success = false, message = "Error loading notifications" }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Edit Announcement
        public ActionResult EditAnnouncement(int? id, int? announcementId)
        {
            try
            {
                // Validate session
                if (Session["Id"] == null || (string)Session["Role"] != "Student")
                {
                    TempData["ErrorMessage"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Home");
                }

                if (!id.HasValue || !announcementId.HasValue)
                {
                    TempData["ErrorMessage"] = "Invalid course or announcement ID.";
                    return RedirectToAction("Announcement", "Student", new { id = id });
                }

                int studentId = Convert.ToInt32(Session["Id"]);

                // Get student's enrollment for this course
                var studentCourse = db.StudentCourses
                    .Include(sc => sc.Section)
                    .FirstOrDefault(sc => sc.StudentId == studentId && sc.CourseId == id.Value);

                if (studentCourse == null)
                {
                    TempData["ErrorMessage"] = "You are not enrolled in this course.";
                    return RedirectToAction("Announcement", "Student", new { id = id });
                }

                // Get the announcement
                var announcement = db.Announcements
                    .FirstOrDefault(a => a.Id == announcementId.Value && a.IsActive);

                if (announcement == null)
                {
                    TempData["ErrorMessage"] = "Announcement not found.";
                    return RedirectToAction("Announcement", "Student", new { id = id });
                }

                // Verify the student owns this announcement
                if (announcement.CreatedByUserId != studentId)
                {
                    TempData["ErrorMessage"] = "You don't have access to edit this announcement.";
                    return RedirectToAction("Announcement", "Student", new { id = id });
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
                return RedirectToAction("Announcement", "Student", new { id = id });
            }
        }

        // POST: Update Announcement
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)] // Allow HTML content
        public ActionResult UpdateAnnouncement(int AnnouncementId, int StudentCourseId, int TeacherCourseSectionId, string content)
        {
            try
            {
                // Validate session
                if (Session["Id"] == null || (string)Session["Role"] != "Student")
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                int studentId = Convert.ToInt32(Session["Id"]);

                // Verify the student is enrolled in this course
                var studentCourse = db.StudentCourses
                    .FirstOrDefault(sc => sc.StudentId == studentId && sc.CourseId == StudentCourseId);

                if (studentCourse == null)
                {
                    return Json(new { success = false, message = "You are not enrolled in this course." });
                }

                // Get the existing announcement
                var announcement = db.Announcements
                    .FirstOrDefault(a => a.Id == AnnouncementId &&
                                       a.TeacherCourseSectionId == TeacherCourseSectionId &&
                                       a.IsActive);

                if (announcement == null)
                {
                    return Json(new { success = false, message = "Announcement not found." });
                }

                // Verify the student owns this announcement
                if (announcement.CreatedByUserId != studentId)
                {
                    return Json(new { success = false, message = "You don't have access to edit this announcement." });
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

        // POST: Delete Announcement
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAnnouncement(int id)
        {
            try
            {
                if (Session["Id"] == null || (string)Session["Role"] != "Student")
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                int studentId = Convert.ToInt32(Session["Id"]);

                var announcement = db.Announcements
                    .Include(a => a.Comments)
                    .FirstOrDefault(a => a.Id == id);

                if (announcement == null)
                {
                    return Json(new { success = false, message = "Announcement not found." });
                }

                // Student can only delete their own announcement
                if (announcement.CreatedByUserId != studentId)
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

        public ActionResult TodoList()
        {
            try
            {
                // Validate session
                if (Session["Id"] == null || (string)Session["Role"] != "Student")
                {
                    TempData["ErrorMessage"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Home");
                }

                int studentId = Convert.ToInt32(Session["Id"]);

                // Check if student has any enrolled courses
                var hasEnrollments = db.StudentCourses
                    .Any(sc => sc.StudentId == studentId);

                if (hasEnrollments)
                {
                    // Redirect to the Assigned tab showing all courses
                    return RedirectToAction("Assigned", "Student");
                }
                else
                {
                    // If no courses found, show empty todo list view
                    TempData["InfoMessage"] = "You are not enrolled in any courses yet.";
                    return View();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TodoList Error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading your todo list.";
                return View();
            }
        }

        public ActionResult Notification()
        {
            return View();
        }

        public new ActionResult Profile()
        {
            // Validate session
            if (Session["Id"] == null || (string)Session["Role"] != "Student")
            {
                TempData["ErrorMessage"] = "Session expired. Please log in again.";
                return RedirectToAction("Login", "Home");
            }

            int studentId = Convert.ToInt32(Session["Id"]);
            var user = db.Users
                .Include(u => u.Department)
                .FirstOrDefault(u => u.Id == studentId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index", "Student");
            }

            // Get student's enrollment information
            var studentCourse = db.StudentCourses
                .Include(sc => sc.Section)
                .Include(sc => sc.Section.Program)
                .Include(sc => sc.Section.Program.Department)
                .FirstOrDefault(sc => sc.StudentId == studentId);

            // Create profile view model with restricted access for students
            var profileViewModel = new ProfileUpdateViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DateOfBirth = user.DateOfBirth,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                DepartmentId = user.DepartmentId,
                EmergencyContactName = user.EmergencyContactName,
                EmergencyContactPhone = user.EmergencyContactPhone,
                EmergencyContactRelationship = user.EmergencyContactRelationship,
                Role = user.Role,
                UserID = user.UserID,
                ProfilePicture = user.ProfilePicture
            };

            ViewBag.User = user;
            ViewBag.StudentCourse = studentCourse;
            ViewBag.ProfileViewModel = profileViewModel;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(string firstName, string lastName, string email, string phoneNumber, string address, 
            string emergencyContactName, string emergencyContactPhone, string emergencyContactRelationship, string dateOfBirth)
        {
            try
            {
                if (Session["Id"] == null || (string)Session["Role"] != "Student")
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                int studentId = Convert.ToInt32(Session["Id"]);
                var user = db.Users.Find(studentId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                // Students can now update:
                // - First Name, Last Name, Date of Birth (personal info)
                // - Email, Phone Number, Address (contact info)
                // - Emergency Contact Information

                // Validate required fields
                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    return Json(new { success = false, message = "First name and last name are required." });
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    return Json(new { success = false, message = "Email is required." });
                }

                // Check if email is already in use by another user
                var existingUser = db.Users.FirstOrDefault(u => u.Email == email && u.Id != studentId);
                if (existingUser != null)
                {
                    return Json(new { success = false, message = "This email is already in use by another user." });
                }

                // Parse date of birth
                DateTime? parsedDateOfBirth = null;
                if (!string.IsNullOrWhiteSpace(dateOfBirth))
                {
                    DateTime tempDate;
                    if (DateTime.TryParse(dateOfBirth, out tempDate))
                    {
                        parsedDateOfBirth = tempDate;
                    }
                }

                // Update allowed fields for students
                user.FirstName = firstName?.Trim();
                user.LastName = lastName?.Trim();
                user.DateOfBirth = parsedDateOfBirth;
                user.Email = email?.Trim();
                user.PhoneNumber = phoneNumber?.Trim();
                user.Address = address?.Trim();
                user.EmergencyContactName = emergencyContactName?.Trim();
                user.EmergencyContactPhone = emergencyContactPhone?.Trim();
                user.EmergencyContactRelationship = emergencyContactRelationship?.Trim();

                // Students cannot update: UserID, Role, DepartmentId
                // These should be managed by administrators only

                db.SaveChanges();

                // Update session variables
                Session["FirstName"] = user.FirstName;
                Session["LastName"] = user.LastName;

                return Json(new { success = true, message = "Profile updated successfully!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateProfile Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while updating your profile." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadProfilePicture()
        {
            try
            {
                if (Session["Id"] == null || (string)Session["Role"] != "Student")
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                if (Request.Files.Count == 0 || Request.Files[0] == null || Request.Files[0].ContentLength == 0)
                {
                    return Json(new { success = false, message = "Please select a file to upload." });
                }

                var file = Request.Files[0];
                
                // Validate file type
                var allowedTypes = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = System.IO.Path.GetExtension(file.FileName).ToLower();
                
                if (!allowedTypes.Contains(fileExtension))
                {
                    return Json(new { success = false, message = "Only JPG, PNG, and GIF files are allowed." });
                }

                // Validate file size (5MB max)
                if (file.ContentLength > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "File size must be less than 5MB." });
                }

                int studentId = Convert.ToInt32(Session["Id"]);
                var user = db.Users.Find(studentId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                // Create uploads directory if it doesn't exist
                var uploadsDir = Server.MapPath("~/Uploads/ProfilePictures");
                if (!System.IO.Directory.Exists(uploadsDir))
                {
                    System.IO.Directory.CreateDirectory(uploadsDir);
                }

                // Delete old profile picture if exists
                if (!string.IsNullOrEmpty(user.ProfilePicture))
                {
                    var oldFilePath = Server.MapPath(user.ProfilePicture);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error deleting old profile picture: {ex.Message}");
                        }
                    }
                }

                // Generate unique filename
                var fileName = $"{user.UserID}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                var filePath = System.IO.Path.Combine(uploadsDir, fileName);
                
                // Save file
                file.SaveAs(filePath);

                // Update user profile picture path
                user.ProfilePicture = $"~/Uploads/ProfilePictures/{fileName}";
                db.SaveChanges();

                return Json(new { 
                    success = true, 
                    message = "Profile picture updated successfully!",
                    profilePictureUrl = Url.Content(user.ProfilePicture)
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UploadProfilePicture Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while uploading the profile picture." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            try
            {
                if (Session["Id"] == null || (string)Session["Role"] != "Student")
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                int studentId = Convert.ToInt32(Session["Id"]);
                var user = db.Users.Find(studentId);

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
                {
                    return Json(new { success = false, message = "Please fill in all password fields." });
                }

                if (newPassword != confirmPassword)
                {
                    return Json(new { success = false, message = "New passwords do not match." });
                }

                if (newPassword.Length < 6)
                {
                    return Json(new { success = false, message = "Password must be at least 6 characters long." });
                }

                // Verify current password
                if (HashPassword(currentPassword) != user.Password)
                {
                    return Json(new { success = false, message = "Current password is incorrect." });
                }

                // Update password
                user.Password = HashPassword(newPassword);
                db.SaveChanges();

                return Json(new { success = true, message = "Password changed successfully!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChangePassword Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while changing your password." });
            }
        }

        // POST: Add Comment to Material
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddMaterialComment(int materialId, string comment)
        {
            try
            {
                if (Session["Id"] == null || (string)Session["Role"] != "Student")
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                int studentId = Convert.ToInt32(Session["Id"]);

                // Verify material exists
                var material = db.Materials
                    .Include(m => m.TeacherCourseSection)
                    .FirstOrDefault(m => m.Id == materialId);

                if (material == null)
                {
                    return Json(new { success = false, message = "Material not found." });
                }

                // Verify the student is enrolled in the course
                var isEnrolled = db.StudentCourses
                    .Any(sc => sc.StudentId == studentId && 
                              sc.CourseId == material.TeacherCourseSection.CourseId &&
                              sc.SectionId == material.TeacherCourseSection.SectionId);

                if (!isEnrolled)
                {
                    return Json(new { success = false, message = "You don't have access to this material." });
                }

                // Validate comment
                if (string.IsNullOrWhiteSpace(comment))
                {
                    return Json(new { success = false, message = "Comment cannot be empty." });
                }

                // Add comment
                var newComment = new MaterialComment
                {
                    MaterialId = materialId,
                    UserId = studentId,
                    Comment = comment.Trim(),
                    CreatedAt = DateTime.Now
                };

                db.MaterialComments.Add(newComment);
                db.SaveChanges();

                // Return the comment with user info for immediate display
                var user = db.Users.Find(studentId);
                var userName = $"{user.FirstName} {user.LastName}";
                var initials = $"{user.FirstName.Substring(0, 1)}{user.LastName.Substring(0, 1)}".ToUpper();

                return Json(new
                {
                    success = true,
                    message = "Comment added successfully.",
                    comment = new
                    {
                        id = newComment.Id,
                        userName = userName,
                        userInitials = initials,
                        comment = newComment.Comment,
                        createdAt = newComment.CreatedAt.ToString("MMM dd, h:mm tt"),
                        userId = studentId
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddMaterialComment Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while adding the comment." });
            }
        }

        // GET: Get Material Comments
        [HttpGet]
        public JsonResult GetMaterialComments(int materialId)
        {
            try
            {
                if (Session["Id"] == null || (string)Session["Role"] != "Student")
                {
                    return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);
                }

                int studentId = Convert.ToInt32(Session["Id"]);

                // Verify material exists
                var material = db.Materials
                    .Include(m => m.TeacherCourseSection)
                    .FirstOrDefault(m => m.Id == materialId);

                if (material == null)
                {
                    return Json(new { success = false, message = "Material not found." }, JsonRequestBehavior.AllowGet);
                }

                // Verify the student is enrolled in the course
                var isEnrolled = db.StudentCourses
                    .Any(sc => sc.StudentId == studentId && 
                              sc.CourseId == material.TeacherCourseSection.CourseId &&
                              sc.SectionId == material.TeacherCourseSection.SectionId);

                if (!isEnrolled)
                {
                    return Json(new { success = false, message = "You don't have access to this material." }, JsonRequestBehavior.AllowGet);
                }

                // Get all comments for this material
                var comments = db.MaterialComments
                    .Where(mc => mc.MaterialId == materialId)
                    .Include(mc => mc.User)
                    .OrderBy(mc => mc.CreatedAt)
                    .ToList()
                    .Select(mc => new
                    {
                        id = mc.Id,
                        userId = mc.UserId,
                        userName = mc.User.FirstName + " " + mc.User.LastName,
                        userInitials = mc.User.FirstName.Substring(0, 1) + mc.User.LastName.Substring(0, 1),
                        comment = mc.Comment,
                        createdAt = mc.CreatedAt.ToString("MMM dd, h:mm tt"),
                        isMine = mc.UserId == studentId
                    })
                    .ToList();

                return Json(new { success = true, comments }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetMaterialComments Error: {ex.Message}");
                return Json(new { success = false, message = "Error loading comments: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // Helper method for password hashing (same as in AccountController)
        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                var builder = new System.Text.StringBuilder();
                foreach (byte b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        // Helper method to get semester from curriculum
        private int GetSemesterFromCurriculum(int programId, int courseId, int yearLevel)
        {
            var curriculumCourse = db.CurriculumCourses
                .FirstOrDefault(cc => cc.ProgramId == programId
                    && cc.CourseId == courseId
                    && cc.YearLevel == yearLevel);

            return curriculumCourse?.Semester ?? 1;
        }

        // Private helper method to load course tabs
        private ActionResult LoadCourseTab(int? id, string viewName)
        {
            if (Session["Id"] == null || (string)Session["Role"] != "Student")
            {
                TempData["ErrorMessage"] = "Session expired. Please log in again.";
                return RedirectToAction("Login", "Home");
            }

            if (!id.HasValue)
            {
                TempData["ErrorMessage"] = "Invalid course ID.";
                return RedirectToAction("Course", "Student");
            }

            int studentId = Convert.ToInt32(Session["Id"]);

            // Get student's enrollment for this course
            var studentCourse = db.StudentCourses
                .Include(sc => sc.Course)
                .Include(sc => sc.Section)
                .Include(sc => sc.Section.Program)
                .FirstOrDefault(sc => sc.StudentId == studentId && sc.CourseId == id.Value);

            if (studentCourse == null)
            {
                TempData["ErrorMessage"] = "You are not enrolled in this course.";
                return RedirectToAction("Course", "Student");
            }

            var course = studentCourse.Course;

            // Get semester from curriculum
            var semester = GetSemesterFromCurriculum(studentCourse.Section.ProgramId, id.Value, studentCourse.Section.YearLevel);

            // Get teacher assignment for this course section
            var teacherAssignment = db.TeacherCourseSections
                .Include(tcs => tcs.Teacher)
                .Include(tcs => tcs.Section)
                .Include(tcs => tcs.Section.Program)
                .FirstOrDefault(tcs => tcs.CourseId == id.Value &&
                                     tcs.SectionId == studentCourse.SectionId &&
                                     tcs.Semester == semester);

            if (teacherAssignment == null)
            {
                TempData["ErrorMessage"] = "No teacher assigned to this course section.";
                return RedirectToAction("Course", "Student");
            }

            // Filter materials to only show those for this specific section
            var sectionMaterials = db.Materials
                .Include(m => m.MaterialFiles)
                .Where(m => m.TeacherCourseSectionId == teacherAssignment.Id)
                .OrderByDescending(m => m.UploadedAt)
                .ToList();

            // Get student count for this section
            var studentCount = db.StudentCourses
                .Count(sc => sc.CourseId == id.Value && sc.SectionId == studentCourse.SectionId);

            // Pass materials through ViewBag
            ViewBag.Materials = sectionMaterials;

            // Common ViewBag data
            ViewBag.SectionName = teacherAssignment.Section.Program.ProgramCode + "-" +
                                  teacherAssignment.Section.YearLevel +
                                  teacherAssignment.Section.SectionName;
            ViewBag.Semester = semester;
            ViewBag.TeacherCourseSectionId = teacherAssignment.Id;
            ViewBag.CourseId = id.Value;
            ViewBag.TeacherName = $"{teacherAssignment.Teacher.FirstName} {teacherAssignment.Teacher.LastName}";
            ViewBag.StudentCount = studentCount;
            ViewBag.Day = studentCourse.Day;
            ViewBag.TimeFrom = studentCourse.TimeFrom;
            ViewBag.TimeTo = studentCourse.TimeTo;

            // Return the specific tab view
            return View($"~/Views/Student/Course/{viewName}.cshtml", course);
        }

        // GET: Submit/Take Classwork
        public ActionResult SubmitClasswork(int? id, int? classworkId)
        {
            try
            {
                // Validate session
                if (Session["Id"] == null || (string)Session["Role"] != "Student")
                {
                    TempData["ErrorMessage"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Home");
                }

                if (!id.HasValue || !classworkId.HasValue)
                {
                    TempData["ErrorMessage"] = "Invalid parameters.";
                    return RedirectToAction("Classwork", "Student", new { id = id });
                }

                int studentId = Convert.ToInt32(Session["Id"]);

                // Get student's enrollment
                var studentCourse = db.StudentCourses
                    .Include(sc => sc.Course)
                    .Include(sc => sc.Section)
                    .Include(sc => sc.Section.Program)
                    .FirstOrDefault(sc => sc.StudentId == studentId && sc.CourseId == id.Value);

                if (studentCourse == null)
                {
                    TempData["ErrorMessage"] = "You are not enrolled in this course.";
                    return RedirectToAction("Classwork", "Student", new { id = id });
                }

                var semester = GetSemesterFromCurriculum(studentCourse.Section.ProgramId, id.Value, studentCourse.Section.YearLevel);

                var teacherCourseSectionId = db.TeacherCourseSections
                    .Where(tcs => tcs.CourseId == id.Value &&
                                 tcs.SectionId == studentCourse.SectionId &&
                                 tcs.Semester == semester)
                    .Select(tcs => tcs.Id)
                    .FirstOrDefault();

                // Get classwork
                var classwork = db.Classworks
                    .FirstOrDefault(c => c.Id == classworkId.Value && 
                                       c.TeacherCourseSectionId == teacherCourseSectionId &&
                                       c.IsActive &&
                                       !c.IsManualEntry); // Prevent access to manual entries

                if (classwork == null)
                {
                    TempData["ErrorMessage"] = "Classwork not found or not available.";
                    return RedirectToAction("Classwork", "Student", new { id = id });
                }

                // Check if already submitted
                var existingSubmission = db.ClassworkSubmissions
                    .FirstOrDefault(s => s.ClassworkId == classworkId.Value && s.StudentId == studentId);

                if (existingSubmission != null && existingSubmission.Status != "Not Submitted")
                {
                    TempData["ErrorMessage"] = "You have already submitted this classwork.";
                    return RedirectToAction("ViewClasswork", "Student", new { id = id, classworkId = classworkId });
                }

                // Check if it's a quiz/exam with questions
                if (!string.IsNullOrEmpty(classwork.QuestionsJson))
                {
                    // Parse questions
                    var questions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(classwork.QuestionsJson);

                    dynamic classworkData = new ExpandoObject();
                    classworkData.Id = classwork.Id;
                    classworkData.Title = classwork.Title;
                    classworkData.Description = classwork.Description;
                    classworkData.Points = classwork.Points;
                    classworkData.Deadline = classwork.Deadline;

                    ViewBag.Classwork = classworkData;
                    ViewBag.Questions = questions;
                    ViewBag.CourseId = id.Value;

                    return View("~/Views/Student/Course/TakeQuiz.cshtml", studentCourse.Course);
                }
                else
                {
                    // File submission type - redirect to file upload view
                    return RedirectToAction("SubmitClassworkFile", "Student", new { id = id, classworkId = classworkId });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SubmitClasswork Error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return RedirectToAction("Classwork", "Student", new { id = id });
            }
        }

        // POST: Submit Quiz Answers
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitQuizAnswers(int classworkId, int courseId, string answersJson)
        {
            try
            {
                if (Session["Id"] == null || (string)Session["Role"] != "Student")
                {
                    return Json(new { success = false, message = "Session expired." });
                }

                int studentId = Convert.ToInt32(Session["Id"]);

                // Get or create submission
                var submission = db.ClassworkSubmissions
                    .FirstOrDefault(s => s.ClassworkId == classworkId && s.StudentId == studentId);

                if (submission == null)
                {
                    submission = new ClassworkSubmission
                    {
                        ClassworkId = classworkId,
                        StudentId = studentId,
                        Status = "Not Submitted"
                    };
                    db.ClassworkSubmissions.Add(submission);
                }

                // Check if already graded
                if (submission.Status == "Graded")
                {
                    return Json(new { success = false, message = "This submission has already been graded." });
                }

                // Update submission
                submission.AnswersJson = answersJson;
                submission.SubmittedAt = DateTime.Now;
                submission.Status = "Submitted";

                // Save to get submission ID
                db.SaveChanges();

                // Handle file uploads from file upload questions
                if (Request.Files.Count > 0)
                {
                    var uploadPath = Server.MapPath("~/Uploads/Submissions");
                    if (!System.IO.Directory.Exists(uploadPath))
                    {
                        System.IO.Directory.CreateDirectory(uploadPath);
                    }

                    for (int i = 0; i < Request.Files.Count; i++)
                    {
                        var file = Request.Files[i];
                        if (file != null && file.ContentLength > 0)
                        {
                            var fileName = Path.GetFileName(file.FileName);
                            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                            var filePath = Path.Combine(uploadPath, uniqueFileName);
                            file.SaveAs(filePath);

                            // Save file info to database
                            var submissionFile = new SubmissionFile
                            {
                                SubmissionId = submission.Id,
                                FileName = fileName,
                                FilePath = $"/Uploads/Submissions/{uniqueFileName}",
                                SizeInMB = (decimal)file.ContentLength / (1024 * 1024),
                                UploadedAt = DateTime.Now
                            };
                            db.SubmissionFiles.Add(submissionFile);
                        }
                    }
                    db.SaveChanges();
                }

                // Auto-grade if possible
                var classwork = db.Classworks.Find(classworkId);
                if (classwork != null && !string.IsNullOrEmpty(classwork.QuestionsJson))
                {
                    var questions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(classwork.QuestionsJson);
                    var answers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(answersJson);
                    
                    decimal totalScore = 0;
                    bool canAutoGrade = true;

                    for (int i = 0; i < questions.Count && i < answers.Count; i++)
                    {
                        var question = questions[i];
                        var answer = answers[i];
                        string questionType = question.type;

                        // Only auto-grade objective questions (skip file upload and essay)
                        if (questionType == "multipleChoice" || questionType == "trueFalse" || 
                            questionType == "identification" || questionType == "multipleAnswer")
                        {
                            bool isCorrect = false;

                            if (questionType == "multipleChoice")
                            {
                                var correctIndex = -1;
                                for (int j = 0; j < question.options.Count; j++)
                                {
                                    if ((bool)question.options[j].isCorrect)
                                    {
                                        correctIndex = j;
                                        break;
                                    }
                                }
                                isCorrect = answer.answer != null && answer.answer.ToString() == correctIndex.ToString();
                            }
                            else if (questionType == "trueFalse")
                            {
                                bool correctAnswer = (bool)question.correctAnswer;
                                isCorrect = answer.answer != null && answer.answer.ToString().ToLower() == correctAnswer.ToString().ToLower();
                            }
                            else if (questionType == "identification")
                            {
                                string correctAnswer = question.correctAnswer.ToString();
                                string studentAnswer = answer.answer?.ToString() ?? "";
                                bool caseSensitive = question.caseSensitive ?? false;

                                if (caseSensitive)
                                {
                                    isCorrect = studentAnswer.Trim() == correctAnswer.Trim();
                                }
                                else
                                {
                                    isCorrect = studentAnswer.Trim().Equals(correctAnswer.Trim(), StringComparison.OrdinalIgnoreCase);
                                }
                            }
                            else if (questionType == "multipleAnswer")
                            {
                                var correctIndices = new List<int>();
                                for (int j = 0; j < question.options.Count; j++)
                                {
                                    if ((bool)question.options[j].isCorrect)
                                    {
                                        correctIndices.Add(j);
                                    }
                                }

                                var studentAnswers = new List<int>();
                                if (answer.answer != null)
                                {
                                    var answerArray = answer.answer as Newtonsoft.Json.Linq.JArray;
                                    if (answerArray != null)
                                    {
                                        studentAnswers = answerArray.Select(a => (int)a).ToList();
                                    }
                                }

                                isCorrect = correctIndices.Count == studentAnswers.Count &&
                                           correctIndices.All(studentAnswers.Contains);
                            }

                            if (isCorrect)
                            {
                                totalScore += (decimal)question.points;
                            }
                        }
                        else
                        {
                            // Essay or file upload - needs manual grading
                            canAutoGrade = false;
                        }
                    }

                    if (canAutoGrade)
                    {
                        submission.Grade = totalScore;
                        submission.GradedAt = DateTime.Now;
                        submission.Status = "Graded";
                        db.SaveChanges();
                    }
                }

                return Json(new { success = true, message = "Quiz submitted successfully!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SubmitQuizAnswers Error: {ex.Message}");
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        // GET: View Classwork/Submission Results
        public ActionResult ViewClasswork(int? id, int? classworkId)
        {
            try
            {
                if (Session["Id"] == null || (string)Session["Role"] != "Student")
                {
                    TempData["ErrorMessage"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Home");
                }

                if (!id.HasValue || !classworkId.HasValue)
                {
                    TempData["ErrorMessage"] = "Invalid parameters.";
                    return RedirectToAction("Classwork", "Student", new { id = id });
                }

                int studentId = Convert.ToInt32(Session["Id"]);

                var studentCourse = db.StudentCourses
                    .Include(sc => sc.Course)
                    .Include(sc => sc.Section)
                    .Include(sc => sc.Section.Program)
                    .FirstOrDefault(sc => sc.StudentId == studentId && sc.CourseId == id.Value);

                if (studentCourse == null)
                {
                    TempData["ErrorMessage"] = "You are not enrolled in this course.";
                    return RedirectToAction("Classwork", "Student", new { id = id });
                }

                var semester = GetSemesterFromCurriculum(studentCourse.Section.ProgramId, id.Value, studentCourse.Section.YearLevel);

                var teacherCourseSectionId = db.TeacherCourseSections
                    .Where(tcs => tcs.CourseId == id.Value &&
                                 tcs.SectionId == studentCourse.SectionId &&
                                 tcs.Semester == semester)
                    .Select(tcs => tcs.Id)
                    .FirstOrDefault();

                var classwork = db.Classworks
                    .Include(c => c.ClassworkFiles)
                    .FirstOrDefault(c => c.Id == classworkId.Value &&
                                       c.TeacherCourseSectionId == teacherCourseSectionId &&
                                       c.IsActive &&
                                       !c.IsManualEntry); // Prevent access to manual entries

                if (classwork == null)
                {
                    TempData["ErrorMessage"] = "Classwork not found or not available.";
                    return RedirectToAction("Classwork", "Student", new { id = id });
                }

                var submission = db.ClassworkSubmissions
                    .FirstOrDefault(s => s.ClassworkId == classworkId.Value && s.StudentId == studentId);

                dynamic classworkData = new ExpandoObject();
                classworkData.Id = classwork.Id;
                classworkData.Title = classwork.Title;
                classworkData.Description = classwork.Description;
                classworkData.ClassworkType = classwork.ClassworkType;
                classworkData.Points = classwork.Points;
                classworkData.Deadline = classwork.Deadline;
                classworkData.QuestionsJson = classwork.QuestionsJson;

                ViewBag.Classwork = classworkData;
                ViewBag.Submission = submission;
                ViewBag.CourseId = id.Value;

                return View("~/Views/Student/Course/ViewClasswork.cshtml", studentCourse.Course);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ViewClasswork Error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return RedirectToAction("Classwork", "Student", new { id = id });
            }
        }

        // GET: Student/Todo/Finished - Show finished tasks from ALL courses
        public ActionResult Finished(int? id = null)
        {
            try
            {
                // Validate session
                if (Session["Id"] == null || (string)Session["Role"] != "Student")
                {
                    TempData["ErrorMessage"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Home");
                }

                int studentId = Convert.ToInt32(Session["Id"]);

                // Get all student's enrollments
                var allEnrollments = db.StudentCourses
                    .Include(sc => sc.Course)
                    .Include(sc => sc.Section)
                    .Include(sc => sc.Section.Program)
                    .Where(sc => sc.StudentId == studentId)
                    .ToList();

                if (!allEnrollments.Any())
                {
                    TempData["ErrorMessage"] = "You are not enrolled in any courses.";
                    return RedirectToAction("Course", "Student");
                }

                // Get all teacher course section IDs for all enrolled courses
                var allTeacherCourseSectionIds = new List<int>();
                foreach (var enrollment in allEnrollments)
                {
                    var semester = GetSemesterFromCurriculum(enrollment.Section.ProgramId, enrollment.CourseId, enrollment.Section.YearLevel);
                    var teacherCourseSectionId = db.TeacherCourseSections
                        .Where(tcs => tcs.CourseId == enrollment.CourseId &&
                                     tcs.SectionId == enrollment.SectionId &&
                                     tcs.Semester == semester)
                        .Select(tcs => tcs.Id)
                        .FirstOrDefault();

                    if (teacherCourseSectionId > 0)
                    {
                        allTeacherCourseSectionIds.Add(teacherCourseSectionId);
                    }
                }

                if (!allTeacherCourseSectionIds.Any())
                {
                    TempData["ErrorMessage"] = "No teachers assigned to your courses.";
                    return RedirectToAction("Course", "Student");
                }

                // Get finished tasks from ALL courses
                var finishedTasks = db.ClassworkSubmissions
                    .Where(s => s.StudentId == studentId && s.Status == "Graded")
                    .Join(db.Classworks, s => s.ClassworkId, c => c.Id, (s, c) => new { Submission = s, Classwork = c })
                    .Join(db.TeacherCourseSections, sc => sc.Classwork.TeacherCourseSectionId, tcs => tcs.Id, (sc, tcs) => new { sc.Submission, sc.Classwork, TeacherCourseSection = tcs })
                    .Join(db.Courses, sct => sct.TeacherCourseSection.CourseId, course => course.Id, (sct, course) => new { sct.Submission, sct.Classwork, sct.TeacherCourseSection, Course = course })
                    .Where(result => allTeacherCourseSectionIds.Contains(result.Classwork.TeacherCourseSectionId) && result.Classwork.IsActive)
                    .OrderByDescending(result => result.Submission.GradedAt ?? result.Submission.SubmittedAt)
                    .ToList()
                    .Select(result =>
                    {
                        dynamic item = new ExpandoObject();
                        item.Id = result.Classwork.Id;
                        item.Title = result.Classwork.Title;
                        item.ClassworkType = result.Classwork.ClassworkType;
                        item.Points = result.Classwork.Points;
                        item.Grade = result.Submission.Grade;
                        item.Feedback = result.Submission.Feedback;
                        item.Deadline = result.Classwork.Deadline;
                        item.DateCreated = result.Classwork.DateCreated;
                        item.SubmittedAt = result.Submission.SubmittedAt;
                        item.GradedAt = result.Submission.GradedAt;
                        item.SubmissionStatus = result.Submission.Status;
                        item.CourseId = result.Course.Id;
                        item.CourseTitle = result.Course.CourseTitle;
                        item.CourseCode = result.Course.CourseCode;
                        
                        // Calculate percentage
                        if (result.Classwork.Points > 0 && result.Submission.Grade.HasValue)
                        {
                            item.Percentage = Math.Round((result.Submission.Grade.Value / result.Classwork.Points) * 100, 1);
                        }
                        else
                        {
                            item.Percentage = 0;
                        }
                        
                        return item;
                    })
                    .ToList();

                // Set ViewBag properties
                ViewBag.FinishedTasks = finishedTasks;
                ViewBag.CourseId = id ?? (allEnrollments.FirstOrDefault()?.CourseId ?? 0);
                ViewBag.ActiveTab = "Finished";
                ViewBag.ShowingAllCourses = true; // Flag to indicate we're showing all courses

                // Use the first course for header display, but we'll modify the view to show it's all courses
                var firstCourse = allEnrollments.FirstOrDefault()?.Course;
                if (firstCourse == null)
                {
                    TempData["ErrorMessage"] = "No course data found.";
                    return RedirectToAction("Course", "Student");
                }

                // Set section name to indicate all courses
                ViewBag.SectionName = "All Enrolled Courses";

                return View("~/Views/Student/Todo/Finished.cshtml", firstCourse);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Finished Error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading finished tasks: " + ex.Message;
                return RedirectToAction("Course", "Student");
            }
        }

        // GET: Student/Todo/Assigned - Show assigned tasks from ALL courses
        public ActionResult Assigned(int? id = null)
        {
            try
            {
                // Validate session
                if (Session["Id"] == null || (string)Session["Role"] != "Student")
                {
                    TempData["ErrorMessage"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Home");
                }

                int studentId = Convert.ToInt32(Session["Id"]);

                // Get all student's enrollments
                var allEnrollments = db.StudentCourses
                    .Include(sc => sc.Course)
                    .Include(sc => sc.Section)
                    .Include(sc => sc.Section.Program)
                    .Where(sc => sc.StudentId == studentId)
                    .ToList();

                if (!allEnrollments.Any())
                {
                    TempData["ErrorMessage"] = "You are not enrolled in any courses.";
                    return RedirectToAction("Course", "Student");
                }

                // Get all teacher course section IDs for all enrolled courses
                var allTeacherCourseSectionIds = new List<int>();
                foreach (var enrollment in allEnrollments)
                {
                    var semester = GetSemesterFromCurriculum(enrollment.Section.ProgramId, enrollment.CourseId, enrollment.Section.YearLevel);
                    var teacherCourseSectionId = db.TeacherCourseSections
                        .Where(tcs => tcs.CourseId == enrollment.CourseId &&
                                     tcs.SectionId == enrollment.SectionId &&
                                     tcs.Semester == semester)
                        .Select(tcs => tcs.Id)
                        .FirstOrDefault();

                    if (teacherCourseSectionId > 0)
                    {
                        allTeacherCourseSectionIds.Add(teacherCourseSectionId);
                    }
                }

                if (!allTeacherCourseSectionIds.Any())
                {
                    TempData["ErrorMessage"] = "No teachers assigned to your courses.";
                    return RedirectToAction("Course", "Student");
                }

                var now = DateTime.Now;
                
                // Get assigned tasks from ALL courses
                var assignedTasks = db.Classworks
                    .Where(c => allTeacherCourseSectionIds.Contains(c.TeacherCourseSectionId) && c.IsActive)
                    .Where(c => !c.IsScheduled || (c.ScheduledPublishDate.HasValue && c.ScheduledPublishDate.Value <= now))
                    .Where(c => !c.IsManualEntry) // Exclude manual entries from student todo lists
                    .Join(db.TeacherCourseSections, c => c.TeacherCourseSectionId, tcs => tcs.Id, (c, tcs) => new { Classwork = c, TeacherCourseSection = tcs })
                    .Join(db.Courses, ct => ct.TeacherCourseSection.CourseId, course => course.Id, (ct, course) => new { ct.Classwork, ct.TeacherCourseSection, Course = course })
                    .OrderByDescending(result => result.Classwork.DateCreated)
                    .ToList()
                    .Select(result =>
                    {
                        // Get student's submission for this classwork
                        var submission = db.ClassworkSubmissions
                            .FirstOrDefault(s => s.ClassworkId == result.Classwork.Id && s.StudentId == studentId);

                        dynamic item = new ExpandoObject();
                        item.Id = result.Classwork.Id;
                        item.Title = result.Classwork.Title;
                        item.ClassworkType = result.Classwork.ClassworkType;
                        item.Points = result.Classwork.Points;
                        item.Deadline = result.Classwork.Deadline;
                        item.DateCreated = result.Classwork.DateCreated;
                        item.Description = result.Classwork.Description;
                        item.SubmissionStatus = submission?.Status ?? "Not Submitted";
                        item.SubmittedAt = submission?.SubmittedAt;
                        item.Grade = submission?.Grade;
                        item.Feedback = submission?.Feedback;
                        item.GradedAt = submission?.GradedAt;
                        item.CourseId = result.Course.Id;
                        item.CourseTitle = result.Course.CourseTitle;
                        item.CourseCode = result.Course.CourseCode;
                        
                        return item;
                    })
                    .ToList();

                // Set ViewBag properties
                ViewBag.AssignedTasks = assignedTasks;
                ViewBag.CourseId = id ?? (allEnrollments.FirstOrDefault()?.CourseId ?? 0);
                ViewBag.ActiveTab = "Assigned";
                ViewBag.ShowingAllCourses = true; // Flag to indicate we're showing all courses

                // Use the first course for header display, but we'll modify the view to show it's all courses
                var firstCourse = allEnrollments.FirstOrDefault()?.Course;
                if (firstCourse == null)
                {
                    TempData["ErrorMessage"] = "No course data found.";
                    return RedirectToAction("Course", "Student");
                }

                // Set section name to indicate all courses
                ViewBag.SectionName = "All Enrolled Courses";

                return View("~/Views/Student/Todo/Assigned.cshtml", firstCourse);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Assigned Error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading assigned tasks: " + ex.Message;
                return RedirectToAction("Course", "Student");
            }
        }

        // GET: Student/Todo/Missing - Show missing tasks from ALL courses
        public ActionResult Missing(int? id = null)
        {
            try
            {
                // Validate session
                if (Session["Id"] == null || (string)Session["Role"] != "Student")
                {
                    TempData["ErrorMessage"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Home");
                }

                int studentId = Convert.ToInt32(Session["Id"]);
                var user = db.Users.Find(studentId);

                // Get all student's enrollments
                var allEnrollments = db.StudentCourses
                    .Include(sc => sc.Course)
                    .Include(sc => sc.Section)
                    .Include(sc => sc.Section.Program)
                    .Where(sc => sc.StudentId == studentId)
                    .ToList();

                if (!allEnrollments.Any())
                {
                    TempData["ErrorMessage"] = "You are not enrolled in any courses.";
                    return RedirectToAction("Course", "Student");
                }

                // Get all teacher course section IDs for all enrolled courses
                var allTeacherCourseSectionIds = new List<int>();
                foreach (var enrollment in allEnrollments)
                {
                    var semester = GetSemesterFromCurriculum(enrollment.Section.ProgramId, enrollment.CourseId, enrollment.Section.YearLevel);
                    var teacherCourseSectionId = db.TeacherCourseSections
                        .Where(tcs => tcs.CourseId == enrollment.CourseId &&
                                     tcs.SectionId == enrollment.SectionId &&
                                     tcs.Semester == semester)
                        .Select(tcs => tcs.Id)
                        .FirstOrDefault();

                    if (teacherCourseSectionId > 0)
                    {
                        allTeacherCourseSectionIds.Add(teacherCourseSectionId);
                    }
                }

                if (!allTeacherCourseSectionIds.Any())
                {
                    TempData["ErrorMessage"] = "No teachers assigned to your courses.";
                    return RedirectToAction("Course", "Student");
                }

                var now = DateTime.Now;
                
                // Get missing tasks from ALL courses
                var missingTasks = db.Classworks
                    .Where(c => allTeacherCourseSectionIds.Contains(c.TeacherCourseSectionId) && c.IsActive)
                    .Where(c => !c.IsScheduled || (c.ScheduledPublishDate.HasValue && c.ScheduledPublishDate.Value <= now))
                    .Where(c => !c.IsManualEntry) // Exclude manual entries from student todo lists
                    .Join(db.TeacherCourseSections, c => c.TeacherCourseSectionId, tcs => tcs.Id, (c, tcs) => new { Classwork = c, TeacherCourseSection = tcs })
                    .Join(db.Courses, ct => ct.TeacherCourseSection.CourseId, course => course.Id, (ct, course) => new { ct.Classwork, ct.TeacherCourseSection, Course = course })
                    .OrderBy(result => result.Classwork.Deadline)
                    .ToList()
                    .Select(result =>
                    {
                        // Get student's submission for this classwork
                        var submission = db.ClassworkSubmissions
                            .FirstOrDefault(s => s.ClassworkId == result.Classwork.Id && s.StudentId == studentId);

                        dynamic item = new ExpandoObject();
                        item.Id = result.Classwork.Id;
                        item.Title = result.Classwork.Title;
                        item.ClassworkType = result.Classwork.ClassworkType;
                        item.Points = result.Classwork.Points;
                        item.Deadline = result.Classwork.Deadline;
                        item.DateCreated = result.Classwork.DateCreated;
                        item.Description = result.Classwork.Description;
                        item.SubmissionStatus = submission?.Status ?? "Not Submitted";
                        item.SubmittedAt = submission?.SubmittedAt;
                        item.Grade = submission?.Grade;
                        item.Feedback = submission?.Feedback;
                        item.GradedAt = submission?.GradedAt;
                        item.CourseId = result.Course.Id;
                        item.CourseTitle = result.Course.CourseTitle;
                        item.CourseCode = result.Course.CourseCode;
                        
                        return item;
                    })
                    .Where(item =>
                    {
                        // Filter for missing tasks: not submitted and overdue
                        var notSubmitted = item.SubmissionStatus == "Not Submitted";
                        var isOverdue = item.Deadline != null && (DateTime)item.Deadline < now;
                        
                        return notSubmitted && isOverdue;
                    })
                    .ToList();

                // Set ViewBag properties
                ViewBag.MissingTasks = missingTasks;
                ViewBag.CourseId = id ?? (allEnrollments.FirstOrDefault()?.CourseId ?? 0);
                ViewBag.ActiveTab = "Missing";
                ViewBag.ShowingAllCourses = true; // Flag to indicate we're showing all courses

                // Use the first course for header display, but we'll modify the view to show it's all courses
                var firstCourse = allEnrollments.FirstOrDefault()?.Course;
                if (firstCourse == null)
                {
                    TempData["ErrorMessage"] = "No course data found.";
                    return RedirectToAction("Course", "Student");
                }

                // Set section name to indicate all courses
                ViewBag.SectionName = "All Enrolled Courses";

                return View("~/Views/Student/Todo/Missing.cshtml", firstCourse);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Missing Error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading missing tasks: " + ex.Message;
                return RedirectToAction("Course", "Student");
            }
        }
    }
}