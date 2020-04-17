using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;
using NosCore.Shared.Enumerations;

namespace NosCore.Shared.S3
{
    public class S3Helper
    {
        private const string BucketName = "noscoretranslation";
        private const string KeyName = "missing_translation.json";
        private static IAmazonS3 _client;
        public static async Task<Dictionary<RegionType, List<string>>> GetS3File()
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
                return JsonConvert
                    .DeserializeObject<Dictionary<RegionType, List<string>>
                    >(reader.ReadToEnd()); // Now you process the response body.
            }
        }

        public static async Task UploadS3(Dictionary<RegionType, List<string>> list)
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
    }
}