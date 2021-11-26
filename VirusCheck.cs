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

        public static async Task<List<FileHash>> ScanHashs(IList<FileHash> hashs)
        {
            for (int i = 0; i < hashs.Count(); i++)
            {
                if (!config.ScanExtension.Any(se => hashs[i].Name.EndsWith(se, StringComparison.OrdinalIgnoreCase)))
                    continue;
                hashs[i] = await GetCheckResult(hashs[i]);
            }

            return hashs.ToList();
        }

        private static async Task<FileHash> GetCheckResult(FileHash hash)
        {
            using var httpClient = HttpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("x-apikey", config.ApiKey);
            var response = await httpClient.GetAsync($"{VIRUS_CHECK_SERVER_URL}/{hash.Hash}");
            var c = await response.Content.ReadAsStringAsync();
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
            
            return hash;
        }
    }
}
