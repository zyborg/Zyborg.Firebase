using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceStack;

namespace Zyborg.Firebase
{
    public class FirebaseDb : IDisposable
    {
        public const string ORDER_BY_KEY = "$key";

        public const string ORDER_BY_VALUE = "$value";

        public const string ORDER_BY_PRIORITY = "$priority";

        private HttpClient _http;
        private HttpClientHandler _handler;

        public FirebaseDb(FirebaseService service, HttpClientHandler handler = null)
        {
            Service = service;

            if (handler == null)
                handler = new HttpClientHandler();

            _handler = handler;
            _http = new HttpClient(_handler);

            _http.BaseAddress = new Uri(Service.BaseUrl);
        }

        // public void Subscribe()
        // {
        //     var seClient = new ServerEventsClient(_http.BaseAddress.ToString() + "?auth=" + Service.AuthToken)
        //     {
        //         OnConnect = x => Console.WriteLine("CON: " + x),
        //         OnHeartbeat = () => Console.WriteLine("BEAT"),
        //         OnCommand = x => Console.WriteLine("CMD: " + x),
        //         OnMessage = x => Console.WriteLine("MSG: " + x),
        //         OnException = x => Console.WriteLine("EX!: " + x),
        //     }.Start();

        //     var svcClient = (JsonServiceClient)seClient.ServiceClient;
        //     svcClient.Proxy = new BasicWebProxy("http://localhost:8888");
        //     //svcClient.Accept = "text/event-stream";
        // }

        public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path,
                string content = null, params object[] queryParams)
        {
            string query = null;

            if (queryParams?.Length > 0)
            {
                if (queryParams.Length % 2 == 1)
                    throw new ArgumentException("query parameters have to be provided as"
                            + " an even number of name-value pairs", nameof(queryParams));
                query = string.Join("", queryParams.Select((p,i) =>
                {
                    if (i % 2 == 0)
                        return p + "=";
                    else
                        return p + "&";
                }));
            }

            if (!path.EndsWith(".json"))
                path += ".json";

            if (!string.IsNullOrEmpty(Service.AuthToken))
                if (query == null)
                    query = $"auth={Service.AuthToken}";
                else
                    query += $"&auth={Service.AuthToken}";

            if (query != null)
                path += $"?{query}";

            using (var requ = new HttpRequestMessage())
            {
                requ.Method = method;
                requ.RequestUri = new Uri(path, UriKind.Relative);

                if (content != null)
                    requ.Content = new StringContent(content);

                var resp = await _http.SendAsync(requ);
                resp.EnsureSuccessStatusCode();
                return resp;
            }
        }

        public FirebaseService Service
        { get; private set; }

        public async Task Put(string path, object value)
        {
            var jsonValue = JsonConvert.SerializeObject(value);
            await PutRaw(path, jsonValue);
        }

        public async Task PutRaw(string path, string jsonValue)
        {
            var resp = await SendAsync(HttpMethod.Put, path, jsonValue);
            Console.WriteLine(resp);
        }

        public async Task Patch(string path, object value)
        {
            var jsonValue = JsonConvert.SerializeObject(value);
            await PatchRaw(path, jsonValue);
        }

        public MultiPathPatch PatchMultiPath(string path)
        {
            return new MultiPathPatch(this, path);
        }

        public async Task PatchRaw(string path, string jsonValue)
        {
            var resp = await SendAsync(FirebaseService.PATCH, path, jsonValue);
            Console.WriteLine(resp);
        }

        public async Task<string> Post(string path, object value)
        {
            var jsonValue = JsonConvert.SerializeObject(value);
            var resp = await PostRaw(path, jsonValue);

            var postResp = JsonConvert.DeserializeObject<PostResponse>(resp);
            return postResp.Name;
        }

        public async Task<string> PostRaw(string path, string jsonValue)
        {
            var resp = await SendAsync(HttpMethod.Post, path, jsonValue);
            return await resp.Content.ReadAsStringAsync();
        }

