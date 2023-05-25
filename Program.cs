using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ResourceGraph;
using Azure.ResourceManager.ResourceGraph.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
namespace AzureResourceGraphCSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            var querySecureScoreDetailed =
                "securityresources\r\n| where type == 'microsoft.security/securescores/securescorecontrols'\r\n| extend SecureControl=tostring(properties.displayName), current = properties.score.current,max=todouble(properties.score.max), unhealthy=properties.unhealthyResourceCount, healthy=properties.healthyResourceCount\r\n| project  DT=format_datetime(bin( now(), 1d ),'yyyy-MM-dd'), subscriptionId,SecureControl,healthy,unhealthy,current,max, percentage=iif(max>0,((current/max)*100),double(0))";
            List<string> sb = new List<string>();
            RunGraphAndReturnStringArray(querySecureScoreDetailed, sb);
            var detailed = new List<SecureScoreDetailed>();
            foreach (var s in sb)
            {
                detailed.AddRange(JsonConvert.DeserializeObject<List<SecureScoreDetailed>>(s.ToString()));
            }


            foreach (var s in detailed)
            {
                Console.WriteLine(s.SecureControl + " - " + s.percentage);
            }
        }

        private static void RunGraphAndReturnStringArray(string strQuery, List<string> sb)
        {

            string userAssignedClientId = "<<USER ASSIGNED MANAGED IDENTITY>>";
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = userAssignedClientId });

            var client = new ArmClient(credential);

            var tenant = client.GetTenants().First();

            ResourceQueryContent queryContent;
            string _skipToken = null;
            int totalCount = 0;
            do
            {
                queryContent = new ResourceQueryContent(strQuery)
                {
                    Options = new ResourceQueryRequestOptions()
                    {
                        Top = 5000,
                        SkipToken = _skipToken
                    }
                };
                var response = tenant.GetResources(queryContent);
                var result = response.Value;
                sb.Add(result.Data.ToString());
                totalCount += (int)result.Count;
                _skipToken = result.SkipToken;
            }
            while (_skipToken != null);

        }

        public class SecureScoreDetailed : object
        {
            public DateTime DT { get; set; }
            public string subscriptionId { get; set; }
            public string SecureControl { get; set; }
            public long healthy { get; set; }
            public long unhealthy { get; set; }
            public long current { get; set; }
            public long max { get; set; }
            public long percentage { get; set; }
        }

    }
}
