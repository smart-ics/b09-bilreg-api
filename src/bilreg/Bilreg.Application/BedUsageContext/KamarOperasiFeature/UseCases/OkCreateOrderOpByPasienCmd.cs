using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkCreateOrderOpByPasienCmd(
    string PasienId, string DiagCode, string JenisOperasiId, string NamaOperasi,
    string DokterDpjpId, string TarifId, int EstimasiDurasiInMinutes,
    string PreferedDate, string SpecialEquipment, string UserId, bool IsForceCreate) :
        IRequest<OkCreateOrderOpByPasienResponse>;

public record OkCreateOrderOpByPasienResponse(
    string OrderOpId,
    bool IsDuplicated,
    bool IsForceCreate,
    OkCreateOrderOpByPasienResponseContent Existing,
    OkCreateOrderOpByPasienResponseContent Requested);

public record OkCreateOrderOpByPasienResponseContent(
    string PasienId,
    string NamaOperasi,
    string PreferedDate);

public class OkCreateOrderOpByPasienHandler :
    IRequestHandler<OkCreateOrderOpByPasienCmd, OkCreateOrderOpByPasienResponse>
{
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IPasienRepo _pasienRepo;
    private readonly IIcd10Repo _icdRepo;
    private readonly IJenisOperasiRepo _jenisOperasiRepo;
    private readonly IPpaRepo _dokterRepo;
    private readonly IOpCaseRepo _opCaseRepo;
    private readonly ITarifRepo _tarifRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public OkCreateOrderOpByPasienHandler(IOrderOpRepo orderOpRepo,
        IPasienRepo pasienRepo,
        IIcd10Repo icdRepo,
        IJenisOperasiRepo jenisOperasiRepo,
        IPpaRepo dokterRepo, IOpCaseRepo opCaseRepo, ITarifRepo tarifRepo,
        ITglJamProvider? tglJamProvider = null)
    {
        _orderOpRepo = orderOpRepo;
        _pasienRepo = pasienRepo;
        _icdRepo = icdRepo;
        _jenisOperasiRepo = jenisOperasiRepo;
        _dokterRepo = dokterRepo;
        _opCaseRepo = opCaseRepo;
        _tarifRepo = tarifRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<OkCreateOrderOpByPasienResponse> Handle(
        OkCreateOrderOpByPasienCmd request, CancellationToken cancellationToken)
    {
        var occurredAt = _tglJamProvider.Now;
        //  GUARD
        Guard.Against.NullOrEmpty(request.PasienId);
        Guard.Against.NullOrEmpty(request.DiagCode);
        Guard.Against.NullOrEmpty(request.JenisOperasiId);
        Guard.Against.NullOrEmpty(request.DokterDpjpId);
        Guard.Against.NullOrEmpty(request.TarifId);

        if (!request.IsForceCreate)
        {
            var existing = FindExistingOrder(request.PasienId, occurredAt);
            if (existing is not null)
                return Task.FromResult(RespondDuplicated(request, existing));
        }

        //  BUILD
        var pasien = LoadPasien(request.PasienId);
        var icd = LoadIcd(request.DiagCode);
        var jenisOperasi = LoadJenisOperasi(request.JenisOperasiId);
        var dokter = LoadDokter(request.DokterDpjpId);
        var tarif = LoadTarif(request.TarifId);
        var orderOp = CreateOrder(pasien, icd, jenisOperasi, dokter, tarif, request, occurredAt);
        var opCase = OpCaseModel.Create(orderOp, occurredAt);

        //  WRITE
        using var trans = TransHelper.NewScope();
        _orderOpRepo.SaveChanges(orderOp);
        _opCaseRepo.SaveChanges(opCase);
        trans.Complete();

        return Task.FromResult(RespondSuccess(orderOp));
    }

    private PasienModel LoadPasien(string id) =>
        _pasienRepo.LoadEntity(PasienModel.Key(id))
            .GetValueOrThrow("Pasien ID Invalid");

    private Icd10Type LoadIcd(string code) =>
        _icdRepo.LoadEntity(Icd10Type.Key(code))
            .GetValueOrThrow("Diagnosa Code invalid");

    private JenisOperasiType LoadJenisOperasi(string id) =>
        _jenisOperasiRepo.LoadEntity(JenisOperasiType.Key(id))
            .GetValueOrThrow("Jenis Operasi ID invalid");

    private PpaType LoadDokter(string id) =>
        _dokterRepo.LoadEntity(PpaType.Key(id))
            .GetValueOrThrow("Dokter DPJP ID invalid");

    private TarifType LoadTarif(string id) => 
        _tarifRepo.LoadEntity(TarifType.Key(id))
            .GetValueOrThrow("Tarif ID invalid");

    private OrderOpView? FindExistingOrder(string pasienId, DateTime occurredAt)
    {
        var list = _orderOpRepo.ListData(new Periode(occurredAt));
        return list.FirstOrDefault(x => x.PasienId == pasienId);
    }

    //  DOMAIN BUILDER
    private static OrderOpModel CreateOrder(
        PasienModel pasien,
        Icd10Type icd,
        JenisOperasiType jenisOp,
        PpaType dokter,
        TarifType tarif,
        OkCreateOrderOpByPasienCmd req,
        DateTime occurredAt)
    {
        var orderOp = OrderOpModel.CreateByPasien(pasien, req.UserId, occurredAt);

        orderOp.SetKlinis(icd, jenisOp, req.NamaOperasi);
        orderOp.OperationalRequest(
            dokter,
            req.EstimasiDurasiInMinutes,
            req.PreferedDate.ToDate(DateFormatEnum.YMD),
            req.SpecialEquipment,
            occurredAt);
        orderOp.SetTarif(tarif);

        return orderOp;
    }

    //  RESPONSE BUILDER
    private OkCreateOrderOpByPasienResponse RespondDuplicated(
        OkCreateOrderOpByPasienCmd req,
        OrderOpView existing)
    {
        return new OkCreateOrderOpByPasienResponse(
            OrderOpId: "-",
            IsDuplicated: true,
            IsForceCreate: false,
            Existing: new OkCreateOrderOpByPasienResponseContent(
                existing.PasienId,
                existing.NamaOperasi,
                existing.PreferedDate.ToString("yyyy-MM-dd")),
            Requested: new OkCreateOrderOpByPasienResponseContent(
                req.PasienId,
                req.NamaOperasi,
                req.PreferedDate)
        );
    }

    private OkCreateOrderOpByPasienResponse RespondSuccess(OrderOpModel orderOp)
    {
        return new OkCreateOrderOpByPasienResponse(
            OrderOpId: orderOp.OrderOpId,
            IsDuplicated: false,
            IsForceCreate: true,
            Existing: new OkCreateOrderOpByPasienResponseContent("-", "-", "3000-01-01"),
            Requested: new OkCreateOrderOpByPasienResponseContent(
                orderOp.Pasien.PasienId,
                orderOp.NamaOperasi,
                orderOp.PreferedDate.ToString("yyyy-MM-dd"))
        );
    }
}
