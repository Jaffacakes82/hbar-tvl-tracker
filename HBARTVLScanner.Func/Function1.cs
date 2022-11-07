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
using Microsoft.Extensions.Logging;

namespace HBARTVLScanner.Func
{
    public class Function1
    {
        private static readonly HttpClient client = new HttpClient();

        [FunctionName("Function1")]
        public async Task RunTVL([TimerTrigger("0 0 */1 * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var response = await client.GetAsync("https://mainnet-public.mirrornode.hedera.com/api/v1/accounts/0.0.1412503");
            var responseJson = await response.Content.ReadAsStringAsync();

            var obj = JsonSerializer.Deserialize<ContractPayload>(responseJson);
            var decimals = int.Parse(Environment.GetEnvironmentVariable("ContractDecimals"));

            var tvl = obj.Balance.Balance.ToString();

            var tvlWithDecimal = tvl.Insert(tvl.Length - decimals, ".");

            BlobServiceClient blobServiceClient = new BlobServiceClient(new Uri("https://sthbartvl.blob.core.windows.net/"), new StorageSharedKeyCredential("sthbartvl", Environment.GetEnvironmentVariable("StorageKey")));
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("hbartvl");
            BlobClient blobClient = containerClient.GetBlobClient(DateTime.UtcNow.ToString("yyyyMMddHHmmss"));

            using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(tvlWithDecimal));
            var blobResult = await blobClient.UploadAsync(memoryStream);
        }

        [FunctionName("Function2")]
        public async Task RunRewards([TimerTrigger("0 0 7 * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var response = await client.GetAsync("https://mainnet-public.mirrornode.hedera.com/api/v1/transactions?limit=100&account.id=0.0.833842");
            var responseJson = await response.Content.ReadAsStringAsync();

            BlobServiceClient blobServiceClient = new BlobServiceClient(new Uri("https://sthbartvl.blob.core.windows.net/"), new StorageSharedKeyCredential("sthbartvl", Environment.GetEnvironmentVariable("StorageKey")));
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("hbarrewards");
            BlobClient blobClient = containerClient.GetBlobClient(DateTime.UtcNow.ToString("yyyyMMddHHmmss"));

            using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(responseJson));
            var blobResult = await blobClient.UploadAsync(memoryStream);
        }
    }
}
