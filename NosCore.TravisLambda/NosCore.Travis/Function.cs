using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NosCore.Shared;
using NosCore.Shared.Discord;
using NosCore.Shared.Enumeration;
using NosCore.Shared.S3;
using JsonSerializer = Amazon.Lambda.Serialization.Json.JsonSerializer;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(JsonSerializer))]

namespace NosCore.Travis
{
    public class Function
    {
        private static readonly HttpClient Client = new HttpClient();
        public string FunctionHandler(InputObject input, ILambdaContext context)
        {
            return TravisCheck(input).Result;
        }

        public static async Task<string> TravisCheck(InputObject input)
        {
            var commitDetails = await GetCommitDetails(input);
            var newList = new Dictionary<RegionType, List<string>>();
            foreach (var type in Enum.GetValues(typeof(RegionType)).Cast<RegionType>())
            {
                newList.Add(type, new List<string>());
            }
            var oldList = S3Helper.GetS3File().Result;
            var country = JsonConvert.DeserializeObject<Dictionary<RegionType, string>>(Environment.GetEnvironmentVariable("language_webhooks"));
            var normal = Environment.GetEnvironmentVariable("dev_webhook");

            var text = await Client.GetStringAsync("https://api.travis-ci.org/v3/job/" + input.Build_Id + "/log.txt");

            bool passed = input.Travis_Test_Result == 0;
            var countTranslation = 0;
            if (input.Travis_Test_Result == 0 || input.Travis_Branch == "master")
            {
                var tasks = new List<Task>();
                foreach (RegionType type in Enum.GetValues(typeof(RegionType)))
                {
                    var reply = text;
                    var start = $"CheckEveryLanguageValueSet ({type})";
                    var pFrom = reply.IndexOf(start, StringComparison.Ordinal) + start.Length;
                    var pTo = reply.Substring(pFrom).IndexOf("Stack Trace:", StringComparison.Ordinal);
                    var leng = pTo < 0 ? 0 : pTo;
                    var result = reply.Substring(pFrom, leng);
                    var results = reply.IndexOf(start, StringComparison.Ordinal) > 0
                        ? result.Split($"{'\r'}{'\n'}").ToList().Skip(3).SkipLast(1).ToArray() : new string[0];
                    var webhook = country[type];
                    var newlist = new List<string>();
                    if (results.Any())
                    {
                        if (results.Except(oldList[type]).Any())
                        {
                            tasks.Add(Task.Run(async () =>
                            {
                                DiscordHelper.SendToDiscord(webhook, new DiscordObject
                                {
                                    Username = "",
                                    Avatar_url = "https://travis-ci.org/images/logos/TravisCI-Mascot-red.png",
                                    Content = "/clear"
                                });
                                await Task.Delay(10000);
                                DiscordHelper.SendToDiscord(webhook, new DiscordObject
                                {
                                    Username = "",
                                    Avatar_url = "https://travis-ci.org/images/logos/TravisCI-Mascot-red.png",
                                    Embeds = CreateEmbeds(results, $"Language {type} Translation Missing!", 15158332,
                                        new List<string>(), false, ref newlist)
                                });
                            }));
                        }

                        var emptylist = new List<string>();
                        var translatedresults = oldList[type].Except(results).ToArray();
                        var embeds = CreateEmbeds(translatedresults,
                            $"Language {type} Translated!", 3066993, new List<string>(), true, ref emptylist);
                        if (embeds.Any())
                        {
                            DiscordHelper.SendToDiscord(webhook, new DiscordObject
                            {
                                Username = "",
                                Avatar_url = "https://travis-ci.org/images/logos/TravisCI-Mascot-blue.png",
                                Embeds = embeds
                            });
                        }

                        countTranslation += translatedresults.Length;
                        newList[type] = results.ToList();
                    }
                    else if (oldList[type].Any() && reply.IndexOf(start, StringComparison.Ordinal) == -1)
                    {
                        var color = 3066993;
                        DiscordHelper.SendToDiscord(webhook, new DiscordObject
                        {
                            Username = "",
                            Avatar_url = "https://travis-ci.org/images/logos/TravisCI-Mascot-blue.png",
                            Embeds = new List<Embed>
                                    {
                                        new Embed
                                        {
                                            Color = color,
                                            Timestamp = DateTime.Now,
                                            Title = $"Not Any Language {type} Translation Missing!"
                                        }
                                    }
                        }
                        );
                    }
                    else
                    {
                        newList[type] = oldList[type];
                    }
                }

                Task.WaitAll(tasks.ToArray());
                S3Helper.UploadS3(newList).Wait();
            }

            if (countTranslation > 0 && !string.IsNullOrEmpty(commitDetails.DiscordName))
            {
                DiscordHelper.SendToDiscord(normal, new DiscordObject
                {
                    Username = "",
                    Avatar_url = "https://travis-ci.org/images/logos/TravisCI-Mascot-blue.png",
                    Content = $"/add-points {commitDetails.DiscordName} TranslationPoint {countTranslation}"
                });
            }
            var discordObject = CraftDiscordObject(passed, input, commitDetails);
            DiscordHelper.SendToDiscord(normal, discordObject);
            return "OK";
        }

