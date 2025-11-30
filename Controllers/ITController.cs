using System;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Collections.Generic;
using LMS.Models;

namespace LMS.Controllers
{
    public class ITController : Controller
    {
        private readonly LMSContext db = new LMSContext();


        // GET: IT
        public ActionResult Index()
        {
            return View();
        }

        // GET: IT/GetDashboardStats - AJAX endpoint for dashboard statistics
        [HttpGet]
        public JsonResult GetDashboardStats()
        {
            try
            {
                var totalCourses = db.Courses.Count();
                var totalAssignments = db.TeacherCourseSections.Count();

                var unassignedCount = db.CurriculumCourses
                    .Include(cc => cc.Course)
                    .ToList()
                    .Sum(cc =>
                    {
                        var sections = db.Sections
                            .Where(s => s.ProgramId == cc.ProgramId && s.YearLevel == cc.YearLevel)
                            .ToList();

                        return sections.Count(section =>
                            !db.TeacherCourseSections.Any(tcs =>
                                tcs.CourseId == cc.CourseId &&
                                tcs.SectionId == section.Id &&
                                tcs.Semester == cc.Semester)
                        );
                    });

                return Json(new
                {
                    success = true,
                    totalCourses = totalCourses,
                    totalAssignments = totalAssignments,
                    unassignedCourses = unassignedCount
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetDashboardStats Error: {ex.Message}");
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: IT/GetRecentActivities - AJAX endpoint for recent activities
        [HttpGet]
        public JsonResult GetRecentActivities()
        {
            try
            {
                var activities = new List<object>();

                var recentCourses = db.Courses
                    .OrderByDescending(c => c.DateCreated)
                    .Take(2)
                    .ToList()
                    .Select(c => new
                    {
                        type = "Course",
                        message = $"New course created: <strong>{c.CourseCode} - {c.CourseTitle}</strong>",
                        timestamp = c.DateCreated
                    });

                activities.AddRange(recentCourses);

                var recentAssignments = db.TeacherCourseSections
                    .Include(tcs => tcs.Teacher)
                    .Include(tcs => tcs.Course)
                    .Include(tcs => tcs.Section)
                    .Include(tcs => tcs.Section.Program)
                    .OrderByDescending(tcs => tcs.DateAssigned)
                    .Take(2)
                    .ToList()
                    .Select(tcs => new
                    {
                        type = "Assignment",
                        message = $"<strong>{tcs.Teacher.FirstName} {tcs.Teacher.LastName}</strong> assigned to {tcs.Course.CourseCode} ({tcs.Section.Program.ProgramCode} {tcs.Section.YearLevel}-{tcs.Section.SectionName})",
                        timestamp = tcs.DateAssigned
                    });

                activities.AddRange(recentAssignments);

                var recentEnrollments = db.StudentCourses
                    .Include(sc => sc.Student)
                    .Include(sc => sc.Course)
                    .OrderByDescending(sc => sc.DateEnrolled)
                    .Take(2)
                    .ToList()
                    .Select(sc => new
                    {
                        type = "Enrollment",
                        message = $"Student <strong>{sc.Student.FirstName} {sc.Student.LastName}</strong> enrolled in {sc.Course.CourseCode}",
                        timestamp = sc.DateEnrolled
                    });

                activities.AddRange(recentEnrollments);

                var sortedActivities = activities
                    .OrderByDescending(a => ((dynamic)a).timestamp)
                    .Take(4)
                    .ToList();

                return Json(new
                {
                    success = true,
                    activities = sortedActivities
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetRecentActivities Error: {ex.Message}");
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: IT/GetRecentTeacherAssignments - AJAX endpoint for recent teacher assignments
        [HttpGet]
        public JsonResult GetRecentTeacherAssignments()
        {
            try
            {
                var recentAssignments = db.TeacherCourseSections
                    .Include(tcs => tcs.Teacher)
                    .Include(tcs => tcs.Course)
                    .Include(tcs => tcs.Section)
                    .Include(tcs => tcs.Section.Program)
                    .OrderByDescending(tcs => tcs.DateAssigned)
                    .Take(5)
                    .ToList()
                    .Select(tcs => new
                    {
                        teacherId = tcs.TeacherId,
                        teacherName = $"{tcs.Teacher.FirstName} {tcs.Teacher.LastName}",
                        teacherEmail = tcs.Teacher.Email,
                        courseId = tcs.CourseId,
                        courseCode = tcs.Course.CourseCode,
                        courseTitle = tcs.Course.CourseTitle,
                        sectionId = tcs.SectionId,
                        sectionName = tcs.Section.SectionName,
                        programCode = tcs.Section.Program.ProgramCode,
                        yearLevel = tcs.Section.YearLevel,
                        semester = tcs.Semester,
                        fullSectionName = $"{tcs.Section.Program.ProgramCode} {tcs.Section.YearLevel}-{tcs.Section.SectionName}",
                        dateAssigned = tcs.DateAssigned,
                        studentCount = db.StudentCourses.Count(sc => sc.SectionId == tcs.SectionId && sc.CourseId == tcs.CourseId)
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    assignments = recentAssignments
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetRecentTeacherAssignments Error: {ex.Message}");
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Course()

        {
            //if (Session["Id"] == null || (string)Session["Role"] != "IT")
            //{
            //    TempData["ErrorMessage"] = "Session expired. Please log in again.";
            //    return RedirectToAction("Login", "Home");
            //}

            var courses = db.Courses
                .OrderByDescending(c => c.CourseTitle)
                .ToList();

            return View(courses);
        }

        // GET: IT/CreateCourse
        public ActionResult CreateCourse()
        {
            ViewBag.Programs = db.Programs.ToList();
            ViewBag.Departments = db.Departments.ToList();

            return View(new Course());
        }
        public ActionResult EditCourse()
        {
            return View();
        }

        // GET: IT/AssignedCourses
        public ActionResult AssignedCourses()
        {
            try
            {
                var curriculumGroups = db.CurriculumCourses
                    .Include(cc => cc.Program)
                    .Include(cc => cc.Program.Department)
                    .Include(cc => cc.Course)
                    .ToList()
                    .GroupBy(cc => new
                    {
                        cc.ProgramId,
                        cc.YearLevel,
                        cc.Semester,
                        ProgramName = cc.Program.ProgramName,
                        ProgramCode = cc.Program.ProgramCode
                    })
                    .Select(g => new CurriculumGroupViewModel
                    {
                        ProgramId = g.Key.ProgramId,
                        YearLevel = g.Key.YearLevel,
                        Semester = g.Key.Semester,
                        ProgramName = FormatProgramName(g.Key.ProgramName),
                        ProgramCode = g.Key.ProgramCode,
                        Courses = g.Select(cc => cc.Course).ToList(),
                        CourseCount = g.Count()
                    })
                    .ToList();

                return View(curriculumGroups);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AssignedCourses Error: {ex.Message}");
                TempData["ErrorMessage"] = "Error loading curriculum courses: " + ex.Message;
                return View(new List<CurriculumGroupViewModel>());
            }
        }

        private string FormatProgramName(string programName)
        {
            if (string.IsNullOrEmpty(programName))
                return programName;

            programName = programName.Replace("Bachelor of Science in ", "BS in ");
            programName = programName.Replace("Bachelor of Arts in ", "BA in ");
            programName = programName.Replace("Bachelor of ", "B ");
            programName = programName.Replace("Master of Science in ", "MS in ");
            programName = programName.Replace("Master of Arts in ", "MA in ");
            programName = programName.Replace("Master of ", "M ");

            return programName;
        }

        public ActionResult AssignCourse()
        {
            ViewBag.Programs = db.Programs.ToList();
            ViewBag.Departments = db.Departments.ToList();

            var existingCombinations = db.CurriculumCourses
                .Select(cc => new
                {
                    cc.ProgramId,
                    cc.YearLevel,
                    cc.Semester
                })
                .Distinct()
                .ToList();

            ViewBag.ExistingCombinations = existingCombinations;

            return View();
        }

        // GET: IT/EditAssignedCourses
        public ActionResult EditAssignedCourses(int programId, int yearLevel, int semester)
        {
            try
            {
                // Get program details
                var program = db.Programs.Find(programId);
                if (program == null)
                {
                    TempData["ErrorMessage"] = "Program not found.";
                    return RedirectToAction("AssignedCourses");
                }

                // Get assigned courses for this curriculum
                var assignedCourses = db.CurriculumCourses
                    .Where(cc => cc.ProgramId == programId && cc.YearLevel == yearLevel && cc.Semester == semester)
                    .Include(cc => cc.Course)
                    .Select(cc => cc.Course)
                    .ToList();

                // Set ViewBag data
                ViewBag.ProgramId = programId;
                ViewBag.ProgramCode = program.ProgramCode;
                ViewBag.ProgramName = program.ProgramName;
                ViewBag.YearLevel = yearLevel;
                ViewBag.Semester = semester;
                ViewBag.AssignedCourses = assignedCourses;

                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EditAssignedCourses Error: {ex.Message}");
                TempData["ErrorMessage"] = "Error loading curriculum: " + ex.Message;
                return RedirectToAction("AssignedCourses");
            }
        }

        // GET: IT/GetExistingCurriculumCombinations - AJAX endpoint for checking existing curriculum combinations
        [HttpGet]
        public JsonResult GetExistingCurriculumCombinations()
        {
            try
            {
                var existingCombinations = db.CurriculumCourses
                    .Select(cc => new
                    {
                        programId = cc.ProgramId,
                        yearLevel = cc.YearLevel,
                        semester = cc.Semester
                    })
                    .Distinct()
                    .ToList();

                return Json(existingCombinations, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetExistingCurriculumCombinations Error: {ex.Message}");
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult ManageStudentCourses()
        {
            return View();
        }

        // 🔥 NEW: Search Students for ManageStudentCourses
        [HttpGet]
        public JsonResult SearchStudents(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return Json(new { success = true, students = new object[0] }, JsonRequestBehavior.AllowGet);
                }

                var searchTerm = query.Trim().ToLower();

                var students = db.Users
                    .Where(u => u.Role == "Student" &&
                               (u.FirstName.ToLower().Contains(searchTerm) ||
                                u.LastName.ToLower().Contains(searchTerm) ||
                                u.Email.ToLower().Contains(searchTerm) ||
                                u.UserID.ToLower().Contains(searchTerm)))
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .Take(20)
                    .ToList();

                var result = students.Select(student =>
                {
                    // Get student's section information
                    var studentSection = db.StudentCourses
                        .Include(sc => sc.Section)
                        .Include(sc => sc.Section.Program)
                        .Where(sc => sc.StudentId == student.Id)
                        .FirstOrDefault();

                    return new
                    {
                        id = student.Id,
                        firstName = student.FirstName,
                        lastName = student.LastName,
                        studentId = student.UserID,
                        email = student.Email,
                        program = studentSection?.Section?.Program?.ProgramName ?? "N/A",
                        programCode = studentSection?.Section?.Program?.ProgramCode ?? "N/A",
                        yearLevel = studentSection?.Section?.YearLevel ?? 0,
                        section = studentSection?.Section?.SectionName ?? "N/A"
                    };
                }).ToList();

                return Json(new { success = true, students = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SearchStudents Error: {ex.Message}");
                return Json(new { success = false, message = "Error searching students: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: IT/GetCurriculumCourses - AJAX endpoint for modal
        [HttpGet]
        public JsonResult GetCurriculumCourses(int programId, int yearLevel, int semester)
        {
            try
            {
                var courses = db.CurriculumCourses
                    .Where(cc => cc.ProgramId == programId && cc.YearLevel == yearLevel && cc.Semester == semester)
                    .Include(cc => cc.Course)
                    .Select(cc => new
                    {
                        id = cc.Course.Id,
                        code = cc.Course.CourseCode,
                        title = cc.Course.CourseTitle
                    })
                    .ToList();

                return Json(courses, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetCurriculumCourses Error: {ex.Message}");
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        public ActionResult AssignTeacher()
        {
            return View();
        }

        public new ActionResult Profile()
        {
            return View();
        }



        // GET: IT/GetTeachers - Get all teachers for dropdown
        [HttpGet]
        public JsonResult GetTeachers()
        {
            try
            {
                var teachers = db.Users
                    .Where(u => u.Role == "Teacher")
                    .OrderBy(u => u.LastName)
                    .Select(u => new
                    {
                        id = u.Id,
                        name = u.FirstName + " " + u.LastName,
                        email = u.Email,
                        userId = u.UserID
                    })
                    .ToList();

                return Json(new { success = true, teachers = teachers }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetTeachers Error: {ex.Message}");
                return Json(new { success = false, message = "Error loading teachers: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: IT/GetTeacherAssignments - Get all teacher assignments
        [HttpGet]
        public JsonResult GetTeacherAssignments(int? programId = null, int? yearLevel = null, int? semester = null)
        {
            try
            {
                var query = db.TeacherCourseSections
                    .Include(tcs => tcs.Teacher)
                    .Include(tcs => tcs.Course)
                    .Include(tcs => tcs.Section)
                    .Include(tcs => tcs.Section.Program)
                    .AsQueryable();

                if (programId.HasValue)
                {
                    query = query.Where(tcs => tcs.Section.ProgramId == programId.Value);
                }

                if (yearLevel.HasValue)
                {
                    query = query.Where(tcs => tcs.Section.YearLevel == yearLevel.Value);
                }

                if (semester.HasValue)
                {
                    query = query.Where(tcs => tcs.Semester == semester.Value);
                }

                var assignments = query
                    .OrderByDescending(tcs => tcs.DateAssigned)
                    .ToList()
                    .Select(tcs => new
                    {
                        id = tcs.Id,
                        teacherId = tcs.TeacherId,
                        teacherName = tcs.Teacher.FirstName + " " + tcs.Teacher.LastName,
                        teacherEmail = tcs.Teacher.Email,
                        courseId = tcs.CourseId,
                        courseCode = tcs.Course.CourseCode,
                        courseTitle = tcs.Course.CourseTitle,
                        sectionId = tcs.SectionId,
                        sectionName = tcs.Section.SectionName,
                        programCode = tcs.Section.Program.ProgramCode,
                        programId = tcs.Section.ProgramId,
                        yearLevel = tcs.Section.YearLevel,
                        semester = tcs.Semester,
                        semesterName = GetSemesterName(tcs.Semester),
                        fullSectionName = $"{tcs.Section.Program.ProgramCode} {tcs.Section.YearLevel}-{tcs.Section.SectionName}",
                        dateAssigned = tcs.DateAssigned.ToString("yyyy-MM-dd HH:mm:ss"),
                        studentCount = db.StudentCourses.Count(sc => sc.SectionId == tcs.SectionId && sc.CourseId == tcs.CourseId)
                    })
                    .ToList();

                return Json(new { success = true, assignments = assignments }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetTeacherAssignments Error: {ex.Message}");
                return Json(new { success = false, message = "Error loading assignments: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: IT/AssignTeacherToCourse
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult AssignTeacherToCourse(int teacherId, int courseId, int sectionId, int semester, string remarks = null)
        {
            try
            {
                if (teacherId <= 0 || courseId <= 0 || sectionId <= 0 || semester <= 0)
                {
                    return Json(new { success = false, message = "Invalid input. Please fill all required fields." });
                }

                var teacher = db.Users.FirstOrDefault(u => u.Id == teacherId && u.Role == "Teacher");
                if (teacher == null)
                {
                    return Json(new { success = false, message = "Teacher not found." });
                }

                var course = db.Courses.Find(courseId);
                if (course == null)
                {
                    return Json(new { success = false, message = "Course not found." });
                }

                var section = db.Sections.Include(s => s.Program).FirstOrDefault(s => s.Id == sectionId);
                if (section == null)
                {
                    return Json(new { success = false, message = "Section not found." });
                }

                var existingCourseAssignment = db.TeacherCourseSections
                    .FirstOrDefault(tcs => tcs.CourseId == courseId 
                                        && tcs.SectionId == sectionId 
                                        && tcs.Semester == semester);

                if (existingCourseAssignment != null)
                {
                    var assignedTeacher = db.Users.Find(existingCourseAssignment.TeacherId);
                    return Json(new { 
                        success = false, 
                        message = $"{course.CourseCode} for {section.Program.ProgramCode} {section.YearLevel}-{section.SectionName} is already assigned to {assignedTeacher.FirstName} {assignedTeacher.LastName}. A course-section can only have one teacher." 
                    });
                }

                var existingTeacherAssignment = db.TeacherCourseSections
                    .FirstOrDefault(tcs => tcs.TeacherId == teacherId 
                                        && tcs.CourseId == courseId 
                                        && tcs.SectionId == sectionId 
                                        && tcs.Semester == semester);

                if (existingTeacherAssignment != null)
                {
                    return Json(new { 
                        success = false, 
                        message = $"This teacher is already assigned to {course.CourseCode} for {section.Program.ProgramCode} {section.YearLevel}-{section.SectionName}." 
                    });
                }

                var assignment = new TeacherCourseSection
                {
                    TeacherId = teacherId,
                    CourseId = courseId,
                    SectionId = sectionId,
                    Semester = semester,
                    DateAssigned = DateTime.Now
                };

                db.TeacherCourseSections.Add(assignment);
                db.SaveChanges();

                // Send email notifications to students in the section about the new teacher assignment
                LMS.Helpers.StudentNotificationService.NotifyTeacherAssigned(assignment.Id);

                var studentCount = db.StudentCourses.Count(sc => sc.SectionId == sectionId && sc.CourseId == courseId);

                return Json(new
                {
                    success = true,
                    message = "Teacher assigned successfully!",
                    data = new
                    {
                        id = assignment.Id,
                        teacherId = teacher.Id,
                        teacherName = $"{teacher.FirstName} {teacher.LastName}",
                        teacherEmail = teacher.Email,
                        courseId = course.Id,
                        courseCode = course.CourseCode,
                        courseTitle = course.CourseTitle,
                        sectionId = section.Id,
                        sectionName = section.SectionName,
                        programCode = section.Program.ProgramCode,
                        programId = section.ProgramId,
                        yearLevel = section.YearLevel,
                        semester = semester,
                        semesterName = GetSemesterName(semester),
                        fullSectionName = $"{section.Program.ProgramCode} {section.YearLevel}-{section.SectionName}",
                        dateAssigned = assignment.DateAssigned.ToString("yyyy-MM-dd HH:mm:ss"),
                        studentCount = studentCount
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AssignTeacherToCourse Error: {ex.Message}");
                return Json(new { success = false, message = "Error assigning teacher: " + ex.Message });
            }
        }

        // POST: IT/ReassignTeacher
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ReassignTeacher(int assignmentId, int teacherId, int courseId, int sectionId, int semester, string remarks = null)
        {
            try
            {
                if (assignmentId <= 0 || teacherId <= 0 || courseId <= 0 || sectionId <= 0 || semester <= 0)
                {
                    return Json(new { success = false, message = "Invalid input. Please fill all required fields." });
                }

                var assignment = db.TeacherCourseSections
                    .Include(tcs => tcs.Teacher)
                    .Include(tcs => tcs.Course)
                    .Include(tcs => tcs.Section)
                    .Include(tcs => tcs.Section.Program)
                    .FirstOrDefault(tcs => tcs.Id == assignmentId);

                if (assignment == null)
                {
                    return Json(new { success = false, message = "Assignment not found." });
                }

                var newTeacher = db.Users.FirstOrDefault(u => u.Id == teacherId && u.Role == "Teacher");
                if (newTeacher == null)
                {
                    return Json(new { success = false, message = "Teacher not found." });
                }

                var course = db.Courses.Find(courseId);
                if (course == null)
                {
                    return Json(new { success = false, message = "Course not found." });
                }

                var section = db.Sections.Include(s => s.Program).FirstOrDefault(s => s.Id == sectionId);
                if (section == null)
                {
                    return Json(new { success = false, message = "Section not found." });
                }

                var oldTeacherName = $"{assignment.Teacher.FirstName} {assignment.Teacher.LastName}";
                var newTeacherName = $"{newTeacher.FirstName} {newTeacher.LastName}";

                assignment.TeacherId = teacherId;
                assignment.CourseId = courseId;
                assignment.SectionId = sectionId;
                assignment.Semester = semester;
                assignment.DateAssigned = DateTime.Now;

                db.SaveChanges();

                var studentCount = db.StudentCourses.Count(sc => sc.SectionId == sectionId && sc.CourseId == courseId);

                var message = oldTeacherName == newTeacherName 
                    ? "Assignment updated successfully!"
                    : $"Successfully reassigned from {oldTeacherName} to {newTeacherName}!";

                return Json(new
                {
                    success = true,
                    message = message,
                    data = new
                    {
                        id = assignment.Id,
                        teacherId = newTeacher.Id,
                        teacherName = newTeacherName,
                        teacherEmail = newTeacher.Email,
                        courseId = course.Id,
                        courseCode = course.CourseCode,
                        courseTitle = course.CourseTitle,
                        sectionId = section.Id,
                        sectionName = section.SectionName,
                        programCode = section.Program.ProgramCode,
                        programId = section.ProgramId,
                        yearLevel = section.YearLevel,
                        semester = semester,
                        semesterName = GetSemesterName(semester),
                        fullSectionName = $"{section.Program.ProgramCode} {section.YearLevel}-{section.SectionName}",
                        dateAssigned = assignment.DateAssigned.ToString("yyyy-MM-dd HH:mm:ss"),
                        studentCount = studentCount
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ReassignTeacher Error: {ex.Message}");
                return Json(new { success = false, message = "Error reassigning teacher: " + ex.Message });
            }
        }

        // GET: IT/GetCoursesForSection
        [HttpGet]
        public JsonResult GetCoursesForSection(int programId, int yearLevel, int semester)
        {
            try
            {
                var courses = db.CurriculumCourses
                    .Where(cc => cc.ProgramId == programId && cc.YearLevel == yearLevel && cc.Semester == semester)
                    .Include(cc => cc.Course)
                    .Select(cc => new
                    {
                        id = cc.Course.Id,
                        code = cc.Course.CourseCode,
                        title = cc.Course.CourseTitle,
                        description = cc.Course.Description
                    })
                    .OrderBy(c => c.code)
                    .ToList();

                return Json(new { success = true, courses = courses }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetCoursesForSection Error: {ex.Message}");
                return Json(new { success = false, message = "Error loading courses: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: IT/GetAllCourseSections - Get all course-section combinations with assignment status
        [HttpGet]
        public JsonResult GetAllCourseSections(int? programId = null, int? yearLevel = null, int? semester = null)
        {
            try
            {
                var query = db.CurriculumCourses
                    .Include(cc => cc.Course)
                    .Include(cc => cc.Program)
                    .AsQueryable();

                if (programId.HasValue)
                {
                    query = query.Where(cc => cc.ProgramId == programId.Value);
                }

                if (yearLevel.HasValue)
                {
                    query = query.Where(cc => cc.YearLevel == yearLevel.Value);
                }

                if (semester.HasValue)
                {
                    query = query.Where(cc => cc.Semester == semester.Value);
                }

                var curriculumCourses = query.ToList();

                var allCourseSections = new List<object>();

                foreach (var cc in curriculumCourses)
                {
                    var sections = db.Sections
                        .Where(s => s.ProgramId == cc.ProgramId && s.YearLevel == cc.YearLevel)
                        .ToList();

                    foreach (var section in sections)
                    {
                        var assignment = db.TeacherCourseSections
                            .Include(tcs => tcs.Teacher)
                            .FirstOrDefault(tcs => tcs.CourseId == cc.CourseId 
                                                && tcs.SectionId == section.Id 
                                                && tcs.Semester == cc.Semester);

                        var studentCount = db.StudentCourses
                            .Count(sc => sc.SectionId == section.Id && sc.CourseId == cc.CourseId);

                        if (assignment != null)
                        {
                            allCourseSections.Add(new
                            {
                                id = assignment.Id,
                                courseId = cc.CourseId,
                                courseCode = cc.Course.CourseCode,
                                courseTitle = cc.Course.CourseTitle,
                                sectionId = section.Id,
                                sectionName = section.SectionName,
                                programCode = cc.Program.ProgramCode,
                                programId = cc.ProgramId,
                                yearLevel = cc.YearLevel,
                                semester = cc.Semester,
                                semesterName = GetSemesterName(cc.Semester),
                                fullSectionName = $"{cc.Program.ProgramCode} {cc.YearLevel}-{section.SectionName}",
                                isAssigned = true,
                                teacherId = assignment.TeacherId,
                                teacherName = $"{assignment.Teacher.FirstName} {assignment.Teacher.LastName}",
                                teacherEmail = assignment.Teacher.Email,
                                dateAssigned = assignment.DateAssigned.ToString("yyyy-MM-dd HH:mm:ss"),
                                studentCount = studentCount
                            });
                        }
                        else
                        {
                            // Unassigned - no teacher info
                            allCourseSections.Add(new
                            {
                                id = (int?)null,
                                courseId = cc.CourseId,
                                courseCode = cc.Course.CourseCode,
                                courseTitle = cc.Course.CourseTitle,
                                sectionId = section.Id,
                                sectionName = section.SectionName,
                                programCode = cc.Program.ProgramCode,
                                programId = cc.ProgramId,
                                yearLevel = cc.YearLevel,
                                semester = cc.Semester,
                                semesterName = GetSemesterName(cc.Semester),
                                fullSectionName = $"{cc.Program.ProgramCode} {cc.YearLevel}-{section.SectionName}",
                                isAssigned = false,
                                teacherId = (int?)null,
                                teacherName = (string)null,
                                teacherEmail = (string)null,
                                dateAssigned = (string)null,
                                studentCount = studentCount
                            });
                        }
                    }
                }

                return Json(new 
                { 
                    success = true, 
                    courseSections = allCourseSections.OrderByDescending(cs => ((dynamic)cs).isAssigned)
                                                      .ThenBy(cs => ((dynamic)cs).programCode)
                                                      .ThenBy(cs => ((dynamic)cs).yearLevel)
                                                      .ThenBy(cs => ((dynamic)cs).courseCode)
                                                      .ToList()
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAllCourseSections Error: {ex.Message}");
                return Json(new { success = false, message = "Error loading course sections: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // Helper method to get semester name
        private string GetSemesterName(int semester)
        {
            switch (semester)
            {
                case 1: return "1st Semester";
                case 2: return "2nd Semester";
                case 3: return "Summer";
                default: return $"Semester {semester}";
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
    }
}