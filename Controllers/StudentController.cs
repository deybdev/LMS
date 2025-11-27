using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Dynamic;
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
                        var classworks = db.Classworks
                            .Where(c => c.TeacherCourseSectionId == teacherCourseSectionId && c.IsActive)
                            .Where(c => !c.IsScheduled || (c.ScheduledPublishDate.HasValue && c.ScheduledPublishDate.Value <= now))
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
            return View();
        }

        public ActionResult Notification()
        {
            return View();
        }

        public new ActionResult Profile()
        {
            return View();
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
    }
}