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

