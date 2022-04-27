namespace HBARTVLScanner.Web.Data;

public class StakePoolReward
{
    public DateTime ConsensusDate { get; set; }

    public double RewardAfterStaderFee { get; set; }

    public double TVLAtReward { get; set; }

    public bool IsPhase3 { get; set; }
}

