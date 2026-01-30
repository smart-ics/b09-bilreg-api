namespace Bilreg.Domain.AdmisiContext.RemotCetakFeature;

public record RemoteCetakType
{
    public RemoteCetakType(string transaksiId, 
        string jenisDokumen, DateTime tanggalSend, 
        string remoteAddress, bool isCetak, 
        DateTime tanggalCetak, string jsonData, 
        string callbackData)
    {
        TransaksiId = transaksiId;
        JenisDokumen = jenisDokumen;
        TanggalSend = tanggalSend;
        RemoteAddress = remoteAddress;
        IsCetak = isCetak;
        TanggalCetak = tanggalCetak;
        JsonData = jsonData;
        CallbackData = callbackData;
    }

    public string TransaksiId { get; init; }
    public string JenisDokumen { get; init; }
    public DateTime TanggalSend { get; init; }
    public string RemoteAddress { get; init; }
    public bool IsCetak { get; init; }
    public DateTime TanggalCetak { get; init; }
    public string JsonData { get; init; }
    public string CallbackData { get; init; }
}

