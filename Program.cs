using Microsoft.Extensions.Configuration;
using System;
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
            var sendMail = config.GetValue<bool>("SendMail");
            var mailConfig = config.GetSection("MailConfig").Get<MailConfig>();
            #endregion

            Console.WriteLine($"檢驗路徑: {scanDir}");

            //掃描需檢查的檔案
            var files = CheckSum.ScanFile(scanDir, except);

            //取得所有檔案的Hash(SHA256)
            var fileHashs = CheckSum.GetFileHashs(files);
            Console.WriteLine($"此次驗證檔案筆數: {fileHashs.Count}");

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

                //產生差異報告文字
                var reportText = CheckSum.GenCheckReportText(createdFiles, modifiedFiles, deletedFile);

                //產生差異報告檔案
                _ = CheckSum.WriteReport(output, reportText);

                //寄出差異報告Mail
                if (sendMail)
                {
                    //若檢測到檔案異動則顯示於主旨
                    if (createdFiles.Any() || modifiedFiles.Any() || deletedFile.Any())
                        mailConfig.Subject += $" - 檔案異動 {createdFiles.Count + modifiedFiles.Count + deletedFile.Count} 支";

                    CheckSum.SendReportMail(mailConfig, reportText);
                }
            }

            Console.WriteLine("檢驗完畢!");
        }
    }
}
