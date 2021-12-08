using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CheckSum
{
    public static class CheckSum
    {
        public static List<FileInfo> ScanFile(string root, string[] except = null)
        {
            var result = new List<FileInfo>();

            if (except?.Any(e => root.EndsWith(e)) == true)
                return result;

            var checkPath = CheckPath(root);

            if (PathEnum.NotExsits == checkPath)
                return result;

            if (PathEnum.File == checkPath)
            {
                result.Add(new FileInfo(root));
                return result;
            }

            var rootDir = new DirectoryInfo(root);

            foreach(var childDir in rootDir.EnumerateDirectories())
                result.AddRange(ScanFile(childDir.FullName, except));

            foreach (var file in rootDir.EnumerateFiles())
                result.Add(file);

            return result;
        }

        public static List<FileHash> GetFileHashs(IEnumerable<FileInfo> files)
        {
            return files
                .AsParallel()
                .Where(file => file.Exists)
                .Select(file => GetFileHash(file))
                .ToList();
        }

        private static FileHash GetFileHash(FileInfo file)
        {
            using var stream = file.OpenRead();
            using var md5 = MD5.Create();

            return new FileHash
            {
                Name = file.FullName,
                Hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty),
                HashTime = DateTime.Now
            };
        }

        public static FileInfo WriteHashLog(string output, IEnumerable<FileHash> fileHashes)
        {
            Directory.CreateDirectory(output);
            var hashLogPath = Path.Combine(output, $"FileHash_{DateTime.Now.ToFileTime()}.hashlog");
            FileInfo hashLog = new FileInfo(hashLogPath);
            using var stream = hashLog.Create();
            JsonSerializer.Serialize(stream, fileHashes);
            stream.Close();
            return hashLog;
        }

        public static IEnumerable<FileInfo> ReadHashLogFromOutputDir(string output)
        {
            if (!Directory.Exists(output))
                return Enumerable.Empty<FileInfo>();

            var outputDir = new DirectoryInfo(output);
            return outputDir.EnumerateFiles("FileHash_*.hashlog");
        }

        public static FileInfo GetLastHashLogFromOutputDir(string output)
        {
            return ReadHashLogFromOutputDir(output)
                .OrderByDescending(file => file.LastWriteTime)
                .FirstOrDefault();
        }

        public static List<FileHash> ReadHashLog(FileInfo hashLog)
        {
            if (hashLog == null || !hashLog.Exists)
                return Enumerable.Empty<FileHash>().ToList();

            var stream = hashLog.OpenRead();

            return JsonSerializer.Deserialize<List<FileHash>>(stream);
        }

        public static (List<FileHash> createdFiles, List<FileHash> modifiedFiles) FindCreatedOrModifiedFile(IEnumerable<FileHash> currentFileHashs, IEnumerable<FileHash> lastFileHashs)
        {

            ISet<string> fileNamsSet = lastFileHashs.Select(h => h.Name).ToHashSet();
            ISet<string> fileHashSet = lastFileHashs.Select(h => $"{h.Name}-{h.Hash}").ToHashSet();

            var createdFiles = Enumerable.Empty<FileHash>().ToList();
            var modifiedFiles = Enumerable.Empty<FileHash>().ToList();

            foreach(FileHash currentFileHash in currentFileHashs)
            {
                if (fileNamsSet.Add(currentFileHash.Name))
                {
                    createdFiles.Add(currentFileHash);
                    continue;
                }
                else if (fileHashSet.Add($"{currentFileHash.Name}-{currentFileHash.Hash}"))
                {
                    modifiedFiles.Add(currentFileHash);
                    continue;
                }
            }

            return (createdFiles, modifiedFiles);
        }

        public static List<FileHash> FindDeletedFile(IEnumerable<FileHash> currentFileHashs, IEnumerable<FileHash> lastFileHashs)
        {
            ISet<string> fileNamsSet = currentFileHashs.Select(h => h.Name).ToHashSet();

            var deletedFiles = Enumerable.Empty<FileHash>().ToList();

            foreach (FileHash lastFileHash in lastFileHashs)
            {
                if (fileNamsSet.Add(lastFileHash.Name))
                    deletedFiles.Add(lastFileHash);
            }

            return deletedFiles;
        }

        public static string GenCheckReportText(IEnumerable<FileHash> createdFiles, IEnumerable<FileHash> modifiedFiles, IEnumerable<FileHash> deletedFiles)
        {
            StringBuilder reportText = new StringBuilder();
            reportText.AppendLine(@"    檔案完整性檢查報告    ");
            reportText.AppendLine(@"==========================");

            if(!createdFiles.Any() && !modifiedFiles.Any() && !deletedFiles.Any())
            {

                reportText.AppendLine(@"    檔案無異動    ");
            }
            else
            {
                if (createdFiles.Any())
                {
                    reportText.AppendLine();
                    reportText.AppendLine("    檔案新增");
                    reportText.AppendLine(@"-----------------------");
                    
                    foreach(FileHash createdFile in createdFiles)
                    {
                        reportText.AppendLine($@"檔名:{createdFile.Name}");
                        reportText.AppendLine($@"HASH:{createdFile.Hash}");
                        reportText.AppendLine();
                    }

                    reportText.AppendLine(@"-----------------------");
                }

                if (modifiedFiles.Any())
                {
                    reportText.AppendLine();
                    reportText.AppendLine("    檔案修改");
                    reportText.AppendLine(@"-----------------------");

                    foreach (FileHash modifiedFile in modifiedFiles)
                    {
                        reportText.AppendLine($@"檔名:{modifiedFile.Name}");
                        reportText.AppendLine($@"HASH:{modifiedFile.Hash}");
                        reportText.AppendLine();
                    }

                    reportText.AppendLine(@"-----------------------");
                }

                if (deletedFiles.Any())
                {
                    reportText.AppendLine();
                    reportText.AppendLine("    檔案刪除");
                    reportText.AppendLine(@"-----------------------");

                    foreach (FileHash deletedFile in deletedFiles)
                    {
                        reportText.AppendLine(@$"檔名:{deletedFile.Name}");
                        reportText.AppendLine($@"HASH:{deletedFile.Hash}");
                        reportText.AppendLine();
                    }

                    reportText.AppendLine(@"-----------------------");
                }
            }

            reportText.AppendLine(@"==========================");
            reportText.AppendLine($@"製表時間 : {DateTime.Now}");

            return reportText.ToString();
        }

        public static FileInfo WriteReport(string output, string reportText)
        {
            if (PathEnum.NotExsits == CheckPath(output))
                throw new DirectoryNotFoundException("output not exsits");

            var reportPath = Path.Combine(output, $"Report_{DateTime.Now.ToFileTime()}.txt");
            var report = new FileInfo(reportPath);

            using var streamWriter = report.CreateText();

            streamWriter.Write(reportText);
            streamWriter.Close();

            return report;
        }

        public static void SendReportMail(MailConfig mailConfig, string reportText)
        {
            var message = new MimeMessage();
            //寄件者
            message.From.Add(MailboxAddress.Parse(mailConfig.User));
            //收件者
            foreach (string recipient in mailConfig.Recipients)
                message.To.Add(MailboxAddress.Parse(recipient));
            //主旨
            message.Subject = mailConfig.Subject;
            //內容
            var body = new TextPart(TextFormat.Text);
            body.SetText(Encoding.UTF8, reportText);
            message.Body = body;

            using var smtpClient = new SmtpClient();
            smtpClient.Connect(mailConfig.Host);
            smtpClient.Authenticate(mailConfig.User, mailConfig.Password);
            smtpClient.Send(message);
            smtpClient.Disconnect(true);
        }

        private static PathEnum CheckPath(string path)
        {
            if (Directory.Exists(path))
                return PathEnum.Directory;
            if (File.Exists(path))
                return PathEnum.File;
            return PathEnum.NotExsits;
        }
    }
}