        public async Task Delete(string path)
        {
            var resp = await SendAsync(HttpMethod.Delete, path);
        }

        /// <summary>
        /// Retrieves node(s) at the specified path, optionally using
        /// a shallow result.
        /// </summary>
        /// <param name="path">path to the node or node collection to return</param>
        /// <param name="shallow">if true, only the first-level children
        ///   are returned, and only as booleans indicating their existence</param>
        public async Task<string> Get(string path, bool shallow = false)
        {
            var resp = await SendAsync(HttpMethod.Get, path, null,
                    nameof(shallow), shallow);
            return await resp.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Retrieves node(s) at the specified path, matching the criteria
        /// defined by one or more query filter parameters.
        /// </summary>
        /// <param name="path">path to the node or node collection to return</param>
        /// <param name="orderBy">identifies the element to sort the result by
        ///    specified as either a child key name or a grand-child key path,
        ///    or the special values <see cref="ORDER_BY_KEY"/> to indicate the
        ///    immediate node keys or <see cref="ORDER_BY_VALUE"/> to indicate the
        ///    immediate node values</param>
        /// <param name="equalTo">specifies a matching value against the element
        ///    specified in <c>orderBy</c></param>
        /// <param name="startAt">specifies a range starting value (inclusive) against the element
        ///    specified in <c>orderBy</c></param>
        /// <param name="endAt">specifies a range ending value (inclusive) against the element
        ///    specified in <c>orderBy</c></param>
        /// <param name="limitToFirst">restricts the final result to the first
        ///    number of matching nodes</param>
        /// <param name="limitToLast">restrics the final result to the last
        ///    number of matching nodes</param>
        /// <returns></returns>
        public async Task<string> Get(string path, string orderBy,
                string equalTo = null,
                string startAt = null,
                string endAt = null,
                int limitToFirst = -1,
                int limitToLast = -1)
        {
            var queryParams = new List<string>();
            queryParams.Add(nameof(orderBy));
            queryParams.Add($"\"{orderBy}\"");
            if (equalTo != null)
            {
                queryParams.Add(nameof(equalTo));
                queryParams.Add(equalTo);
            }
            if (startAt != null)
            {
                queryParams.Add(nameof(startAt));
                queryParams.Add(startAt);
            }
            if (endAt != null)
            {
                queryParams.Add(nameof(endAt));
                queryParams.Add(endAt);
            }
            if (limitToFirst != -1)
            {
                queryParams.Add(nameof(limitToFirst));
                queryParams.Add(limitToFirst.ToString());
            }
            if (limitToLast != -1)
            {
                queryParams.Add(nameof(limitToLast));
                queryParams.Add(limitToLast.ToString());
            }

            var resp = await SendAsync(HttpMethod.Get, path,
                    queryParams: queryParams.ToArray());
            return await resp.Content.ReadAsStringAsync();
        }

        #region -- IDisposable Support --

        public bool IsDisposed
        { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    _http.Dispose();
                    _handler.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                IsDisposed = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FirebaseDb() {
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

        #endregion -- IDisposable Support --

        public class PostResponse
        {
            [JsonProperty("name")]
            public string  Name
            { get; set; }
        }

        public class ServerValue
        {
            public static readonly ServerValue TIMESTAMP = new ServerValue("timestamp");

            [JsonExtensionData]
            private Dictionary<string, JToken> _properties = new Dictionary<string, JToken>();

            private ServerValue(string value)
            {
                _properties[".sv"] = JToken.FromObject(value);
            }
        }

        public class MultiPathPatch
        {
            private Dictionary<string, object> _multiPathPatch = new Dictionary<string, object>();

            internal MultiPathPatch(FirebaseDb db, string path)
            {
                Database = db;
                Path = path;
            }

            public FirebaseDb Database
            { get; private set; }

            public string Path
            { get; private set; }

            public MultiPathPatch Add(string subpath, object value)
            {
                _multiPathPatch[subpath] = value;
                return this;
            }

            public async Task Apply()
            {
                await Database.Patch(Path, _multiPathPatch);
            }
        }
    }
}