using System.Text.Json.Serialization;

namespace HBARTVLScanner.Core;

public class ContractPayload
{
    [JsonPropertyName("data")]
    public ContractPayloadInner? Data { get; set; }
}

public class ContractPayloadInner
{
    [JsonPropertyName("contract")]
    public ContractData? Contract { get; set; }
}

public class ContractData
{
    [JsonPropertyName("balance")]
    public string? Balance { get; set; }
}

public class TokenPayload
{
    [JsonPropertyName("data")]
    public TokenPayloadInner? Data { get; set; }
}

public class TokenPayloadInner
{
    [JsonPropertyName("token")]
    public TokenData? Token { get; set; }
}

public class TokenData
{
    [JsonPropertyName("decimals")]
    public int Decimals { get; set; }

    [JsonPropertyName("totalSupply")]
    public string? TotalSupply { get; set; }
}

public class TransactionsPayload
{
    [JsonPropertyName("data")]
    public IList<Transaction>? Data { get; set; }
}

public class Transaction
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("consensusAt")]
    public DateTime ConsensusAt { get; set; }

    [JsonPropertyName("transfers")]
    public IList<TransactionTransfer> Transfers { get; set; }

}

public class TransactionTransfer
{
    [JsonPropertyName("accountId")]
    public string AccountId { get; set; }
    [JsonPropertyName("amount")]
    public string Amount { get; set; }
    [JsonPropertyName("balance")]
    public string Balance { get; set; }
}