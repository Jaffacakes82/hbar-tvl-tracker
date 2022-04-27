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
            allTransactions.AddRange(transactionPayload.Data);
        }

        var distinctTransactions = allTransactions
            .Where(t => t.Type == "CONTRACT_CALL")
            .GroupBy(t => t.Id)
            .Select(t => t.First())
            .ToList();

        var rewards = new List<StakePoolReward>();

        foreach (var tran in distinctTransactions)
        {
            var rewardAfterFeeAmount = tran.Transfers.Where(tran => tran.AccountId == "0.0.834119").First().Amount;
            var tvlAtReward = tran.Transfers.Where(tran => tran.AccountId == "0.0.834119").First().Balance;

            var rewardAfterFeeAmountWithDecimal = rewardAfterFeeAmount.Insert(rewardAfterFeeAmount.Length - config.GetValue<int>("ContractDecimals"), ".");
            var tvlAtRewardWithDecimal = tvlAtReward.Insert(tvlAtReward.Length - config.GetValue<int>("ContractDecimals"), ".");

            var rewardAsDouble = double.Parse(rewardAfterFeeAmountWithDecimal);
            var tvlAsDouble = double.Parse(tvlAtRewardWithDecimal);

            var reward = new StakePoolReward
            {
                ConsensusDate = tran.ConsensusAt,
                IsPhase3 = tvlAsDouble > 400000000,
                RewardAfterStaderFee = rewardAsDouble,
                TVLAtReward = tvlAsDouble
            };

            rewards.Add(reward);
        }

        return rewards.OrderBy(r => r.ConsensusDate).ToList();
    }

    public async Task<IList<StakePoolRewardWithFee>> GetRewardsWithStaderFee()
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
            allTransactions.AddRange(transactionPayload.Data);
        }

        var distinctTransactions = allTransactions
            .Where(t => t.Type == "CONTRACT_CALL")
            .GroupBy(t => t.Id)
            .Select(t => t.First())
            .ToList();

        var rewards = new List<StakePoolRewardWithFee>();

        foreach (var tran in distinctTransactions)
        {
            var rewardWithFee = tran.Transfers.Where(tran => tran.AccountId == "0.0.834120").First().Amount;
            var tvlAtReward = tran.Transfers.Where(tran => tran.AccountId == "0.0.834119").First().Balance;

            var rewardAfterFeeAmountWithDecimal = rewardWithFee.Insert(rewardWithFee.Length - config.GetValue<int>("ContractDecimals"), ".");
            var tvlAtRewardWithDecimal = tvlAtReward.Insert(tvlAtReward.Length - config.GetValue<int>("ContractDecimals"), ".");

            var rewardAsDouble = double.Parse(rewardAfterFeeAmountWithDecimal);
            var tvlAsDouble = double.Parse(tvlAtRewardWithDecimal);

            var reward = new StakePoolRewardWithFee
            {
                ConsensusDate = tran.ConsensusAt,
                IsPhase3 = tvlAsDouble > 400000000,
                TotalReward = Math.Abs(rewardAsDouble)
            };

            rewards.Add(reward);
        }

        return rewards.OrderBy(r => r.ConsensusDate).ToList();
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