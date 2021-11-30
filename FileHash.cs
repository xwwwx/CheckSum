using System;

namespace CheckSum
{
    public class FileHash
    {
        public string Name { get; set; }

        public string Hash { get; set; }

        public DateTime HashTime { get; set; }

        public bool IsSuspiciousFile { get; set; }
    }
}