        static async Task<CommitDetails> GetCommitDetails(InputObject input)
        {
            var address = "https://github.com/" + input.Travis_Repo_Slug + "/commit/" + input.Travis_Commit;
            var textgithub = await Client.GetStringAsync(address);

            var document = new HtmlDocument();
            document.LoadHtml(textgithub);
            var collection = document.DocumentNode.SelectNodes("//*");
            
            var avatarbody = collection.FirstOrDefault(s => s.HasClass("AvatarStack-body"));
            var title = collection.FirstOrDefault(s => s.Name == "title");
            var commitdesc = collection.FirstOrDefault(s => s.HasClass("commit-desc"));
            var discordName = "";
            if (commitdesc.InnerText.StartsWith("["))
            {
                discordName = new string(commitdesc.InnerText.Skip(1).TakeWhile(s => s != ']').ToArray()).TrimStart('@');
            }

            return new CommitDetails
            {
                Author = avatarbody.Attributes["aria-label"].Value,
                Subject = title.InnerText,
                Message = commitdesc.InnerText,
                DiscordName = discordName
            };
        }

        static DiscordObject CraftDiscordObject(bool passed, InputObject input, CommitDetails commitDetails)
        {
            var status = passed ? "Passed" : "Failed";
            var colortest = passed ? 3066993 : 15158332;
            var icourl = passed ? "https://travis-ci.org/images/logos/TravisCI-Mascot-blue.png" : "https://travis-ci.org/images/logos/TravisCI-Mascot-red.png";

            return new DiscordObject
            {
                Username = "",
                Avatar_url = "https://travis-ci.org/images/logos/TravisCI-Mascot-1.png",
                Embeds = new List<Embed>
                {
                    new Embed
                    {
                        URL = input.Travis_Pull_Request ? "https://github.com/" + input.Travis_Repo_Slug +
                            $"/pull/{input.Travis_Pull_Request}" : "",
                        Author = new Author
                        {
                            Name = $"Tests {status} (Build #{input.Build_Id}) - {input.Travis_Repo_Slug}",
                            Icon_url = icourl,
                            Url = "https://travis-ci.org/" + input.Travis_Repo_Slug + $"/builds/{input.Build_Id}"
                        },
                        Color = colortest,
                        Timestamp = DateTime.Now,
                        Description = $"{commitDetails.Message}{'\n'}{'\n'}{commitDetails.Author} authored",
                        Title = $"{commitDetails.Subject}",
                        Fields = new List<Field>
                        {
                            new Field
                            {
                                Name = "Commit",
                                Value = $"[{input.Travis_Commit}](https://github.com/" + input.Travis_Repo_Slug +
                                    $"/commit/{input.Travis_Commit})",
                                Inline = true

                            },
                            new Field
                            {
                                Name = "Branch/Tag",
                                Value = $"[{input.Travis_Branch}](https://github.com/" + input.Travis_Repo_Slug +
                                    $"/tree/{input.Travis_Branch})",
                                Inline = true

                            }
                        }
                    }
                }
            };
        }

        private static List<Embed> CreateEmbeds(string[] results, string title, int color, List<string> oldList, bool remove, ref List<string> newList)
        {
            var description = new List<string> { string.Empty };
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
                var embed = new Embed
                {
                    Color = color,
                    Description = remove ? $"~~{description[index]}~~" : description[index],
                    Timestamp = DateTime.Now
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

    }
}