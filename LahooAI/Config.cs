using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace LahooAI
{
    class Config
    {
        public static string DatabaseServer { get; set; } = ConfigurationManager.AppSettings["DatabaseServer"];
        public static string Database { get; set; } = ConfigurationManager.AppSettings["Database"];
        public static string UserID { get; set; } = ConfigurationManager.AppSettings["UserID"];
        public static string Password { get; set; } = ConfigurationManager.AppSettings["Password"];
        public static string Port { get; set; } = ConfigurationManager.AppSettings["Port"];
        public static string Charset { get; set; } = ConfigurationManager.AppSettings["Charset"];
    }
}
