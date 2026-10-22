namespace Bilreg.Application.Shared;

public interface ICurrentUserContext
{
    string GetActorUserId();

    /// <summary>
    /// Peran yang dimiliki aktor saat ini. Kosong bila aktor tidak terautentikasi.
    /// </summary>
    IReadOnlyList<string> GetRoles();

    /// <summary>
    /// Alamat IP klien. String kosong bila tidak tersedia.
    /// </summary>
    string GetClientIpAddress();
}
