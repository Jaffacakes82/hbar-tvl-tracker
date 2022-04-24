using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Storage;
using Azure.Storage.Blobs;
using HBARTVLScanner.Core;

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

        var latestBlobClient = containerClient.GetBlobClient(latestBlobRef?.Name);
        var oldestBlobClient = containerClient.GetBlobClient(oldestBlobRef?.Name);

        var latestBlobDownload = latestBlobClient.DownloadContent();
        var latestBlobVal = double.Parse(Encoding.UTF8.GetString(latestBlobDownload.Value.Content));

        var oldestBlobDownload = oldestBlobClient.DownloadContent();
        var oldestBlobVal = double.Parse(Encoding.UTF8.GetString(oldestBlobDownload.Value.Content));

        return new TVL { Date = DateTime.ParseExact(latestBlobRef.Name, "yyyyMMddHHmmss", CultureInfo.InvariantCulture), TVLValue = latestBlobVal, TwelveHrChange = latestBlobVal - oldestBlobVal };
    }

    public async Task<double> GetLiveTVL()
    {
        var response = await client.GetAsync("https://v2.api.kabuto.sh/entity/0.0.834119");
        var responseJson = await response.Content.ReadAsStringAsync();

        var obj = JsonSerializer.Deserialize<ContractPayload>(responseJson);
        var decimals = int.Parse(config["ContractDecimals"]);
        var tvl = obj?.Data?.Contract?.Balance?.ToString();

        var tvlWithDecimal = double.Parse(tvl.Insert(tvl.Length - decimals, "."));
        return tvlWithDecimal;
    }

    public async Task<string> GetCurrentExchangeRate()
    {
        // Get contract TVL
        var response = await client.GetAsync("https://v2.api.kabuto.sh/entity/0.0.834119");
        var responseJson = await response.Content.ReadAsStringAsync();

        var obj = JsonSerializer.Deserialize<ContractPayload>(responseJson);
        var decimals = int.Parse(config["ContractDecimals"]);
        var tvl = obj.Data.Contract.Balance.ToString();

        var tvlWithDecimal = double.Parse(tvl.Insert(tvl.Length - decimals, "."));

        // Get HBARX token supply
        var tokenResponse = await client.GetAsync("https://v2.api.kabuto.sh/entity/0.0.834116");
        var tokenResponseJson = await tokenResponse.Content.ReadAsStringAsync();

        var tokenObj = JsonSerializer.Deserialize<TokenPayload>(tokenResponseJson);

        var tokenDecimals = int.Parse(tokenObj.Data.Token.Decimals.ToString());
        var tokenSupply = tokenObj.Data.Token.TotalSupply.ToString();

        var tokenSupplyWithDecimal = double.Parse(tokenSupply.Insert(tokenSupply.Length - tokenDecimals, "."));

        return (tvlWithDecimal / tokenSupplyWithDecimal).ToString("N4");
    }
}