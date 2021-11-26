using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace CheckSum
{
    class Program
    {
        static async Task Main(string[] args)
        {
            #region 讀取設定檔
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var scanDir = config.GetValue<string>("ScanDir");
            var except = config.GetSection("Except").Get<string[]>();
            var output = config.GetValue<string>("OutPut");
            var virusTotalConfig = config.GetSection("VirusTotalConfig").Get<VirusTotalConfig>();
            var sendMail = config.GetValue<bool>("SendMail");
            var mailConfig = config.GetSection("MailConfig").Get<MailConfig>();
            #endregion

            //掃描需檢查的檔案
            var files = CheckSum.ScanFile(scanDir, except);

            //取得所有檔案的Hash(MD5)
            var fileHashs = CheckSum.GetFileHashs(files);

            //連接 VirusTotal 檢驗檔案可信度
            if (virusTotalConfig.Scan)
            {
                VirusCheck.LoadConfig(virusTotalConfig);
                fileHashs = await VirusCheck.ScanHashs(fileHashs);
            }

            //取得上次檢查的HashLog
            var lastHashLog = CheckSum.GetLastHashLogFromOutputDir(output);

            //記錄此次檔案Hash
            _ = CheckSum.WriteHashLog(output, fileHashs);

            //讀取上次檢查的Hash
            var lastFileHashs = CheckSum.ReadHashLog(lastHashLog);

            //若有讀取到上次的Hash則製作差異報告
            if (lastFileHashs.Any())
            {
                //啟動尋找新增及異動任務
                var findCreatedAndModifiedTask = Task.Run(() => CheckSum.FindCreatedOrModifiedFile(fileHashs, lastFileHashs));

                //啟動尋找刪除任務
                var findDeletedTask = Task.Run(() => CheckSum.FindDeletedFile(fileHashs, lastFileHashs));

                //取得尋找新增及異動任務結果
                var (createdFiles, modifiedFiles) = await findCreatedAndModifiedTask;

                //取得啟動尋找刪除任務結果
                var deletedFile = await findDeletedTask;

                //取得可疑檔案
                var suspiciousFile = fileHashs.Where(f => f.IsSuspiciousFile).ToList();

                //產生差異報告文字
                var reportText = CheckSum.GenCheckReportText(createdFiles, modifiedFiles, deletedFile, suspiciousFile);

                //產生差異報告檔案
                _ = CheckSum.WriteReport(output, reportText);

                //寄出差異報告Mail
                if(sendMail)
                    CheckSum.SendReportMail(mailConfig, reportText);
            }
        }
    }
}
