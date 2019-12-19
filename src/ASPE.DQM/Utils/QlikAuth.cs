using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace QlikAuth
{
    public class Ticket
    {
        private X509Certificate2 certificate_ { get; set; }

        public string UserDirectory { get; set; }
        public string UserId { get; set; }
        public StoreLocation CertificateLocation { get; set; }
        public string CertificateThumbprint { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ProxyRestUri { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TargetId { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<Dictionary<string, string>> Attributes { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string SessionId { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool NewUser { get; set; }

        public Ticket()
        {
            CertificateLocation = StoreLocation.CurrentUser;
        }

        public class ResponseData
        {
            public String UserDirectory;
            public String UserId;
            public List<Dictionary<string, string>> Attributes;
            public String Ticket;
            public String TargetUri;
        }

        /// <summary>
        /// Add custom attributes, delimited string
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <param name="delimiters"></param>
        public void AddAttributes(string key, string values, string delimiters = ";")
        {
            AddAttributes(key, values.Split(delimiters.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList());
        }

        /// <summary>
        /// Add custom attributes as List<string>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        public void AddAttributes(string key, List<string> values)
        {
            if (Attributes == null)
                Attributes = new List<Dictionary<string, string>>();

            Attributes.AddRange(values.Select(v => new Dictionary<string, string> { { key, v.Trim() } }).ToList());
        }

        /// <summary>
        /// Generates a randomized string to be used as XrfKey 
        /// </summary>
        /// <returns>16 character randomized string</returns>
        public string GenerateXrfKey()
        {
            const string allowedChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789";
            var chars = new char[16];
            var rd = new Random();

            for (int i = 0; i < 16; i++)
            {
                chars[i] = allowedChars[rd.Next(0, allowedChars.Length)];
            }

            return new string(chars);
        }

        /// <summary>
        /// Requests a ticket and redirects back to where the user came from using TargetUri from the ticket response.
        /// </summary>
        /// <returns>If targetId is not provided in the request a ticket will be returned for manual processing</returns>
        public string TicketRequest()
        {
            Stream stream = Execute("ticket");

            if (stream != null)
            {
                var res = JsonConvert.DeserializeObject<ResponseData>(new StreamReader(stream).ReadToEnd());

                //Return ticket only due to lack of TargetUri
                if (String.IsNullOrEmpty(res.TargetUri))
                    return "qlikTicket=" + res.Ticket;
                else
                {
                    throw new Exception("Unknown error");
                }
            }
            else
            {
                throw new Exception("Unknown error");
            }
        }

        private void LocateCertificate()
        {
            // First locate the Qlik Sense certificate
            X509Store store = new X509Store(StoreName.My, CertificateLocation);
            store.Open(OpenFlags.ReadOnly);
            certificate_ = store.Certificates.Find(X509FindType.FindByThumbprint, CertificateThumbprint, false).Cast<X509Certificate2>().FirstOrDefault();
            store.Close();
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
        }

        private Stream Execute(string endpoint)
        {
            // Get data as json
            var json = JsonConvert.SerializeObject(this);

            //Create URL to REST endpoint for tickets
            Uri url = CombineUri(ProxyRestUri, endpoint);

            //Get certificate
            LocateCertificate();

            //Create the HTTP Request and add required headers and content in Xrfkey
            string xrfkey = GenerateXrfKey();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url + "?Xrfkey=" + xrfkey);

            // Add the method to authentication the user
            request.Method = "POST";
            request.Accept = "application/json";
            request.Headers.Add("X-Qlik-Xrfkey", xrfkey);

            if (certificate_ == null)
                throw new Exception("Certificate not found! Verify AppPool credentials.");

            request.ClientCertificates.Add(certificate_);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(json);

            if (!string.IsNullOrEmpty(json))
            {
                request.ContentType = "application/json";
                request.ContentLength = bodyBytes.Length;
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(bodyBytes, 0, bodyBytes.Length);
                requestStream.Close();
            }

            // make the web request
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return response.GetResponseStream();
        }
        private static Uri CombineUri(string baseUri, string relativeOrAbsoluteUri)
        {
            if (!baseUri.EndsWith("/"))
                baseUri += "/";

            return new Uri(new Uri(baseUri), relativeOrAbsoluteUri);
        }
    }
}
