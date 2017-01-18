using System.Net.Http;
using System.Threading.Tasks;

namespace Zyborg.Firebase
{
    public class FirebaseService
    {
        public static readonly HttpMethod PATCH = new HttpMethod("PATCH");

        public FirebaseService(string baseUrl, string authToken = null)
        {
            BaseUrl = baseUrl;
            AuthToken = authToken;
        }

        public string BaseUrl
        { get; private set; }

        internal string AuthToken
        { get; private set; }

        public FirebaseDb Database(HttpClientHandler handler = null)
        {
            return new FirebaseDb(this, handler);
        }
    }
}