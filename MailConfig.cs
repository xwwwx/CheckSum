using System;
using System.Collections.Generic;
using System.Text;

namespace CheckSum
{
    public class MailConfig
    {
        public string Host { get; set; }
        public string[] Recipients { get; set; }
        public string Subject { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
    }
}
