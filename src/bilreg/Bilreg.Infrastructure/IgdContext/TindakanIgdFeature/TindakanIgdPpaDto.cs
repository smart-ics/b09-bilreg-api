using Bilreg.Domain.IgdContext.TindakanIgdFeature;

namespace Bilreg.Infrastructure.IgdContext.TindakanIgdFeature;

public record TindakanIgdPpaDto(
    string TindakanIgdId,
    string KomponenId,
    string PpaId,
    string PpaName)
{
    public TindakanIgdPpaDto() : this("", "", "", "") { }

    public static TindakanIgdPpaDto FromModel(TindakanIgdPpaType model, string tindakanIgdId)
        => new(
            TindakanIgdId: tindakanIgdId,
            KomponenId: model.KomponenId,
            PpaId: model.PpaId,
            PpaName: model.PpaName);

    public TindakanIgdPpaType ToModel()
        => new(
            KomponenId: KomponenId,
            PpaId: PpaId,
            PpaName: PpaName);
}
