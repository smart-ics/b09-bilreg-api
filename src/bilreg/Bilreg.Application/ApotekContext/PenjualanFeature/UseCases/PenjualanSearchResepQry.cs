using Ardalis.GuardClauses;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Application.ApotekContext.TelaahResepFeature;
using Bilreg.Application.SalesContext.ResepFeature;
using Bilreg.Domain.ApotekContext.PenjualanFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using MediatR;

namespace Bilreg.Application.ApotekContext.PenjualanFeature.UseCases;

#region Query DTOs

/// <summary>
/// Query pencarian universal e-resep aktif hari ini yang belum tertagih (TD-05).
/// Mendukung multi-parameter: No RM, No Registrasi, Nama Pasien, No Resep.
/// </summary>
public record PenjualanSearchResepQry(
    string Query,
    DateOnly? Date = null
) : IRequest<IReadOnlyList<PenjualanSearchResepResponse>>;

/// <summary>
/// Respons detail data resep e-resep beserta status telaah klinis terkini dan info antrean.
/// </summary>
public record PenjualanSearchResepResponse(
    string ResepId,
    DateTime TglJam,
    string RegId,
    string PasienId,
    string PasienName,
    string LayananId,
    string LayananName,
    string DokterId,
    string DokterName,
    string TipeBarangId,
    string TipeBarangName,
    string UrgenitasId,
    string UrgenitasName,
    bool IsCito,
    int Iter,
    string Description,
    decimal TinggiBadan,
    decimal BeratBadan,
    decimal Lpb,
    // Status Telaah Klinis
    string TelaahResepId,
    int? TelaahStatus,
    string TelaahStatusDesc,
    string PharmacistId,
    DateTime? TelaahStartedAt,
    DateTime? TelaahCompletedAt,
    bool CanEstablishSalesOrder,
    // Status Tiket Antrean (jika sudah dipetakan)
    string AntrianId,
    int NoUrut,
    bool PasienHadir,
    DateTime? PasienHadirAt,
    // Daftar Rincian Obat
    IReadOnlyList<PenjualanSearchResepItemResponse> Items
);

/// <summary>
/// Baris item obat pada resep beserta hasil kajian telaah.
/// </summary>
public record PenjualanSearchResepItemResponse(
    int NoUrut,
    string BrgId,
    string BrgName,
    string Satuan,
    decimal Qty,
    int Iter,
    string Signa,
    bool IsRacik,
    int? Disposition,
    string DispositionDesc,
    string AcceptedBrgId,
    string AcceptedBrgName,
    decimal AcceptedQty,
    string Reason
);

#endregion

#region Query Handler

/// <summary>
/// Handler untuk query pencarian universal e-resep aktif belum tertagih.
/// </summary>
public class PenjualanSearchResepHandler : IRequestHandler<PenjualanSearchResepQry, IReadOnlyList<PenjualanSearchResepResponse>>
{
    private readonly IResepRepo _resepRepo;
    private readonly IPenjualanRepo _penjualanRepo;
    private readonly ITelaahResepRepo _telaahRepo;
    private readonly IPharmacyQueueMappingRepo _mappingRepo;

    public PenjualanSearchResepHandler(
        IResepRepo resepRepo,
        IPenjualanRepo penjualanRepo,
        ITelaahResepRepo telaahRepo,
        IPharmacyQueueMappingRepo mappingRepo)
    {
        _resepRepo = resepRepo;
        _penjualanRepo = penjualanRepo;
        _telaahRepo = telaahRepo;
        _mappingRepo = mappingRepo;
    }

