using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Zyborg.Firebase
{
    public class Program
    {

        public async Task Go()
        {
            var config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("_IGNORE/appsettings.json", optional: false)
                    .Build();
            var settings = config.Get<AppSettings>();


            // Configures a handler with proxying            
            var h = new HttpClientHandler
            {
                // Proxy = new BasicWebProxy("http://localhost:8888"),
                // UseProxy = true,
                // ServerCertificateCustomValidationCallback = (a,b,c,d) => true,
            };

            var fbs = new FirebaseService(settings.BaseUrl, settings.AuthToken);
            var db = fbs.Database(h);

            // Console.WriteLine("Writing root #1");
            // await db.Put("/root/1", new
            // {
            //     label = "root #1",
            //     createdOn = FirebaseDb.ServerValue.TIMESTAMP,
            // });

            // await db.Put("/root/1/nodes", new
            // {
            //     one = new {
            //         label = "Foo #1"
            //     },
            //     two = new {
            //         label = "Foo #2"
            //     }
            // });

            // await db.PatchMultiPath("/root/1")
            //         .Add("nodes/one", new { label = "Not Foo #1" })
            //         .Add("nodes/two/label", "Foo # 2")
            //         .Apply();

            var newId = await db.Post("/root", new {
                Secret = Guid.NewGuid(),
                Label = "Company ABC",
                CreatedOn = FirebaseDb.ServerValue.TIMESTAMP,

            });
            Console.WriteLine("NewID = " + newId);

            await db.Delete("/root/1");

            Console.WriteLine(await db.Get("/root"));
            Console.WriteLine(await db.Get("/root", "CreatedOn",
                    limitToFirst: 1));
            Console.WriteLine(await db.Get("/root", "Secret",
                    limitToFirst: 1));
        }

        public static void Main(string[] args)
        {
            new Program().Go().Wait();
        }
    }
}
