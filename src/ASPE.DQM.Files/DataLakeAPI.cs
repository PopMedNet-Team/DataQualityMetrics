using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ASPE.DQM.Files
{
    public class DataLakeAPI
    {
        private readonly string _storageAccountURL;
        private readonly string _clientID;
        private readonly string _clientSecret;
        private readonly string _tenantID;
        private readonly string _directory;
        private string access_token;
        public DataLakeAPI(IConfiguration config)
        {
            _storageAccountURL = string.Format("https://{0}.dfs.core.windows.net", config["Files:DataLakeStorageAccountName"]);
            _clientID = config["Files:DataLakeStorageClientID"];
            _clientSecret = config["Files:DataLakeStorageClientSecret"];
            _tenantID = config["Files:DataLakeStorageTenantID"];
            _directory = config["Files:DataLakeStorageDirectory"].ToLower();
        }

        public async Task CreateFile(string fileName)
        {
            if (string.IsNullOrEmpty(access_token))
            {
                await Authenticate();
            }

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_storageAccountURL);
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + access_token);

                var res = await client.PutAsync(string.Format("/{0}/{1}?resource=file", _directory, fileName), new StringContent(""));

                if (res.IsSuccessStatusCode)
                {
                    return;
                }
                else
                {
                    throw new Exception(await res.Content.ReadAsStringAsync());
                }

            }
        }

        public async Task UploadBytes(string fileName, Stream stream, int postion = 0)
        {
            if (string.IsNullOrEmpty(access_token))
            {
                await Authenticate();
            }

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://dqmetricstoragedatalake.dfs.core.windows.net");
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + access_token);

                var res = await client.PatchAsync(string.Format("/{0}/{1}?action=append&position={2}", _directory, fileName, postion), new StreamContent(stream));

                if (res.IsSuccessStatusCode)
                {
                    return;
                }
                else
                {
                    throw new Exception(await res.Content.ReadAsStringAsync());
                }

            }
        }

        public async Task FlushBytes(string fileName, long byteCount)
        {
            if (string.IsNullOrEmpty(access_token))
            {
                await Authenticate();
            }

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://dqmetricstoragedatalake.dfs.core.windows.net");
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + access_token);

                var res = await client.PatchAsync(string.Format("/{0}/{1}?action=flush&position={2}", _directory, fileName, byteCount), new StringContent(""));

                if (res.IsSuccessStatusCode)
                {
                    return;
                }
                else
                {
                    throw new Exception(await res.Content.ReadAsStringAsync());
                }
            }
        }

        public async Task<Stream> GetFile(string fileName)
        {
            if (string.IsNullOrEmpty(access_token))
            {
                await Authenticate();
            }

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_storageAccountURL);
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + access_token);

                var res = await client.GetAsync(string.Format("/{0}/{1}", _directory, fileName));

                if (res.IsSuccessStatusCode)
                {
                    return await res.Content.ReadAsStreamAsync();
                }
                else
                {
                    throw new Exception(await res.Content.ReadAsStringAsync());
                }

            }
        }

        public async Task DeleteFile(string fileName)
        {
            if (string.IsNullOrEmpty(access_token))
            {
                await Authenticate();
            }

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://dqmetricstoragedatalake.dfs.core.windows.net");
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + access_token);

                var res = await client.DeleteAsync(string.Format("/{0}/{1}", _directory, fileName));

                if (res.IsSuccessStatusCode)
                {
                    return;
                }
                else
                {
                    throw new Exception(await res.Content.ReadAsStringAsync());
                }

            }
        }

        public async Task MoveFile(string originalFileName, string newFileName)
        {
            var origFile = await GetFile(originalFileName);
            await CreateFile(newFileName);
            await UploadBytes(newFileName, origFile);
            await FlushBytes(newFileName, origFile.Length);
            await DeleteFile(originalFileName);
        }

        public async Task<IEnumerable<string>> ListFiles(string prefix = null)
        {
            if (string.IsNullOrEmpty(access_token))
            {
                await Authenticate();
            }

            List<string> files = new List<string>();
            bool continuation = true;
            string continuationToken = "";
            while (continuation)
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_storageAccountURL);
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + access_token);

                    var res = await client.GetAsync(string.Format("{0}?recursive=true&resource=filesystem{1}", _directory, string.IsNullOrEmpty(continuationToken) ? "" : $"&continuation={HttpUtility.UrlEncode(continuationToken)}"));
                    if (res.IsSuccessStatusCode)
                    {
                        var body = JsonConvert.DeserializeObject<PathList>(await res.Content.ReadAsStringAsync());
                        IEnumerable<string> headers = new List<string>();
                        res.Headers.TryGetValues("x-ms-continuation", out headers);
                        if (headers != null && headers.Count() > 0)
                        {
                            continuationToken = res.Headers.GetValues("x-ms-continuation").FirstOrDefault();
                        }
                        else
                        {
                            continuationToken = "";
                        }

                        if (string.IsNullOrEmpty(prefix))
                        {
                            foreach (var item in body.paths)
                            {
                                files.Add(item.name);
                            }
                        }
                        else
                        {
                            foreach (var item in body.paths.Where(x => x.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                            {
                                files.Add(item.name);
                            }
                        }

                        if (string.IsNullOrEmpty(continuationToken))
                        {
                            continuation = false;
                        }
                    }
                    else
                    {
                        continuation = false;
                    }

                }
            }
            return files;
        }

        private async Task Authenticate()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://login.microsoftonline.com");

                List<KeyValuePair<string, string>> parameters = new List<KeyValuePair<string, string>>();
                parameters.Add(new KeyValuePair<string, string>("client_id", _clientID));
                parameters.Add(new KeyValuePair<string, string>("client_secret", _clientSecret));
                parameters.Add(new KeyValuePair<string, string>("scope", "https://storage.azure.com/.default"));
                parameters.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));

                var content = new FormUrlEncodedContent(parameters);

                var res = await client.PostAsync(string.Format("{0}/oauth2/v2.0/token", _tenantID), content);

                if (res.IsSuccessStatusCode)
                {
                    var body = JsonConvert.DeserializeObject<AuthResponse>(await res.Content.ReadAsStringAsync());

                    access_token = body.access_token;
                    return;
                }
                else
                {
                    throw new Exception("Unable to Successfully Authenticate");
                }

            }
        }

        public class AuthResponse
        {
            public string token_type { get; set; }
            public int expires_in { get; set; }
            public int ext_expires_in { get; set; }
            public string access_token { get; set; }
        }

        public class PathList
        {
            public IEnumerable<PathItem> paths { get; set; }
        }

        public class PathItem
        {
            public int contentLength { get; set; }
            public string eTag { get; set; }
            public string group { get; set; }
            public bool isDirectory { get; set; }
            public string lastModified { get; set; }
            public string name { get; set; }
            public string owner { get; set; }
            public string permissions { get; set; }
        }
    }
}
