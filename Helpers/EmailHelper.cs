using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace LMS.Helpers
{
    public static class EmailHelper
    {
        public static bool SendEmail(string toEmail, string subject, string htmlBody, string fromName = null)
        {
            try
            {
                // Load SMTP configuration from Web.config
                string host = ConfigurationManager.AppSettings["EmailHost"];
                int port = int.Parse(ConfigurationManager.AppSettings["EmailPort"]);
                string username = ConfigurationManager.AppSettings["EmailUsername"];
                string password = ConfigurationManager.AppSettings["EmailPassword"];
                string defaultFromName = ConfigurationManager.AppSettings["EmailFromName"] ?? "G2 Academy University";
                bool enableSSL = bool.Parse(ConfigurationManager.AppSettings["EmailEnableSSL"] ?? "true");

                // Check for missing configuration
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
                // Log or print for debugging
                System.Diagnostics.Debug.WriteLine("Email sending failed: " + ex.Message);
                return false;
            }
        }

        public static string CreateWelcomeEmailTemplate(string userEmail, string userPassword, string userName = null)
        {
            string baseUrl = ConfigurationManager.AppSettings["AppBaseUrl"] ?? "https://localhost:44354";
            string greeting = !string.IsNullOrEmpty(userName) ? $"Dear {userName}," : "Dear user,";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Welcome to G2 Academy University LMS</title>
</head>
<body style='margin:0;padding:0;font-family:Arial,sans-serif;'>
    <div>
        <h1 style='color:#003366;text-align:center;margin-bottom:30px;'>Welcome to G2 Academy University LMS</h1>

        <p style='color: #000;>{greeting}</p>
        <p style='color: #000;'>
            Your account has been successfully created! You can now access our <strong>Learning Management System (LMS)</strong> using the credentials below:
        </p>
            <p style='margin-top:0;'>Your Login Credentials</h3>
            <p style='color: #000'><strong>Email:</strong> <span>{userEmail}</span></p>
            <p><strong>Password:</strong> <span>{userPassword}</span></p>
        <div style='text-align:center;margin:30px 0;'>
            <a href='{baseUrl}/Home/Login' 
               style='display:inline-block;background:#007bff;color:white;text-decoration:none;padding:12px 30px;border-radius:6px;font-weight:bold;'>
               Access LMS Portal
            </a>
        </div>
        <p style='color:#666;'>If you have any questions or need assistance, please contact our support team.</p>
    </div>
</body>
</html>";
        }
    }
}
