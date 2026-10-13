using Ardalis.GuardClauses;
using Bilreg.Application.ApotekContext.ResepKerjaFeature;
using Bilreg.Application.SalesContext.ResepFeature;
using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.ApotekContext.ResepKerjaFeature;

public class PrescriptionContractAdapter : IPrescriptionContractPort
{
    private readonly IResepRepo _resepRepo;

    public PrescriptionContractAdapter(IResepRepo resepRepo)
    {
        _resepRepo = resepRepo ?? throw new ArgumentNullException(nameof(resepRepo));
    }

    public PrescriptionContract Load(ResepKerjaSourceKindEnum sourceKind, string sourceResepId)
    {
        Guard.Against.NullOrWhiteSpace(sourceResepId, nameof(sourceResepId));

        if (sourceKind == ResepKerjaSourceKindEnum.Physical)
            throw new InvalidOperationException("Physical source kind is not supported by electronic prescription adapter.");

        var resepOpt = _resepRepo.LoadEntity(ResepModel.Key(sourceResepId));
        if (!resepOpt.HasValue)
            throw new KeyNotFoundException($"Resep with ID '{sourceResepId}' not found.");

        var resep = resepOpt.Value;
        if (resep.IsVoid)
            throw new ApotekDomainException($"Resep with ID '{sourceResepId}' has been voided.");

        var items = new List<PrescriptionContractItem>();
        var components = new List<PrescriptionContractComponent>();
        var itemIndex = 1;

        foreach (var obat in resep.ListObat)
        {
            var sourceItemNo = obat.NoUrut > 0 ? obat.NoUrut : itemIndex;
            var brgName = string.IsNullOrWhiteSpace(obat.Brg.BrgName) ? obat.Brg.BrgId : obat.Brg.BrgName.Trim();
            var satuanId = obat.Satuan?.SatuanId ?? string.Empty;
            var satuanName = string.IsNullOrWhiteSpace(obat.Satuan?.SatuanName) ? satuanId : obat.Satuan.SatuanName.Trim();
            var signa = obat.Etiket?.Signa ?? string.Empty;
            var instruction = obat.Etiket?.Instruction ?? string.Empty;
            var note = obat.Etiket?.Note ?? string.Empty;

            items.Add(new PrescriptionContractItem(
                sourceItemNo,
                obat.Brg.BrgId,
                brgName,
                satuanId,
                satuanName,
                obat.Qty,
                obat.Iter,
                signa,
                instruction,
                note,
                obat.IsRacik));

            if (obat.IsRacik || obat.ListItemRacik.Any())
            {
                var compIndex = 1;
                foreach (var racik in obat.ListItemRacik)
                {
                    var compNo = racik.NoUrut > 0 ? racik.NoUrut : compIndex;
                    var compBrgName = string.IsNullOrWhiteSpace(racik.Brg.BrgName) ? racik.Brg.BrgId : racik.Brg.BrgName.Trim();
                    var compSatuanId = racik.Satuan?.SatuanId ?? string.Empty;

                    components.Add(new PrescriptionContractComponent(
                        sourceItemNo,
                        compNo,
                        racik.Brg.BrgId,
                        compBrgName,
                        compSatuanId,
                        racik.Qty));

                    compIndex++;
                }
            }

            itemIndex++;
        }

        var regId = resep.Register?.RegId ?? string.Empty;
        var pasienId = resep.Register?.PasienId ?? string.Empty;
        var pasienName = resep.Register?.PasienName ?? string.Empty;
        var dokterId = resep.Dokter?.DokterId ?? string.Empty;
        var dokterName = resep.Dokter?.DokterName ?? string.Empty;
        var layananId = !string.IsNullOrWhiteSpace(resep.Layanan?.LayananId) && resep.Layanan.LayananId != AppConst.DASH
            ? resep.Layanan.LayananId
            : (!string.IsNullOrWhiteSpace(resep.LayananStok?.LayananId) && resep.LayananStok.LayananId != AppConst.DASH
                ? resep.LayananStok.LayananId
                : string.Empty);
        var urgenitas = ParseUrgenitas(resep.Urgenitas);
        var iterEntitled = resep.Iter;

        return new PrescriptionContract(
            sourceKind,
            resep.ResepId,
            regId,
            pasienId,
            pasienName,
            dokterId,
            dokterName,
            layananId,
            urgenitas,
            iterEntitled,
            items,
            components);
    }

    private static int ParseUrgenitas(UrgenitasType? urgenitas)
    {
        if (urgenitas is null)
            return 0;

        if (int.TryParse(urgenitas.UrgenitasId, out var parsed))
            return parsed;

        if (urgenitas.UrgenitasId.Equals("U", StringComparison.OrdinalIgnoreCase) ||
            urgenitas.UrgenitasName.Contains("CITO", StringComparison.OrdinalIgnoreCase) ||
            urgenitas.UrgenitasName.Contains("URGENT", StringComparison.OrdinalIgnoreCase))
            return 1;

        return 0;
    }
}
