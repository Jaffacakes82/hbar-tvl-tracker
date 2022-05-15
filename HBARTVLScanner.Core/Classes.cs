using System.Text.Json.Serialization;

namespace HBARTVLScanner.Core;

public class ContractPayload
{
    [JsonPropertyName("balance")]
    public ContractBalance? Balance { get; set; }
}

public class ContractBalance
{
    [JsonPropertyName("balance")]
    public double? Balance { get; set; }
}

public class TokenPayload
{
    [JsonPropertyName("total_supply")]
    public string? TotalSupply { get; set; }

    [JsonPropertyName("decimals")]
    public string? Decimals { get; set; }
}


public class TransactionsPayload
{
    [JsonPropertyName("transactions")]
    public IList<Transaction>? Transactions { get; set; }
}

public class Transaction
{
    [JsonPropertyName("transaction_id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Type { get; set; }

    [JsonPropertyName("consensus_timestamp")]
    public string ConsensusAt { get; set; }

    [JsonPropertyName("transfers")]
    public IList<TransactionTransfer> Transfers { get; set; }

}

public class TransactionTransfer
{
    [JsonPropertyName("account")]
    public string AccountId { get; set; }
    [JsonPropertyName("amount")]
    public double Amount { get; set; }
}