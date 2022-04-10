using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Storage;
using Azure.Storage.Blobs;

namespace HBARTVLScanner.Web.Data;

public class TVLService
{
    private static readonly HttpClient client = new HttpClient();
    private readonly IConfiguration config;

    public TVLService(IConfiguration config)
    {
        this.config = config;
    }

    public async Task<TVL> GetTVLAsync()
    {
        BlobServiceClient blobServiceClient = new BlobServiceClient(new Uri("https://sthbartvl.blob.core.windows.net/"), new StorageSharedKeyCredential("sthbartvl", config["StorageKey"]));
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("hbartvl");

        var last12Blobs = containerClient.GetBlobs().Where(b => b.Properties.LastModified > DateTime.Now.AddHours(-13));
        var latestBlobRef = last12Blobs.OrderByDescending(b => b.Properties.LastModified).FirstOrDefault();
        var oldestBlobRef = last12Blobs.OrderBy(b => b.Properties.LastModified).FirstOrDefault();

        var latestBlobClient = containerClient.GetBlobClient(latestBlobRef.Name);
        var oldestBlobClient = containerClient.GetBlobClient(oldestBlobRef.Name);

        var latestBlobDownload = latestBlobClient.DownloadContent();
        var latestBlobVal = double.Parse(Encoding.UTF8.GetString(latestBlobDownload.Value.Content));

        var oldestBlobDownload = oldestBlobClient.DownloadContent();
        var oldestBlobVal = double.Parse(Encoding.UTF8.GetString(oldestBlobDownload.Value.Content));

        return new TVL { Date = DateTime.ParseExact(latestBlobRef.Name, "yyyyMMddHHmmss", CultureInfo.InvariantCulture), TVLValue = latestBlobVal, TwelveHrChange = latestBlobVal - oldestBlobVal };
    }

    public async Task<double> GetLiveTVL()
    {
        var response = await client.GetAsync("https://v2.api.kabuto.sh/entity/0.0.834116");
        var responseJson = await response.Content.ReadAsStringAsync();

        var obj = JsonSerializer.Deserialize<TVLPayload>(responseJson);
        var decimals = int.Parse(obj.Data.Token.Decimals.ToString());
        var tvl = obj.Data.Token.TotalSupply.ToString();

        var tvlWithDecimal = double.Parse(tvl.Insert(tvl.Length - decimals, "."));
        return tvlWithDecimal;
    }
}

public class TVLPayload
{
    [JsonPropertyName("data")]
    public TVLPayloadData Data { get; set; }
}

public class TVLPayloadData
{
    [JsonPropertyName("token")]
    public TVLPayloadToken Token { get; set; }
}

public class TVLPayloadToken
{
    [JsonPropertyName("decimals")]
    public int Decimals { get; set; }

    [JsonPropertyName("totalSupply")]
    public string TotalSupply { get; set; }
}
