using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using dotenv.net;

namespace LMS.Helpers
{
    public static class EmailHelper
    {
        public static bool SendEmail(string toEmail, string subject, string htmlBody, string fromName = null)
        {
            try
            {
                DotEnv.Load();

                // Load SMTP configuration from .env
                string host = Environment.GetEnvironmentVariable("EmailHost");
                int port = int.Parse(Environment.GetEnvironmentVariable("EmailPort"));
                string username = Environment.GetEnvironmentVariable("EmailUsername");
                string password = Environment.GetEnvironmentVariable("EmailPassword");
                string defaultFromName = Environment.GetEnvironmentVariable("EmailFromName") ?? "G2 Academy University";
                bool enableSSL = bool.Parse(Environment.GetEnvironmentVariable("EmailEnableSSL") ?? "true");

                // Check for missing configuration
                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(defaultFromName))
                    throw new InvalidOperationException("Incomplete email configuration in .env.");

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
            <a href='#' 
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
