using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Farinv.Domain.BrgContext.StandardFeature;

namespace Farinv.Domain.BrgContext.BrgFeature;

public interface IFarmakologi
{
    GenerikReff Generik { get; }
    KelasTerapiType KelasTerapi { get; }
    GolTerapiType GolTerapi { get; }
    OriginalType Original { get; }
}