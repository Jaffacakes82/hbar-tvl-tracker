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

    public async Task<TVLStats> GetTVLAsync()
    {
        try
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(new Uri("https://sthbartvl.blob.core.windows.net/"), new StorageSharedKeyCredential("sthbartvl", config["StorageKey"]));
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("hbartvl");

            var last12Blobs = containerClient.GetBlobs().Where(b => b.Properties.LastModified > DateTime.Now.AddHours(-25));
            var latestBlobRef = last12Blobs.OrderByDescending(b => b.Properties.LastModified).FirstOrDefault();
            var oldestBlobRef = last12Blobs.OrderBy(b => b.Properties.LastModified).FirstOrDefault();

            var latestBlobClient = containerClient.GetBlobClient(latestBlobRef?.Name);
            var oldestBlobClient = containerClient.GetBlobClient(oldestBlobRef?.Name);

            var latestBlobDownload = latestBlobClient.DownloadContent();
            var latestBlobVal = double.Parse(Encoding.UTF8.GetString(latestBlobDownload.Value.Content));

            var oldestBlobDownload = oldestBlobClient.DownloadContent();
            var oldestBlobVal = double.Parse(Encoding.UTF8.GetString(oldestBlobDownload.Value.Content));

            return new TVLStats { Date = DateTime.ParseExact(latestBlobRef.Name, "yyyyMMddHHmmss", CultureInfo.InvariantCulture), SnapshotTVLValue = latestBlobVal, TwentyFourHrChange = latestBlobVal - oldestBlobVal };
        }
        catch (Exception e)
        {
            return new TVLStats { Date = DateTime.Now, SnapshotTVLValue = 0, TwentyFourHrChange = 0 };
        }
    }

    public async Task<double> GetLiveTVL()
    {
        var response = await client.GetAsync("https://mainnet-public.mirrornode.hedera.com/api/v1/accounts/0.0.1412503");
        var responseJson = await response.Content.ReadAsStringAsync();

        var obj = JsonSerializer.Deserialize<ContractPayload>(responseJson);
        var decimals = int.Parse(config["ContractDecimals"]);
        var tvl = obj?.Balance?.Balance?.ToString("F0");

        var tvlWithDecimal = double.Parse(tvl.Insert(tvl.Length - decimals, "."));
        return tvlWithDecimal;
    }

    public async Task<IList<StakePoolReward>> GetStakingOnlyRewards()
    {
        BlobServiceClient blobServiceClient = new BlobServiceClient(new Uri("https://sthbartvl.blob.core.windows.net/"), new StorageSharedKeyCredential("sthbartvl", config["StorageKey"]));
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("hbarrewards");

        var allBlobs = containerClient.GetBlobs();
        var allTransactions = new List<Transaction>();

        foreach (var blob in allBlobs)
        {
            var blobClient = containerClient.GetBlobClient(blob.Name);
            var blobContent = await blobClient.DownloadContentAsync();
            var blobContentString = Encoding.UTF8.GetString(blobContent.Value.Content);
            var transactionPayload = JsonSerializer.Deserialize<TransactionsPayload>(blobContentString);
            allTransactions.AddRange(transactionPayload.Transactions);
        }

        var distinctTransactions = allTransactions
            .Where(t => t.Type == "CONTRACTCALL")
            .GroupBy(t => t.Id)
            .Select(t => t.First())
            .ToList();

        var rewards = new List<StakePoolReward>();

        foreach (var tran in distinctTransactions)
        {
            var rewardAfterFeeAmount = tran.Transfers.Where(tran => tran.AccountId == "0.0.1027588").FirstOrDefault()?.Amount.ToString();

            if (!string.IsNullOrWhiteSpace(rewardAfterFeeAmount))
            {
                var rewardAfterFeeAmountWithDecimal = rewardAfterFeeAmount.Insert(rewardAfterFeeAmount.Length - config.GetValue<int>("ContractDecimals"), ".");

                var rewardAsDouble = double.Parse(rewardAfterFeeAmountWithDecimal);

                var consensusString = tran.ConsensusAt.Substring(0, tran.ConsensusAt.IndexOf("."));
                var consensusAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(consensusString)).DateTime;

                var reward = new StakePoolReward
                {
                    ConsensusDate = consensusAt,
                    IsPhase3 = true,
                    RewardAfterStaderFee = rewardAsDouble
                };

                rewards.Add(reward);
            }
        }

        return rewards.OrderBy(r => r.ConsensusDate).ToList();
    }

    public async Task<string> GetCurrentExchangeRate()
    {
        // Get contract TVL
        var response = await client.GetAsync("https://mainnet-public.mirrornode.hedera.com/api/v1/accounts/0.0.1412503");
        var responseJson = await response.Content.ReadAsStringAsync();

        var obj = JsonSerializer.Deserialize<ContractPayload>(responseJson);
        var decimals = int.Parse(config["ContractDecimals"]);
        var tvl = obj?.Balance?.Balance?.ToString("F0");

        var tvlWithDecimal = double.Parse(tvl.Insert(tvl.Length - decimals, "."));

        // Get HBARX token supply
        var tokenResponse = await client.GetAsync(" https://mainnet-public.mirrornode.hedera.com/api/v1/tokens/0.0.834116");
        var tokenResponseJson = await tokenResponse.Content.ReadAsStringAsync();

        var tokenObj = JsonSerializer.Deserialize<TokenPayload>(tokenResponseJson);

        var tokenDecimals = int.Parse(tokenObj.Decimals);
        var tokenSupply = tokenObj.TotalSupply;

        var tokenSupplyWithDecimal = double.Parse(tokenSupply.Insert(tokenSupply.Length - tokenDecimals, "."));

        return (tvlWithDecimal / tokenSupplyWithDecimal).ToString("N4");
    }

    public async Task<Price> GetHBARPrice()
    {
        var response = await client.GetAsync("https://api.coingecko.com/api/v3/simple/price?ids=hedera-hashgraph&vs_currencies=usd&include_24hr_change=true");

        var responseJson = await response.Content.ReadAsStringAsync();

        var obj = JsonSerializer.Deserialize<PricePayload>(responseJson);

        return obj.Payload;
    }
}