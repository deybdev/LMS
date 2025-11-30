using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using LMS.Models;

namespace LMS.Helpers
{
    public static class StudentNotificationService
    {
        /// <summary>
        /// Send material posted notification to all students in the course section
        /// </summary>
        public static void NotifyMaterialPosted(int teacherCourseSectionId, int materialId)
        {
            try
            {
                using (var db = new LMSContext())
                {
                    // Get the material with related data
                    var material = db.Materials
                        .Include(m => m.TeacherCourseSection.Course)
                        .Include(m => m.TeacherCourseSection.Section)
                        .Include(m => m.TeacherCourseSection.Teacher)
                        .FirstOrDefault(m => m.Id == materialId && m.TeacherCourseSectionId == teacherCourseSectionId);

                    if (material == null) return;

                    // Get all students enrolled in this course section
                    var students = db.StudentCourses
                        .Where(sc => sc.CourseId == material.TeacherCourseSection.CourseId && 
                                    sc.SectionId == material.TeacherCourseSection.SectionId)
                        .Include(sc => sc.Student)
                        .Select(sc => sc.Student)
                        .ToList();

                    // Send notifications to all students
                    foreach (var student in students)
                    {
                        if (!string.IsNullOrEmpty(student.Email))
                        {
                            EmailHelper.SendMaterialNotification(
                                student.Email,
                                $"{student.FirstName} {student.LastName}",
                                material.TeacherCourseSection.Course.CourseTitle,
                                material.Title,
                                $"{material.TeacherCourseSection.Teacher.FirstName} {material.TeacherCourseSection.Teacher.LastName}",
                                material.Type
                            );
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"Material notification sent to {students.Count} students for: {material.Title}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending material notifications: {ex.Message}");
            }
        }

        /// <summary>
        /// Send announcement posted notification to all students in the course section
        /// </summary>
        public static void NotifyAnnouncementPosted(int teacherCourseSectionId, int announcementId)
        {
            try
            {
                using (var db = new LMSContext())
                {
                    // Get the announcement with related data
                    var announcement = db.Announcements
                        .Include(a => a.TeacherCourseSection.Course)
                        .Include(a => a.TeacherCourseSection.Section)
                        .Include(a => a.CreatedBy)
                        .FirstOrDefault(a => a.Id == announcementId && a.TeacherCourseSectionId == teacherCourseSectionId);

                    if (announcement == null) return;

                    // Get all students enrolled in this course section
                    var students = db.StudentCourses
                        .Where(sc => sc.CourseId == announcement.TeacherCourseSection.CourseId && 
                                    sc.SectionId == announcement.TeacherCourseSection.SectionId)
                        .Include(sc => sc.Student)
                        .Select(sc => sc.Student)
                        .Where(s => s.Id != announcement.CreatedByUserId) // Don't notify the creator if it's a student
                        .ToList();

                    // Send notifications to all students
                    foreach (var student in students)
                    {
                        if (!string.IsNullOrEmpty(student.Email))
                        {
                            EmailHelper.SendAnnouncementNotification(
                                student.Email,
                                $"{student.FirstName} {student.LastName}",
                                announcement.TeacherCourseSection.Course.CourseTitle,
                                $"{announcement.CreatedBy.FirstName} {announcement.CreatedBy.LastName}",
                                announcement.Content
                            );
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"Announcement notification sent to {students.Count} students");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending announcement notifications: {ex.Message}");
            }
        }

        /// <summary>
        /// Send classwork posted notification to all students in the course section
        /// </summary>
        public static void NotifyClassworkPosted(int teacherCourseSectionId, int classworkId)
        {
            try
            {
                using (var db = new LMSContext())
                {
                    // Get the classwork with related data
                    var classwork = db.Classworks
                        .Include(c => c.TeacherCourseSection.Course)
                        .Include(c => c.TeacherCourseSection.Section)
                        .Include(c => c.TeacherCourseSection.Teacher)
                        .FirstOrDefault(c => c.Id == classworkId && c.TeacherCourseSectionId == teacherCourseSectionId);

                    if (classwork == null) return;

                    // Skip notification if the classwork is scheduled for future (not yet published)
                    if (classwork.IsScheduled && classwork.ScheduledPublishDate.HasValue && 
                        classwork.ScheduledPublishDate.Value > DateTime.Now)
                    {
                        System.Diagnostics.Debug.WriteLine($"Classwork '{classwork.Title}' is scheduled for future, skipping notification");
                        return;
                    }

                    // Get all students enrolled in this course section
                    var students = db.StudentCourses
                        .Where(sc => sc.CourseId == classwork.TeacherCourseSection.CourseId && 
                                    sc.SectionId == classwork.TeacherCourseSection.SectionId)
                        .Include(sc => sc.Student)
                        .Select(sc => sc.Student)
                        .ToList();

                    // Send notifications to all students
                    foreach (var student in students)
                    {
                        if (!string.IsNullOrEmpty(student.Email))
                        {
                            EmailHelper.SendClassworkNotification(
                                student.Email,
                                $"{student.FirstName} {student.LastName}",
                                classwork.TeacherCourseSection.Course.CourseTitle,
                                classwork.Title,
                                classwork.ClassworkType,
                                $"{classwork.TeacherCourseSection.Teacher.FirstName} {classwork.TeacherCourseSection.Teacher.LastName}",
                                classwork.Deadline,
                                classwork.Points
                            );
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"Classwork notification sent to {students.Count} students for: {classwork.Title}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending classwork notifications: {ex.Message}");
            }
        }

        /// <summary>
        /// Send due date reminders for classwork that's due within 24 hours
        /// </summary>
        public static void SendDueDateReminders()
        {
            try
            {
                using (var db = new LMSContext())
                {
                    var now = DateTime.Now;
                    var next24Hours = now.AddHours(24);

                    // Get all classwork that's due within 24 hours
                    var dueSoonClasswork = db.Classworks
                        .Include(c => c.TeacherCourseSection.Course)
                        .Include(c => c.TeacherCourseSection.Section)
                        .Include(c => c.ClassworkSubmissions.Select(s => s.Student))
                        .Where(c => c.IsActive && 
                               c.Deadline.HasValue && 
                               c.Deadline.Value > now && 
                               c.Deadline.Value <= next24Hours)
                        .Where(c => !c.IsScheduled || (c.ScheduledPublishDate.HasValue && c.ScheduledPublishDate.Value <= now))
                        .ToList();

                    foreach (var classwork in dueSoonClasswork)
                    {
                        // Get students who haven't submitted yet
                        var notSubmittedStudents = classwork.ClassworkSubmissions
                            .Where(s => s.Status == "Not Submitted")
                            .Select(s => s.Student)
                            .Where(s => !string.IsNullOrEmpty(s.Email))
                            .ToList();

                        // Send reminders to students who haven't submitted
                        foreach (var student in notSubmittedStudents)
                        {
                            EmailHelper.SendClassworkDueReminder(
                                student.Email,
                                $"{student.FirstName} {student.LastName}",
                                classwork.TeacherCourseSection.Course.CourseTitle,
                                classwork.Title,
                                classwork.ClassworkType,
                                classwork.Deadline.Value,
                                classwork.Points
                            );
                        }

                        System.Diagnostics.Debug.WriteLine($"Due date reminders sent to {notSubmittedStudents.Count} students for: {classwork.Title}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending due date reminders: {ex.Message}");
            }
        }

        /// <summary>
        /// Send teacher assignment notification to students in the section
        /// </summary>
        public static void NotifyTeacherAssigned(int teacherCourseSectionId)
        {
            try
            {
                using (var db = new LMSContext())
                {
                    // Get the teacher course section assignment
                    var assignment = db.TeacherCourseSections
                        .Include(tcs => tcs.Course)
                        .Include(tcs => tcs.Section)
                        .Include(tcs => tcs.Section.Program)
                        .Include(tcs => tcs.Teacher)
                        .FirstOrDefault(tcs => tcs.Id == teacherCourseSectionId);

                    if (assignment == null) return;

                    // Get all students enrolled in this course section
                    var students = db.StudentCourses
                        .Where(sc => sc.CourseId == assignment.CourseId && 
                                    sc.SectionId == assignment.SectionId)
                        .Include(sc => sc.Student)
                        .Select(sc => sc.Student)
                        .ToList();

                    // Send notifications to all students
                    foreach (var student in students)
                    {
                        if (!string.IsNullOrEmpty(student.Email))
                        {
                            var sectionName = $"{assignment.Section.Program.ProgramCode} {assignment.Section.YearLevel}-{assignment.Section.SectionName}";

                            EmailHelper.SendTeacherAssignedToStudentNotification(
                                student.Email,
                                $"{student.FirstName} {student.LastName}",
                                $"{assignment.Teacher.FirstName} {assignment.Teacher.LastName}",
                                assignment.Course.CourseTitle,
                                sectionName,
                                assignment.Semester
                            );
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"Teacher assignment notification sent to {students.Count} students");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending teacher assignment notifications: {ex.Message}");
            }
        }

        /// <summary>
        /// Check and send scheduled classwork notifications (for classwork that's now published)
        /// </summary>
        public static void CheckScheduledClassworkNotifications()
        {
            try
            {
                using (var db = new LMSContext())
                {
                    var now = DateTime.Now;

                    // Get scheduled classwork that should now be published but hasn't been notified yet
                    var newlyPublishedClasswork = db.Classworks
                        .Include(c => c.TeacherCourseSection)
                        .Where(c => c.IsActive && 
                               c.IsScheduled && 
                               c.ScheduledPublishDate.HasValue && 
                               c.ScheduledPublishDate.Value <= now)
                        .ToList();

                    foreach (var classwork in newlyPublishedClasswork)
                    {
                        // Send notification for this newly published classwork
                        NotifyClassworkPosted(classwork.TeacherCourseSectionId, classwork.Id);
                        
                        // Mark as published by clearing the scheduled flag (optional)
                        // classwork.IsScheduled = false;
                    }

                    if (newlyPublishedClasswork.Any())
                    {
                        // db.SaveChanges(); // Uncomment if you want to clear the IsScheduled flag
                        System.Diagnostics.Debug.WriteLine($"Sent notifications for {newlyPublishedClasswork.Count} newly published scheduled classwork");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking scheduled classwork notifications: {ex.Message}");
            }
        }
    }
}