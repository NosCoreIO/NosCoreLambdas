using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NosCore.Shared.Discord
{
    public class DiscordHelper
    {
        private static readonly HttpClient Client = new HttpClient();
        public static IEnumerable<HttpResponseMessage> SendToDiscord(string webhook, DiscordObject values)
        {
            var embeds = values.Embeds;
            var tasks = new List<HttpResponseMessage>();
            for (var i = 0; (embeds == null && i == 0) || (embeds != null && i < embeds.Count); i += 3)
            {
                values.Embeds = embeds?.Skip(i).Take(3).ToList();
                var contractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                };

                var myContent = JsonConvert.SerializeObject(values, new JsonSerializerSettings
                {
                    ContractResolver = contractResolver,
                    Formatting = Formatting.Indented
                });
                var buffer = Encoding.UTF8.GetBytes(myContent);
                var byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                // we want to keep message in a consistent order.
                tasks.Add(Client.PostAsync(webhook, byteContent).Result);
            }

            return tasks;
        }
    }
}