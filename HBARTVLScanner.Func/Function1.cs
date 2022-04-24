using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;
using HBARTVLScanner.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace HBARTVLScanner.Func
{
    public class Function1
    {
        private static readonly HttpClient client = new HttpClient();

        [FunctionName("Function1")]
        public async Task Run([TimerTrigger("0 0 */1 * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var response = await client.GetAsync("https://v2.api.kabuto.sh/entity/0.0.834119");
            var responseJson = await response.Content.ReadAsStringAsync();

            var obj = JsonSerializer.Deserialize<ContractPayload>(responseJson);
            var decimals = int.Parse(Environment.GetEnvironmentVariable("ContractDecimals"));

            var tvl = obj.Data.Contract.Balance.ToString();

            var tvlWithDecimal = tvl.Insert(tvl.Length - decimals, ".");

            BlobServiceClient blobServiceClient = new BlobServiceClient(new Uri("https://sthbartvl.blob.core.windows.net/"), new StorageSharedKeyCredential("sthbartvl", Environment.GetEnvironmentVariable("StorageKey")));
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("hbartvl");
            BlobClient blobClient = containerClient.GetBlobClient(DateTime.UtcNow.ToString("yyyyMMddHHmmss"));

            using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(tvlWithDecimal));
            var blobResult = await blobClient.UploadAsync(memoryStream);
        }
    }
}