    public Task<IReadOnlyList<PenjualanSearchResepResponse>> Handle(PenjualanSearchResepQry request, CancellationToken cancellationToken)
    {
        var targetDate = request.Date ?? DateOnly.FromDateTime(DateTime.Today);
        var reseps = _resepRepo.SearchActiveUnbilled(request.Query ?? string.Empty, targetDate);

        var result = new List<PenjualanSearchResepResponse>();

        foreach (var resep in reseps)
        {
            if (resep.IsVoid)
                continue;

            // Pastikan belum tertagih pada tb_trs_dobill_umum
            var existingSales = _penjualanRepo.ListByResep(resep.ResepId)
                .Where(x => !x.IsVoid)
                .ToList();
            if (existingSales.Count > 0)
                continue;

            // Pemuatan status telaah klinis terkini
            var telaahOpt = _telaahRepo.LoadByResep(resep.ResepId);
            string telaahResepId = "";
            int? telaahStatus = null;
            string telaahStatusDesc = "Belum Ditelaah";
            string pharmacistId = "";
            DateTime? telaahStartedAt = null;
            DateTime? telaahCompletedAt = null;
            bool canEstablishSalesOrder = false;
            TelaahResepModel? telaahModel = null;

            if (telaahOpt.HasValue)
            {
                telaahModel = telaahOpt.Value;
                telaahResepId = telaahModel.TelaahResepId;
                telaahStatus = (int)telaahModel.TelaahStatus;
                pharmacistId = telaahModel.PharmacistId;
                telaahStartedAt = telaahModel.StartedAt != default && telaahModel.StartedAt != new DateTime(3000, 1, 1) ? telaahModel.StartedAt : null;
                telaahCompletedAt = telaahModel.CompletedAt != default && telaahModel.CompletedAt != new DateTime(3000, 1, 1) ? telaahModel.CompletedAt : null;
                canEstablishSalesOrder = telaahModel.CanEstablishSalesOrder;

                telaahStatusDesc = telaahModel.TelaahStatus switch
                {
                    TelaahStatusEnum.Available => "Tersedia / Menunggu Telaah",
                    TelaahStatusEnum.UnderReview => "Sedang Ditelaah",
                    TelaahStatusEnum.Approved => "Disetujui",
                    TelaahStatusEnum.PartiallyApproved => "Disetujui Sebagian",
                    TelaahStatusEnum.Rejected => "Ditolak",
                    _ => telaahModel.TelaahStatus.ToString()
                };
            }

            // Pemuatan info kaitan antrean fisik
            var mappingOpt = _mappingRepo.GetByDemand(QueueDemandKindEnum.Resep, resep.ResepId);
            if (!mappingOpt.HasValue)
                mappingOpt = _mappingRepo.GetByDemand(QueueDemandKindEnum.ResepKerja, resep.ResepId);

            string antrianId = "";
            int noUrut = 0;
            bool pasienHadir = false;
            DateTime? pasienHadirAt = null;

            if (mappingOpt.HasValue)
            {
                var mapping = mappingOpt.Value;
                antrianId = mapping.AntrianId;
                noUrut = mapping.NoUrut;
                pasienHadir = mapping.PasienHadir;
                pasienHadirAt = mapping.PasienHadirAt != default && mapping.PasienHadirAt != new DateTime(3000, 1, 1)
                    ? mapping.PasienHadirAt
                    : null;
            }

            // Evaluasi CITO
            bool isCito = false;
            if (resep.Urgenitas is not null)
            {
                isCito = resep.Urgenitas.UrgenitasName.Contains("CITO", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(resep.Urgenitas.UrgenitasId, "CITO", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(resep.Urgenitas.UrgenitasId, "1", StringComparison.OrdinalIgnoreCase);
            }

            // Item rincian obat dikorelasikan dengan telaah
            var items = new List<PenjualanSearchResepItemResponse>();
            foreach (var obat in resep.ListObat)
            {
                int? disposition = null;
                string dispositionDesc = "";
                string acceptedBrgId = obat.Brg.BrgId;
                string acceptedBrgName = obat.Brg.BrgName;
                decimal acceptedQty = obat.Qty;
                string reason = "";

                if (telaahModel != null)
                {
                    var telaahItem = telaahModel.Items.FirstOrDefault(x => x.ItemNo == obat.NoUrut);
                    if (telaahItem != null)
                    {
                        disposition = (int)telaahItem.Disposition;
                        dispositionDesc = telaahItem.Disposition.ToString();
                        acceptedBrgId = telaahItem.AcceptedBrgId;
                        acceptedBrgName = telaahItem.AcceptedBrgName;
                        acceptedQty = telaahItem.AcceptedQty;
                        reason = telaahItem.Reason;
                    }
                }

                items.Add(new PenjualanSearchResepItemResponse(
                    NoUrut: obat.NoUrut,
                    BrgId: obat.Brg.BrgId,
                    BrgName: obat.Brg.BrgName,
                    Satuan: obat.Satuan?.SatuanName ?? "",
                    Qty: obat.Qty,
                    Iter: obat.Iter,
                    Signa: obat.Etiket?.Signa ?? "",
                    IsRacik: obat.IsRacik,
                    Disposition: disposition,
                    DispositionDesc: dispositionDesc,
                    AcceptedBrgId: acceptedBrgId,
                    AcceptedBrgName: acceptedBrgName,
                    AcceptedQty: acceptedQty,
                    Reason: reason
                ));
            }

            result.Add(new PenjualanSearchResepResponse(
                ResepId: resep.ResepId,
                TglJam: resep.AuditTrail.Created.Timestamp,
                RegId: resep.Register.RegId,
                PasienId: resep.Register.PasienId,
                PasienName: resep.Register.PasienName,
                LayananId: resep.Layanan.LayananId,
                LayananName: resep.Layanan.LayananName,
                DokterId: resep.Dokter.DokterId,
                DokterName: resep.Dokter.DokterName,
                TipeBarangId: resep.TipeBrg.TipeBrgId,
                TipeBarangName: resep.TipeBrg.TipeBrgName,
                UrgenitasId: resep.Urgenitas?.UrgenitasId ?? "",
                UrgenitasName: resep.Urgenitas?.UrgenitasName ?? "",
                IsCito: isCito,
                Iter: resep.Iter,
                Description: resep.Description ?? "",
                TinggiBadan: resep.BodyMetric?.BodyHeight ?? 0m,
                BeratBadan: resep.BodyMetric?.BodyWeight ?? 0m,
                Lpb: resep.BodyMetric?.LingkarPinggang ?? 0m,
                TelaahResepId: telaahResepId,
                TelaahStatus: telaahStatus,
                TelaahStatusDesc: telaahStatusDesc,
                PharmacistId: pharmacistId,
                TelaahStartedAt: telaahStartedAt,
                TelaahCompletedAt: telaahCompletedAt,
                CanEstablishSalesOrder: canEstablishSalesOrder,
                AntrianId: antrianId,
                NoUrut: noUrut,
                PasienHadir: pasienHadir,
                PasienHadirAt: pasienHadirAt,
                Items: items
            ));
        }

        return Task.FromResult<IReadOnlyList<PenjualanSearchResepResponse>>(result);
    }
}

#endregion
