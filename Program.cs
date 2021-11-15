using Microsoft.Extensions.Configuration;
using System.Linq;

namespace CheckSum
{
    class Program
    {
        static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var scanDir = config.GetValue<string>("ScanDir");
            var except = config.GetSection("Except").Get<string[]>();
            var output = config.GetValue<string>("OutPut");
            var sendMail = config.GetValue<bool>("SendMail");
            var mailConfig = config.GetSection("MailConfig").Get<MailConfig>();

            var files = CheckSum.ScanFile(scanDir, except);

            var fileHashs = CheckSum.GetFileHashs(files);

            var lastHashLog = CheckSum.GetLastHashLogFromOutputDir(output);

            _ = CheckSum.WriteHashLog(output, fileHashs);

            var lastFileHashs = CheckSum.ReadHashLog(lastHashLog);

            if (lastFileHashs.Any())
            {
                var (createdFiles, modifiedFiles) = CheckSum.FindCreatedOrModifiedFile(fileHashs, lastFileHashs);

                var deletedFile = CheckSum.FindDeletedFile(fileHashs, lastFileHashs);

                var reportText = CheckSum.GenCheckReportText(createdFiles, modifiedFiles, deletedFile);

                _ = CheckSum.WriteReport(output, reportText);

                if(sendMail)
                    CheckSum.SendReportMail(mailConfig, reportText);
            }
        }
    }
}
