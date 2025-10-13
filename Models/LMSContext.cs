using System;
using System.Data.Entity;
using System.IO;
using dotenv.net;

namespace LMS.Models
{
    public class LMSContext : DbContext
    {
        public LMSContext() : base(GetConnectionString()) { }

        private static string GetConnectionString()
        {
            string conn = Environment.GetEnvironmentVariable("DB_CONNECTION");

            if (string.IsNullOrEmpty(conn))
            {
                    string envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");

                    if (File.Exists(envPath))
                        DotEnv.Load(new DotEnvOptions(envFilePaths: new[] { envPath }));

                    conn = Environment.GetEnvironmentVariable("DB_CONNECTION");

            }

            return conn;
        }

        public DbSet<User> Users { get; set; }
    }
}
