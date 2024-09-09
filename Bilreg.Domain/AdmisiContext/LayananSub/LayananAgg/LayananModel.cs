using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.LayananDkAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.TipeLayananDkAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SmfAgg;
using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;

public class LayananModel(
        string LayananId,
        string LayananName) : ILayananKey
{
    public string LayananId { get; protected set; } = LayananId;
    public string LayananName { get; protected set; } = LayananName;
    public bool IsAktif { get; protected set; } = true;
    public string InstalasiId { get; protected set; } = string.Empty;
    public string InstalasiName { get; protected set; } = string.Empty;
    public string InstalasiDkId { get; protected set; } = string.Empty;
    public string InstalasiDkName { get; protected set; } = string.Empty;
    public string LayananDkId { get; protected set; } = string.Empty;
    public string LayananDkName { get; protected set; } = string.Empty;
    public string LayananTipeDkId { get; protected set; } = string.Empty;
    public string LayananTipeDkName { get; protected set; } = string.Empty;
    public string SmfId { get; protected set; } = string.Empty;
    public string SmfName { get; protected set; } = string.Empty;
    public string PetugasMedisId { get; protected set; } = string.Empty;
    public string PetugasMedisName { get; protected set; } = string.Empty; 

    // METHOD
    public void SetAktif() => IsAktif = true;
    public void UnSetAktif() => IsAktif = false;

    public void SetInstalasi(string instalasi)
    {
        Guard.IsNotWhiteSpace(instalasi);
        InstalasiId = instalasi;
    }
    public void SetInstalasiDk(InstalasiDkModel model)
    {
        InstalasiDkId = model.InstalasiDkId;
    }
    public void SetLayananDk(LayananDkModel model){
        LayananDkId = model.LayananDkId;
    }
    public void SetSmf(SmfModel model){
        SmfId = model.SmfId;
    }
    public void SetPetugasMedis(PetugasMedisModel model) { 
        PetugasMedisId = model.PetugasMedisId;
    }
    public void SetTipeLayananDk(TipeLayananDkModel model) {
        LayananTipeDkId = model.TipeLayananDkId;
    }
}
    

public interface ILayananKey
{
    string LayananId { get; }
}