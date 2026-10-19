namespace Bilreg.Application.ChargeContext.RoomChargeFeature;

public class BillingEpisodeFinalException : InvalidOperationException
{
    public string RegId { get; }

    public BillingEpisodeFinalException(string regId)
        : base($"Episode penagihan untuk registrasi '{regId}' telah berstatus Final / Lunas dan terkunci dari perubahan.")
    {
        RegId = regId;
    }

    public BillingEpisodeFinalException(string regId, string message)
        : base(message)
    {
        RegId = regId;
    }
}

public interface IBillingLockChecker
{
    void AssertBillingNotFinal(string regId);
}
