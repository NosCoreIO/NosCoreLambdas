using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Internal;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace NosCore.Travis
{
    public class Function
    {
        private static readonly HttpClient Client = new HttpClient();
        private const string BucketName = "noscoretranslation";
        private const string KeyName = "missing_translation.json";
        private static IAmazonS3 _client;

        public string FunctionHandler(InputObject input, ILambdaContext context)
        {
            return TravisCheck(input).Result;
        }

        static async Task<Dictionary<RegionType, List<string>>> GetS3File()
        {
            _client = new AmazonS3Client(new BasicAWSCredentials(Environment.GetEnvironmentVariable("accesskey"),
                    Environment.GetEnvironmentVariable("secretkey")),
                RegionEndpoint.USWest2);

            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = BucketName,
                Key = KeyName
            };
            using (GetObjectResponse response = await _client.GetObjectAsync(request))
            using (Stream responseStream = response.ResponseStream)
            using (StreamReader reader = new StreamReader(responseStream))
            {
                return JsonConvert.DeserializeObject<Dictionary<RegionType, List<string>>>(reader.ReadToEnd()); // Now you process the response body.
            }
        }

        static async Task UploadS3(Dictionary<RegionType, List<string>> list)
        {
            _client = new AmazonS3Client(new BasicAWSCredentials(Environment.GetEnvironmentVariable("accesskey"),
                    Environment.GetEnvironmentVariable("secretkey")),
                RegionEndpoint.USWest2);
            var stringtoupload = JsonConvert.SerializeObject(list);
            PutObjectRequest putRequest = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = KeyName,
                ContentType = "text/json",
                InputStream = new MemoryStream(Encoding.UTF8.GetBytes(stringtoupload))
            };

            await _client.PutObjectAsync(putRequest);
        }

        static async Task<string> TravisCheck(InputObject input)
        {
            var newList = new Dictionary<RegionType, List<string>>
            {
                {RegionType.FR, new List<string>()},
                {RegionType.TR, new List<string>()},
                {RegionType.EN, new List<string>()},
                {RegionType.CS, new List<string>()},
                {RegionType.ES, new List<string>()},
                {RegionType.PL, new List<string>()},
                {RegionType.IT, new List<string>()},
                {RegionType.DE, new List<string>()}
            };
            var oldList = GetS3File().Result;
            var address = "https://github.com/" + input.Travis_Repo_Slug + "/commit/" + input.Travis_Commit;
            var textgithub = await Client.GetStringAsync(address);
            var patternBuilder = new StringBuilder();
            patternBuilder.Append(@"class=""AvatarStack-body"" aria-label=""(.*)"">");
            var match = Regex.Match(textgithub, patternBuilder.ToString(), RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var authorName = match.Groups[1].ToString();
            patternBuilder = new StringBuilder();
            patternBuilder.Append(@"<title>(.*) · " + input.Travis_Repo_Slug);
            match = Regex.Match(textgithub, patternBuilder.ToString(), RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var commitSubject = match.Groups[1].ToString();
            patternBuilder = new StringBuilder();
            patternBuilder.Append(@"<div class=""commit-desc""><pre>(.*)</pre></div>");
            match = Regex.Match(textgithub, patternBuilder.ToString(), RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var commitMessage = match.Groups[1].ToString();

            var text = await Client.GetStringAsync("https://api.travis-ci.org/v3/job/" + input.Build_Id + "/log.txt");
            var country = JsonConvert.DeserializeObject<Dictionary<RegionType, string>>(Environment.GetEnvironmentVariable("language_webhooks"));
            var normal = Environment.GetEnvironmentVariable("dev_webhook");
            string status;
            int colortest;
            string icourl;
            if (input.Travis_Test_Result == 0)
            {
                if (input.Travis_Branch == "master")
                {
                    foreach (RegionType type in Enum.GetValues(typeof(RegionType)))
                    {
                        var reply = text;
                        var start = $"CheckEveryLanguageValueSet ({type})";
                        var pFrom = reply.IndexOf(start, StringComparison.Ordinal) + start.Length;
                        var pTo = reply.Substring(pFrom).IndexOf("Stack Trace:", StringComparison.Ordinal);
                        var leng = pTo < 0 ? 0 : pTo;
                        var result = reply.Substring(pFrom, leng);
                        var results = reply.IndexOf(start) > 0
                            ? result.Split($"{'\r'}{'\n'}").ToList().Skip(3).SkipLast(1).ToArray() : new string[0];
                        var webhook = country[type];
                        if (results.Any())
                        {
                            var embeds = CreateEmbeds(results, $"Language {type} Translation Missing!", 15158332, oldList[type], newList[type], false);

                            if (embeds.Any())
                            {
                                SendToDiscord(webhook, new DiscordObject
                                {
                                    Username = "",
                                    Avatar_url = "https://travis-ci.org/images/logos/TravisCI-Mascot-red.png",
                                    Embeds = embeds
                                });
                            }

                            UploadS3(newList).Wait();
                            embeds = CreateEmbeds(oldList[type].Except(newList[type]).ToArray(), $"Language {type} Translated!", 3066993, new List<string>(), new List<string>(), true);
                            if (embeds.Any())
                            {
                                SendToDiscord(webhook, new DiscordObject
                                {
                                    Username = "",
                                    Avatar_url = "https://travis-ci.org/images/logos/TravisCI-Mascot-blue.png",
                                    Embeds = embeds
                                });
                            }
                        }
                        else if (oldList[type].Any())
                        {
                            var color = 3066993;
                            SendToDiscord(webhook, new DiscordObject
                            {
                                Username = "",
                                Avatar_url = "https://travis-ci.org/images/logos/TravisCI-Mascot-blue.png",
                                Embeds = new List<Embed>{new Embed()
                            {
                                Color = color,
                                Timestamp = DateTime.Now,
                                Title = $"Not Any Language {type} Translation Missing!",
                            }}
                            }
                            );
                        }
                    }
                }


                status = "Passed";
                colortest = 3066993;
                icourl = "https://travis-ci.org/images/logos/TravisCI-Mascot-blue.png";
            }
            else
            {
                status = "Failed";
                colortest = 15158332;
                icourl = "https://travis-ci.org/images/logos/TravisCI-Mascot-red.png";
            }

            SendToDiscord(normal, new DiscordObject
            {
                Username = "",
                Avatar_url = "https://travis-ci.org/images/logos/TravisCI-Mascot-1.png",
                Embeds = new List<Embed>
                {  new Embed()
                {
                    URL = input.Travis_Pull_Request ? "https://github.com/" + input.Travis_Repo_Slug + $"/pull/{input.Travis_Pull_Request}" : "",
                    Author = new Author()
                    {
                        Name = $"Tests {status} (Build #{input.Build_Id}) - {input.Travis_Repo_Slug}",
                        Icon_url = icourl,
                        Url = "https://travis-ci.org/" + input.Travis_Repo_Slug + $"/builds/{input.Build_Id}"
                    },
                    Color = colortest,
                    Timestamp = DateTime.Now,
                    Description = $"{commitMessage}{'\n'}{'\n'}{authorName} authored",
                    Title = $"{commitSubject}",
                    Fields = new List<Field>()
                    {
                        new Field()
                        {
                            Name="Commit",
                            Value = $"[{input.Travis_Commit}](https://github.com/"+input.Travis_Repo_Slug+$"/commit/{input.Travis_Commit})",
                            Inline = true

                        },
                        new Field()
                        {
                            Name="Branch/Tag",
                            Value = $"[{input.Travis_Branch}](https://github.com/"+input.Travis_Repo_Slug+$"/tree/{input.Travis_Branch})",
                            Inline = true

                        }
                    }
                }}
            });
            return "OK";
        }

        private static List<Embed> CreateEmbeds(string[] results, string title, int color, List<string> newList, List<string> oldList, bool remove)
        {
            var description = new List<string>() { string.Empty };
            foreach (var lkey in results)
            {
                if (description.Last().Length + (lkey + '\n').Length >= 2000)
                {
                    description.Add(string.Empty);
                }
                newList.Add(lkey);
                if (!oldList.Exists(s => s == lkey))
                {
                    description[description.Count - 1] += lkey + '\n';
                }
            }

            var embeds = new List<Embed>();
            for (int index = 0; index < description.Count; index++)
            {
                var embed = new Embed()
                {
                    Color = color,
                    Description = remove ? $"~~{description[index]}~~" : description[index],
                    Timestamp = DateTime.Now,
                };
                if (index == 0)
                {
                    embed.Title = title;
                }
                if (description[index] != string.Empty)
                {
                    embeds.Add(embed);
                }
            }

            return embeds;
        }

        private static HttpResponseMessage SendToDiscord(string webhook, object values)
        {
            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            var myContent = JsonConvert.SerializeObject(values, new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                Formatting = Formatting.Indented
            });
            var buffer = System.Text.Encoding.UTF8.GetBytes(myContent);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return Client.PostAsync(webhook, byteContent).Result;
        }
    }
}