using Bilreg.Domain.BrgContext.KlasifikasiFeature;
using Bilreg.Domain.BrgContext.StandardFeature;

namespace Bilreg.Domain.BrgContext.BrgFeature;

public interface IFarmakologi
{
    GenerikReff Generik { get; }
    KelasTerapiType KelasTerapi { get; }
    GolTerapiType GolTerapi { get; }
    OriginalType Original { get; }
}