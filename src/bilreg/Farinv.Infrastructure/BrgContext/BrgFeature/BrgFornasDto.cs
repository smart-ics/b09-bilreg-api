using Farinv.Domain.BrgContext.StandardFeature;

namespace Farinv.Infrastructure.BrgContext.BrgFeature;

public record BrgFornasDto(
    string fs_kd_barang,
    string FornasId
)
{
    public static BrgFornasDto FromModel(string brgId, FornasReff fornas)
    {
        return new BrgFornasDto(
            fs_kd_barang: brgId,
            FornasId: fornas.FornasId
        );
    }

    public FornasReff ToModel()
    {
        // FornasReff perlu nama, tapi tabel hanya menyimpan ID.
        return new FornasReff(FornasId, "-");
    }
}