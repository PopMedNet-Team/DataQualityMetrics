using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace PMN.Sync
{
    public class HttpSynchronizer : ISynchronizer
    {
        readonly ILogger<HttpSynchronizer> Logger;
        readonly IConfiguration Config;
        readonly string SyncServiceKey;
        readonly string SyncUrl;
        readonly System.Net.Http.HttpClient Web;
        readonly System.Net.Http.ByteArrayContent NullPostContent;
        readonly Dictionary<Guid, string> RolesToClaimsLookup;

        public HttpSynchronizer(IConfiguration config, ILogger<HttpSynchronizer> logger)
        {
            Logger = logger;
            Config = config;

            var syncConfig = config.GetSection("Sync");
            SyncUrl = syncConfig.GetValue<string>("Url");
            SyncServiceKey = syncConfig.GetValue<string>("ServiceKey");
            SyncInterval = TimeSpan.FromSeconds(syncConfig.GetValue<int>("Interval"));
            MaxErrorCount = syncConfig.GetValue<int>("MaxErrors");
            MaxErrorsWithinHours = syncConfig.GetValue<double>("MaxErrorsWithinHours");

            Web = new System.Net.Http.HttpClient();
            Web.BaseAddress = new Uri(SyncUrl, UriKind.Absolute);
            Web.DefaultRequestHeaders.Add("ServiceKey", SyncServiceKey);
            Web.Timeout = TimeSpan.FromSeconds(25);

            NullPostContent = new System.Net.Http.ByteArrayContent(new byte[0]);
            NullPostContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            SyncID = Guid.Empty;

            var rolesConfig = config.GetSection("AuthorizationRoles");
            EveryoneSecurityGroupID = Guid.Parse(rolesConfig["Everyone"]);

            SecurityGroupIDs = rolesConfig.AsEnumerable().Skip(1).Where(c => !string.IsNullOrEmpty(c.Value)).Select(c => Guid.Parse(c.Value)).ToArray();

            RolesToClaimsLookup = rolesConfig.AsEnumerable().Skip(1).ToDictionary(c => Guid.Parse(c.Value), c => c.Key.Split(':')[1]);
        }

        public Guid EveryoneSecurityGroupID { get; }

        public IEnumerable<Guid> SecurityGroupIDs { get; }

        public TimeSpan SyncInterval { get; }

        public int MaxErrorCount { get; }

        public double MaxErrorsWithinHours { get; }

        public Guid SyncID { get; private set; }

        public async Task StartJobAsync()
        {
            using (var syncStartItem = await Web.PostAsync("Start", NullPostContent).ConfigureAwait(false))
            {
                syncStartItem.EnsureSuccessStatusCode();

                using (var stream = await syncStartItem.Content.ReadAsStreamAsync())
                using (var reader = new Newtonsoft.Json.JsonTextReader(new System.IO.StreamReader(stream)))
                {
                    var serializer = new Newtonsoft.Json.JsonSerializer();
                    var obj = serializer.Deserialize<StartSyncJobResponse>(reader);
                    SyncID = obj.id;
                }
            }
        }

        public async Task UpdateUsersAsync(IEnumerable<Data.UserSyncItemDTO> users, List<Guid> deletedUserIDs)
        {
            System.Net.Http.HttpContent updateUsersHttpContent = SerializeToContent(new
            {
                JobID = SyncID,
                Users = users.Select(u => {
                    var claims = new List<KeyValuePair<string, string>>();
                    claims.AddRange(u.SecurityGroups.Where(sg => sg != EveryoneSecurityGroupID).Select(sg => new KeyValuePair<string, string>(RolesToClaimsLookup[sg], RolesToClaimsLookup[sg])).ToArray());

                    if (!string.IsNullOrEmpty(u.FirstName))
                        claims.Add(new KeyValuePair<string, string>(ASPE.DQM.Identity.Claims.FirstName_Key, u.FirstName));

                    if (!string.IsNullOrEmpty(u.LastName))
                        claims.Add(new KeyValuePair<string, string>(ASPE.DQM.Identity.Claims.LastName_Key, u.LastName));

                    if (!string.IsNullOrEmpty(u.Phone))
                        claims.Add(new KeyValuePair<string, string>(ASPE.DQM.Identity.Claims.Phone_Key, u.Phone));

                    claims.Add(new KeyValuePair<string, string>(ASPE.DQM.Identity.Claims.Organization_Key, u.Organization));

                    u.Claims = claims;
                    return u;
                })
            });

            using (updateUsersHttpContent)
            using (var response = await Web.PostAsync("update-users", updateUsersHttpContent).ConfigureAwait(false))
            {
                if (response.IsSuccessStatusCode == false)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Logger.LogError($"Error ADDING OR UPDATING users: { error }");
                }
                else
                {
                    //remove the reactivated users from the already deleted list
                    deletedUserIDs.RemoveAll(id => users.Any(u => u.ID == id));
                }

            }
        }

        public async Task<List<Guid>> DisableUsersAsync(IEnumerable<Data.UserSyncItemDTO> usersToDisable, List<Guid> deletedUserIDs)
        {
            var disableContent = new { JobID = SyncID, Users = usersToDisable.Where(u => !deletedUserIDs.Contains(u.ID)).ToArray() };
            var disableUsersHttpContent = SerializeToContent(disableContent);

            using (disableUsersHttpContent)
            using (var response = await Web.PostAsync("disable-users", disableUsersHttpContent).ConfigureAwait(false))
            {
                if (response.IsSuccessStatusCode == false)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Logger.LogError($"Error DISABLING users: { error }");
                    return deletedUserIDs;
                }
                else
                {
                    //update the already deleted users list
                    return new List<Guid>(usersToDisable.Select(u => u.ID).ToArray());
                }
            }
        }

        public async Task StopJobAsync()
        {
            var stopSyncHttpContent = SerializeToContent(new { JobID = SyncID });
            using (stopSyncHttpContent)
            using (var response = await Web.PostAsync("stop", stopSyncHttpContent).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        System.Net.Http.HttpContent SerializeToContent(object obj)
        {
            var ms = new System.IO.MemoryStream();
            using (var sw = new System.IO.StreamWriter(ms, new UTF8Encoding(false), 1024, true))
            using (var jtw = new Newtonsoft.Json.JsonTextWriter(sw) { Formatting = Formatting.None })
            {
                var js = new JsonSerializer();
                js.Serialize(jtw, obj);
                jtw.Flush();
            }

            ms.Seek(0, System.IO.SeekOrigin.Begin);
            System.Net.Http.HttpContent httpContent = new System.Net.Http.StreamContent(ms);
            httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            return httpContent;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    NullPostContent.Dispose();
                    Web.Dispose();
                }

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~HttpSynchronizer() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion





    }
}
