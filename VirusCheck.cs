using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CheckSum
{
    class VirusCheck
    {
        private static VirusTotalConfig config = null;
        private static IHttpClientFactory _httpClientFactory;
        private const string VIRUS_CHECK_SERVER_URL = "https://www.virustotal.com/api/v3/files";

        private static IHttpClientFactory HttpClientFactory
        {
            get
            {
                if (_httpClientFactory == null)
                {
                    _httpClientFactory = new ServiceCollection().AddHttpClient().BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
                }

                return _httpClientFactory;
            }
        }

        public static void LoadConfig(VirusTotalConfig config)
        {
            VirusCheck.config = config;
        }

        public static async Task<List<FileHash>> ScanHashs(IEnumerable<FileHash> hashs)
        {
            var tasks = new List<Task>();

            var scanExtensionSet = config.ScanExtension.Select(ext => $".{ext.ToLower()}").ToHashSet();
            var scanHashs = hashs.Where(h => scanExtensionSet.Contains(Path.GetExtension(h.Name)));

            foreach (var hash in scanHashs)
            {
                tasks.Add(GetCheckResult(hash));
            }

            await Task.WhenAll(tasks);

            return hashs.ToList();
        }

        private static async Task GetCheckResult(FileHash hash)
        {
            using var httpClient = HttpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("x-apikey", config.ApiKey);
            using var response = await httpClient.GetAsync($"{VIRUS_CHECK_SERVER_URL}/{hash.Hash}");

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var content = await response.Content.ReadAsStringAsync();

                    var analysisStats = JsonDocument.Parse(content).RootElement
                       .GetProperty("data")
                       .GetProperty("attributes")
                       .GetProperty("last_analysis_stats");

                    var suspicious = analysisStats.GetProperty("suspicious").GetInt32();
                    var malicious = analysisStats.GetProperty("malicious").GetInt32();

                    if ((suspicious + malicious) > 0)
                        hash.IsSuspiciousFile = true;
                }
                finally { }
            }
        }
    }
}
