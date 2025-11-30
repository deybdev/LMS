using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace LMS.Helpers
{
    public enum EmailType
    {
        AccountCreated,
        TeacherAssigned,
        PasswordReset,
        MaterialPosted,
        AnnouncementPosted,
        ClassworkPosted,
        ClassworkDueReminder,
        TeacherAssignedToStudent
        // Add more (e.g., Announcement, etc.)
    }

    public static class EmailHelper
    {
        public static bool SendEmail(string toEmail, string subject, string htmlBody, string fromName = null)
        {
            try
            {
                string host = ConfigurationManager.AppSettings["EmailHost"];
                int port = int.Parse(ConfigurationManager.AppSettings["EmailPort"]);
                string username = ConfigurationManager.AppSettings["EmailUsername"];
                string password = ConfigurationManager.AppSettings["EmailPassword"];
                string defaultFromName = ConfigurationManager.AppSettings["EmailFromName"] ?? "G2 Academy University";
                bool enableSSL = bool.Parse(ConfigurationManager.AppSettings["EmailEnableSSL"] ?? "true");

                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                    throw new InvalidOperationException("Incomplete email configuration in Web.config.");

                using (var smtp = new SmtpClient(host, port))
                {
                    smtp.EnableSsl = enableSSL;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(username, password);

                    var message = new MailMessage
                    {
                        From = new MailAddress(username, fromName ?? defaultFromName),
                        Subject = subject,
                        Body = htmlBody,
                        IsBodyHtml = true
                    };

                    message.To.Add(toEmail);
                    smtp.Send(message);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Email sending failed: " + ex.Message);
                return false;
            }
        }

        // ======================================================
        //  UNIVERSAL TEMPLATE BUILDER
        // ======================================================
        public static string GenerateEmailTemplate(EmailType type, dynamic data)
        {
            switch (type)
            {
                case EmailType.AccountCreated:
                    return AccountCreatedTemplate(
                        data.Name,
                        data.Email,
                        data.Password
                    );

                case EmailType.TeacherAssigned:
                    return TeacherAssignedTemplate(
                        data.TeacherName,
                        data.CourseTitle,
                        data.SectionName,
                        data.Semester
                    );

                case EmailType.PasswordReset:
                    return PasswordResetTemplate(
                        data.Name,
                        data.ResetCode,
                        data.ExpiryTime
                    );

                case EmailType.MaterialPosted:
                    return MaterialPostedTemplate(
                        data.StudentName,
                        data.CourseTitle,
                        data.MaterialTitle,
                        data.TeacherName,
                        data.MaterialType,
                        data.PostedDate
                    );

                case EmailType.AnnouncementPosted:
                    return AnnouncementPostedTemplate(
                        data.StudentName,
                        data.CourseTitle,
                        data.TeacherName,
                        data.AnnouncementContent,
                        data.PostedDate
                    );

                case EmailType.ClassworkPosted:
                    return ClassworkPostedTemplate(
                        data.StudentName,
                        data.CourseTitle,
                        data.ClassworkTitle,
                        data.ClassworkType,
                        data.TeacherName,
                        data.Deadline,
                        data.Points,
                        data.PostedDate
                    );

                case EmailType.ClassworkDueReminder:
                    return ClassworkDueReminderTemplate(
                        data.StudentName,
                        data.CourseTitle,
                        data.ClassworkTitle,
                        data.ClassworkType,
                        data.Deadline,
                        data.HoursRemaining,
                        data.Points
                    );

                case EmailType.TeacherAssignedToStudent:
                    return TeacherAssignedToStudentTemplate(
                        data.StudentName,
                        data.TeacherName,
                        data.CourseTitle,
                        data.SectionName,
                        data.Semester
                    );

                default:
                    return "<p>No template found.</p>";
            }
        }

        // ======================================================
        //  TEMPLATE: ACCOUNT CREATED
        // ======================================================
        private static string AccountCreatedTemplate(string name, string email, string password)
        {
            string greeting = !string.IsNullOrEmpty(name) ? $"Dear {name}," : "Dear user,";

            return $@"
<html>
<body style='font-family:Arial;padding:20px;'>
    <h2 style='color:#003366;'>Welcome to G2 Academy University LMS</h2>

    <p>{greeting}</p>
    <p>Your account has been created successfully. Below are your login credentials:</p>

    <p><strong>Email:</strong> {email}</p>
    <p><strong>Password:</strong> {password}</p>

    <a href='#' 
       style='background:#007bff;color:#fff;padding:12px 25px;border-radius:5px;text-decoration:none;display:inline-block;margin-top:20px;'>
       Access LMS Portal
    </a>

    <p style='color:#777;margin-top:20px;'>If you need assistance, please contact support.</p>
</body>
</html>";
        }

        // ======================================================
        //  TEMPLATE: TEACHER ASSIGNED TO COURSE
        // ======================================================
        private static string TeacherAssignedTemplate(string teacherName, string course, string section, string semester)
        {
            string greeting = !string.IsNullOrEmpty(teacherName) ? $"Dear {teacherName}," : "Dear Instructor,";

            return $@"
<html>
<body style='font-family:Arial;padding:20px;'>
    <h2 style='color:#003366;'>Course Assignment Notification</h2>

    <p>{greeting}</p>

    <p>You have been assigned to the following course:</p>

    <p><strong>Course:</strong> {course}</p>
    <p><strong>Section:</strong> {section}</p>
    <p><strong>Semester:</strong> {semester}</p>

    <p>Please log in to the LMS to view further details.</p>

    <a href='#' 
       style='background:#28a745;color:#fff;padding:12px 25px;border-radius:5px;text-decoration:none;display:inline-block;margin-top:20px;'>
       Go to LMS
    </a>

    <p style='color:#777;margin-top:20px;'>Contact the academic office if you have questions.</p>
</body>
</html>";
        }

        // ======================================================
        //  TEMPLATE: PASSWORD RESET
        // ======================================================
        private static string PasswordResetTemplate(string name, string resetCode, string expiryTime)
        {
            string greeting = !string.IsNullOrEmpty(name) ? $"Dear {name}," : "Dear user,";

            return $@"
<html>
<body style='font-family:Arial;padding:20px;'>
    <h2 style='color:#003366;'>Password Reset Request</h2>

    <p>{greeting}</p>
    <p>We received a request to reset your password for your G2 Academy LMS account.</p>

    <div style='background:#f8f9fa;padding:20px;border-radius:8px;margin:20px 0;text-align:center;'>
        <p style='margin:0 0 10px 0;font-size:14px;color:#666;'>Your Reset Code:</p>
        <h1 style='color:#1852AC;font-size:32px;font-weight:bold;margin:0;letter-spacing:8px;font-family:monospace;'>{resetCode}</h1>
    </div>

    <p><strong>Important:</strong> This code will expire on {expiryTime}. If you didn't request this reset, please ignore this email.</p>

    <p>To reset your password:</p>
    <ol style='color:#666;'>
        <li>Click on the ""Forgot Password?"" link on the login page</li>
        <li>Enter your email address</li>
        <li>Enter the reset code above</li>
        <li>Create your new password</li>
    </ol>

    <p style='color:#777;margin-top:30px;font-size:12px;'>If you need assistance, please contact support at support@g2academy.edu</p>
</body>
</html>";
        }

        // ======================================================
        //  TEMPLATE: MATERIAL POSTED
        // ======================================================
        private static string MaterialPostedTemplate(string studentName, string courseTitle, string materialTitle, 
            string teacherName, string materialType, DateTime postedDate)
        {
            string greeting = !string.IsNullOrEmpty(studentName) ? $"Dear {studentName}," : "Dear Student,";

            return $@"
<html>
<body style='font-family:Arial;padding:20px;background-color:#f9f9f9;'>
    <div style='max-width:600px;margin:0 auto;background:white;padding:30px;border-radius:10px;box-shadow:0 2px 10px rgba(0,0,0,0.1);'>
        <div style='text-align:center;margin-bottom:30px;'>
            <h2 style='color:#1852AC;margin:0;font-size:24px;'>📚 New Learning Material Available</h2>
        </div>

        <p style='font-size:16px;color:#333;line-height:1.6;'>{greeting}</p>
        
        <p style='font-size:16px;color:#333;line-height:1.6;'>
            Your instructor has posted new learning material for your course. Here are the details:
        </p>

        <div style='background:#f0f8ff;padding:20px;border-radius:8px;margin:20px 0;'>
            <p style='margin:0 0 10px 0;'><strong style='color:#1852AC;'>Course:</strong> {courseTitle}</p>
            <p style='margin:0 0 10px 0;'><strong style='color:#1852AC;'>Material:</strong> {materialTitle}</p>
            <p style='margin:0 0 10px 0;'><strong style='color:#1852AC;'>Type:</strong> {materialType}</p>
            <p style='margin:0 0 10px 0;'><strong style='color:#1852AC;'>Instructor:</strong> {teacherName}</p>
            <p style='margin:0;'><strong style='color:#1852AC;'>Posted:</strong> {postedDate:MMM dd, yyyy h:mm tt}</p>
        </div>

        <p style='font-size:16px;color:#333;line-height:1.6;'>
            Log in to the LMS to access this material and continue your learning journey.
        </p>

        <div style='text-align:center;margin:30px 0;'>
            <a href='#' 
               style='background:#1852AC;color:#fff;padding:15px 30px;border-radius:8px;text-decoration:none;display:inline-block;font-weight:bold;'>
               View Material
            </a>
        </div>

        <p style='color:#777;margin-top:30px;font-size:14px;text-align:center;'>
            This is an automated notification from G2 Academy University LMS.
        </p>
    </div>
</body>
</html>";
        }

        // ======================================================
        //  TEMPLATE: ANNOUNCEMENT POSTED
        // ======================================================
        private static string AnnouncementPostedTemplate(string studentName, string courseTitle, string teacherName, 
            string announcementContent, DateTime postedDate)
        {
            string greeting = !string.IsNullOrEmpty(studentName) ? $"Dear {studentName}," : "Dear Student,";
            
            // Truncate announcement content for email
            string truncatedContent = announcementContent.Length > 200 
                ? announcementContent.Substring(0, 200) + "..." 
                : announcementContent;

            return $@"
<html>
<body style='font-family:Arial;padding:20px;background-color:#f9f9f9;'>
    <div style='max-width:600px;margin:0 auto;background:white;padding:30px;border-radius:10px;box-shadow:0 2px 10px rgba(0,0,0,0.1);'>
        <div style='text-align:center;margin-bottom:30px;'>
            <h2 style='color:#FF6B35;margin:0;font-size:24px;'>📢 New Course Announcement</h2>
        </div>

        <p style='font-size:16px;color:#333;line-height:1.6;'>{greeting}</p>
        
        <p style='font-size:16px;color:#333;line-height:1.6;'>
            Your instructor has posted a new announcement for your course:
        </p>

        <div style='background:#fff5f0;padding:20px;border-radius:8px;margin:20px 0;'>
            <p style='margin:0 0 10px 0;'><strong style='color:#FF6B35;'>Course:</strong> {courseTitle}</p>
            <p style='margin:0 0 15px 0;'><strong style='color:#FF6B35;'>From:</strong> {teacherName}</p>
            <div style='background:white;padding:15px;border-radius:5px;border:1px solid #ffe0d4;'>
                <p style='margin:0;color:#555;font-style:italic;'>{truncatedContent}</p>
            </div>
            <p style='margin:15px 0 0 0;'><strong style='color:#FF6B35;'>Posted:</strong> {postedDate:MMM dd, yyyy h:mm tt}</p>
        </div>

        <p style='font-size:16px;color:#333;line-height:1.6;'>
            Log in to the LMS to read the full announcement and participate in any discussions.
        </p>

        <div style='text-align:center;margin:30px 0;'>
            <a href='#' 
               style='background:#FF6B35;color:#fff;padding:15px 30px;border-radius:8px;text-decoration:none;display:inline-block;font-weight:bold;'>
               View Announcement
            </a>
        </div>

        <p style='color:#777;margin-top:30px;font-size:14px;text-align:center;'>
            This is an automated notification from G2 Academy University LMS.
        </p>
    </div>
</body>
</html>";
        }

        // ======================================================
        //  TEMPLATE: CLASSWORK POSTED
        // ======================================================
        private static string ClassworkPostedTemplate(string studentName, string courseTitle, string classworkTitle, 
            string classworkType, string teacherName, DateTime? deadline, int points, DateTime postedDate)
        {
            string greeting = !string.IsNullOrEmpty(studentName) ? $"Dear {studentName}," : "Dear Student,";
            string deadlineText = deadline.HasValue 
                ? $"Due: {deadline.Value:MMM dd, yyyy h:mm tt}" 
                : "No due date specified";

            return $@"
<html>
<body style='font-family:Arial;padding:20px;background-color:#f9f9f9;'>
    <div style='max-width:600px;margin:0 auto;background:white;padding:30px;border-radius:10px;box-shadow:0 2px 10px rgba(0,0,0,0.1);'>
        <div style='text-align:center;margin-bottom:30px;'>
            <h2 style='color:#28A745;margin:0;font-size:24px;'>📝 New {classworkType} Posted</h2>
        </div>

        <p style='font-size:16px;color:#333;line-height:1.6;'>{greeting}</p>
        
        <p style='font-size:16px;color:#333;line-height:1.6;'>
            A new {classworkType.ToLower()} has been posted for your course. Here are the details:
        </p>

        <div style='background:#f0fff0;padding:20px;border-radius:8px;margin:20px 0;'>
            <p style='margin:0 0 10px 0;'><strong style='color:#28A745;'>Course:</strong> {courseTitle}</p>
            <p style='margin:0 0 10px 0;'><strong style='color:#28A745;'>{classworkType}:</strong> {classworkTitle}</p>
            <p style='margin:0 0 10px 0;'><strong style='color:#28A745;'>Instructor:</strong> {teacherName}</p>
            <p style='margin:0 0 10px 0;'><strong style='color:#28A745;'>Points:</strong> {points}</p>
            <p style='margin:0 0 10px 0;'><strong style='color:#28A745;'>Posted:</strong> {postedDate:MMM dd, yyyy h:mm tt}</p>
            <p style='margin:0;'><strong style='color:#dc3545;'>{deadlineText}</strong></p>
        </div>

        {(deadline.HasValue ? $@"
        <div style='background:#fff3cd;border:1px solid #ffeaa7;padding:15px;border-radius:8px;margin:20px 0;'>
            <p style='margin:0;color:#856404;'><strong>⏰ Reminder:</strong> Don't forget to complete and submit your work before the deadline!</p>
        </div>" : "")}

        <p style='font-size:16px;color:#333;line-height:1.6;'>
            Log in to the LMS to view the complete instructions and submit your work.
        </p>

        <div style='text-align:center;margin:30px 0;'>
            <a href='#' 
               style='background:#28A745;color:#fff;padding:15px 30px;border-radius:8px;text-decoration:none;display:inline-block;font-weight:bold;'>
               View {classworkType}
            </a>
        </div>

        <p style='color:#777;margin-top:30px;font-size:14px;text-align:center;'>
            This is an automated notification from G2 Academy University LMS.
        </p>
    </div>
</body>
</html>";
        }

        // ======================================================
        //  TEMPLATE: CLASSWORK DUE REMINDER (24 hours)
        // ======================================================
        private static string ClassworkDueReminderTemplate(string studentName, string courseTitle, string classworkTitle, 
            string classworkType, DateTime deadline, int hoursRemaining, int points)
        {
            string greeting = !string.IsNullOrEmpty(studentName) ? $"Dear {studentName}," : "Dear Student,";
            string urgencyColor = hoursRemaining <= 6 ? "#DC3545" : "#FFC107";
            string urgencyIcon = hoursRemaining <= 6 ? "🚨" : "⏰";

            return $@"
<html>
<body style='font-family:Arial;padding:20px;background-color:#f9f9f9;'>
    <div style='max-width:600px;margin:0 auto;background:white;padding:30px;border-radius:10px;box-shadow:0 2px 10px rgba(0,0,0,0.1);'>
        <div style='text-align:center;margin-bottom:30px;'>
            <h2 style='color:{urgencyColor};margin:0;font-size:24px;'>{urgencyIcon} {classworkType} Due Soon!</h2>
        </div>

        <p style='font-size:16px;color:#333;line-height:1.6;'>{greeting}</p>
        
        <p style='font-size:16px;color:#333;line-height:1.6;'>
            This is a friendly reminder that you have a {classworkType.ToLower()} due in <strong style='color:{urgencyColor};'>{hoursRemaining} hours</strong>.
        </p>

        <div style='background:#fff5f5;padding:20px;border-radius:8px;margin:20px 0;'>
            <p style='margin:0 0 10px 0;'><strong style='color:{urgencyColor};'>Course:</strong> {courseTitle}</p>
            <p style='margin:0 0 10px 0;'><strong style='color:{urgencyColor};'>{classworkType}:</strong> {classworkTitle}</p>
            <p style='margin:0 0 10px 0;'><strong style='color:{urgencyColor};'>Points:</strong> {points}</p>
            <p style='margin:0;'><strong style='color:#DC3545;'>Due:</strong> {deadline:MMM dd, yyyy h:mm tt}</p>
        </div>

        <div style='background:#f8d7da;border:1px solid #f5c6cb;padding:15px;border-radius:8px;margin:20px 0;'>
            <p style='margin:0;color:#721c24;text-align:center;'><strong>📌 Action Required:</strong> Please complete and submit your work to avoid missing the deadline!</p>
        </div>

        <p style='font-size:16px;color:#333;line-height:1.6;'>
            Log in to the LMS now to submit your work on time.
        </p>

        <div style='text-align:center;margin:30px 0;'>
            <a href='#' 
               style='background:{urgencyColor};color:#fff;padding:15px 30px;border-radius:8px;text-decoration:none;display:inline-block;font-weight:bold;'>
               Submit Now
            </a>
        </div>

        <p style='color:#777;margin-top:30px;font-size:14px;text-align:center;'>
            This is an automated notification from G2 Academy University LMS.
        </p>
    </div>
</body>
</html>";
        }

        // ======================================================
        //  TEMPLATE: TEACHER ASSIGNED TO STUDENT
        // ======================================================
        private static string TeacherAssignedToStudentTemplate(string studentName, string teacherName, 
            string courseTitle, string sectionName, string semester)
        {
            string greeting = !string.IsNullOrEmpty(studentName) ? $"Dear {studentName}," : "Dear Student,";

            return $@"
<html>
<body style='font-family:Arial;padding:20px;background-color:#f9f9f9;'>
    <div style='max-width:600px;margin:0 auto;background:white;padding:30px;border-radius:10px;box-shadow:0 2px 10px rgba(0,0,0,0.1);'>
        <div style='text-align:center;margin-bottom:30px;'>
            <h2 style='color:#6F42C1;margin:0;font-size:24px;'>👨‍🏫 New Instructor Assigned</h2>
        </div>

        <p style='font-size:16px;color:#333;line-height:1.6;'>{greeting}</p>
        
        <p style='font-size:16px;color:#333;line-height:1.6;'>
            Great news! A new instructor has been assigned to one of your courses:
        </p>

        <div style='background:#f8f0ff;padding:20px;border-radius:8px;margin:20px 0;'>
            <p style='margin:0 0 10px 0;'><strong style='color:#6F42C1;'>Course:</strong> {courseTitle}</p>
            <p style='margin:0 0 10px 0;'><strong style='color:#6F42C1;'>Instructor:</strong> {teacherName}</p>
            <p style='margin:0 0 10px 0;'><strong style='color:#6F42C1;'>Section:</strong> {sectionName}</p>
            <p style='margin:0;'><strong style='color:#6F42C1;'>Semester:</strong> {semester}</p>
        </div>

        <p style='font-size:16px;color:#333;line-height:1.6;'>
            Your instructor will be posting course materials, assignments, and announcements soon. 
            Make sure to check the LMS regularly for updates.
        </p>

        <div style='text-align:center;margin:30px 0;'>
            <a href='#' 
               style='background:#6F42C1;color:#fff;padding:15px 30px;border-radius:8px;text-decoration:none;display:inline-block;font-weight:bold;'>
               Go to Course
            </a>
        </div>

        <p style='color:#777;margin-top:30px;font-size:14px;text-align:center;'>
            This is an automated notification from G2 Academy University LMS.
        </p>
    </div>
</body>
</html>";
        }

        // ======================================================
        //  HELPER METHODS FOR SENDING STUDENT NOTIFICATIONS
        // ======================================================

        /// <summary>
        /// Send material posted notification to students
        /// </summary>
        public static void SendMaterialNotification(string studentEmail, string studentName, string courseTitle, 
            string materialTitle, string teacherName, string materialType)
        {
            try
            {
                var emailBody = GenerateEmailTemplate(EmailType.MaterialPosted, new
                {
                    StudentName = studentName,
                    CourseTitle = courseTitle,
                    MaterialTitle = materialTitle,
                    TeacherName = teacherName,
                    MaterialType = materialType,
                    PostedDate = DateTime.Now
                });

                SendEmail(studentEmail, $"New Material Posted: {materialTitle} - {courseTitle}", emailBody);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send material notification: {ex.Message}");
            }
        }

        /// <summary>
        /// Send announcement posted notification to students
        /// </summary>
        public static void SendAnnouncementNotification(string studentEmail, string studentName, string courseTitle,
            string teacherName, string announcementContent)
        {
            try
            {
                var emailBody = GenerateEmailTemplate(EmailType.AnnouncementPosted, new
                {
                    StudentName = studentName,
                    CourseTitle = courseTitle,
                    TeacherName = teacherName,
                    AnnouncementContent = announcementContent,
                    PostedDate = DateTime.Now
                });

                SendEmail(studentEmail, $"New Announcement - {courseTitle}", emailBody);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send announcement notification: {ex.Message}");
            }
        }

        /// <summary>
        /// Send classwork posted notification to students
        /// </summary>
        public static void SendClassworkNotification(string studentEmail, string studentName, string courseTitle,
            string classworkTitle, string classworkType, string teacherName, DateTime? deadline, int points)
        {
            try
            {
                var emailBody = GenerateEmailTemplate(EmailType.ClassworkPosted, new
                {
                    StudentName = studentName,
                    CourseTitle = courseTitle,
                    ClassworkTitle = classworkTitle,
                    ClassworkType = classworkType,
                    TeacherName = teacherName,
                    Deadline = deadline,
                    Points = points,
                    PostedDate = DateTime.Now
                });

                SendEmail(studentEmail, $"New {classworkType}: {classworkTitle} - {courseTitle}", emailBody);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send classwork notification: {ex.Message}");
            }
        }

        /// <summary>
        /// Send classwork due reminder to students
        /// </summary>
        public static void SendClassworkDueReminder(string studentEmail, string studentName, string courseTitle,
            string classworkTitle, string classworkType, DateTime deadline, int points)
        {
            try
            {
                var hoursRemaining = (int)Math.Ceiling((deadline - DateTime.Now).TotalHours);
                
                var emailBody = GenerateEmailTemplate(EmailType.ClassworkDueReminder, new
                {
                    StudentName = studentName,
                    CourseTitle = courseTitle,
                    ClassworkTitle = classworkTitle,
                    ClassworkType = classworkType,
                    Deadline = deadline,
                    HoursRemaining = hoursRemaining,
                    Points = points
                });

                SendEmail(studentEmail, $"Reminder: {classworkType} Due Soon - {classworkTitle}", emailBody);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send due reminder: {ex.Message}");
            }
        }

        /// <summary>
        /// Send teacher assigned notification to students
        /// </summary>
        public static void SendTeacherAssignedToStudentNotification(string studentEmail, string studentName, 
            string teacherName, string courseTitle, string sectionName, int semester)
        {
            try
            {
                string semesterText = semester == 1 ? "1st Semester" : semester == 2 ? "2nd Semester" : $"Semester {semester}";

                var emailBody = GenerateEmailTemplate(EmailType.TeacherAssignedToStudent, new
                {
                    StudentName = studentName,
                    TeacherName = teacherName,
                    CourseTitle = courseTitle,
                    SectionName = sectionName,
                    Semester = semesterText
                });

                SendEmail(studentEmail, $"New Instructor Assigned: {courseTitle}", emailBody);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send teacher assignment notification: {ex.Message}");
            }
        }
    }
}
