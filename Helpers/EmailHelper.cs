using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace LMS.Helpers
{
    public enum EmailType
    {
        AccountCreated,
        TeacherAssigned
        // Add more (e.g., PasswordReset, Announcement, etc.)
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
    }
}
