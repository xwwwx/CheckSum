using System;

namespace CheckSum
{
    public struct FileHash
    {
        public string Name { get; set; }

        public string Hash { get; set; }

        public DateTime LastWriteDate { get; set; }

        public DateTime HashTime { get; set; }
    }
}
