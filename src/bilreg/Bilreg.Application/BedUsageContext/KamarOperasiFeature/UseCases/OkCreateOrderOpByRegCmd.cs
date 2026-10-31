using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OrderOpResourceRequest(
    OrderOpResourceTypeEnum ResourceType,
    string ItemName,
    int Jumlah,
    string Satuan,
    string Keterangan = "");

public record OkCreateOrderOpByRegCmd(
    string RegId,
    string DokterPengorderId,
    string LayananPengorderId,
    string DokterOperatorId,
    string DiagCode,
    string NamaOperasi,
    string ProsedurTambahan,
    AnestesiPlanEnum AnestesiPlan,
    PostOpCareUnitEnum PostOpCareUnit,
    string? TarifId,
    int EstimasiDurasiInMinutes,
    DateTime PreferedDate,
    string SpecialEquipment,
    string UserId,
    bool IsForceCreate,
    KlasifikasiOperasiEnum KlasifikasiOperasi = KlasifikasiOperasiEnum.Kecil,
    UrgencyLevelEnum UrgencyLevel = UrgencyLevelEnum.Elective,
    IEnumerable<OrderOpResourceRequest>? ListResource = null)
    : IRequest<OkCreateOrderOpByRegResponse>;

public record OkCreateOrderOpByRegResponse(
    string OrderOpId,
    bool IsDuplicated,
    bool IsForceCreate,
    OkCreateOrderOpByRegResponseContent Existing,
    OkCreateOrderOpByRegResponseContent Requested);

public record OkCreateOrderOpByRegResponseContent(
    string RegId,
    string NamaOperasi,
    string PreferedDate);

public class OkCreateOrderOpByRegHandler 
    : IRequestHandler<OkCreateOrderOpByRegCmd, OkCreateOrderOpByRegResponse>
{
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IRegRepo _regRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly IPpaRepo _dokterRepo;
    private readonly IIcd10Repo _icdRepo;
    private readonly IOpCaseRepo _opCaseRepo;
    private readonly ITarifRepo _tarifRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public OkCreateOrderOpByRegHandler(
        IOrderOpRepo orderOpRepo,
        IRegRepo regRepo,
        ILayananRepo layananRepo,
        IPpaRepo dokterRepo,
        IIcd10Repo icdRepo,
        IOpCaseRepo opCaseRepo,
        ITarifRepo tarifRepo,
        ITglJamProvider tglJamProvider)
    {
        _orderOpRepo = orderOpRepo;
        _regRepo = regRepo;
        _layananRepo = layananRepo;
        _dokterRepo = dokterRepo;
        _icdRepo = icdRepo;
        _opCaseRepo = opCaseRepo;
        _tarifRepo = tarifRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<OkCreateOrderOpByRegResponse> Handle(
        OkCreateOrderOpByRegCmd request, 
        CancellationToken cancellationToken)
    {
        var occurredAt = _tglJamProvider.Now;

        //  GUARD
        Guard.Against.NullOrEmpty(request.RegId, nameof(request.RegId));
        Guard.Against.NullOrEmpty(request.DokterPengorderId, nameof(request.DokterPengorderId));
        Guard.Against.NullOrEmpty(request.LayananPengorderId, nameof(request.LayananPengorderId));
        Guard.Against.NullOrEmpty(request.DiagCode, nameof(request.DiagCode));
        Guard.Against.NullOrWhiteSpace(request.NamaOperasi, nameof(request.NamaOperasi));

        if (!request.IsForceCreate)
        {
            var existing = FindExistingOrder(request.RegId, occurredAt);
            if (existing is not null)
                return Task.FromResult(RespondDuplicated(request, existing));
        }

        //  BUILD
        var reg = LoadReg(request.RegId);
        var layanan = LoadLayanan(request.LayananPengorderId);
        var dokterPengorder = LoadDokter(request.DokterPengorderId, "Dokter Pengorder ID invalid");
        var icd = LoadIcd(request.DiagCode);

        PpaType? dokterOperator = null;
        if (!string.IsNullOrWhiteSpace(request.DokterOperatorId) && request.DokterOperatorId != "-")
        {
            dokterOperator = LoadDokter(request.DokterOperatorId, "Dokter Operator ID invalid");
        }

        TarifType? tarif = null;
        if (!string.IsNullOrWhiteSpace(request.TarifId) && request.TarifId != "-")
        {
            tarif = LoadTarif(request.TarifId);
        }

        var orderOp = CreateOrder(reg, layanan, dokterPengorder, icd, dokterOperator, tarif, request, occurredAt);
        var opCase = OpCaseModel.Create(orderOp, occurredAt);

        //  WRITE
        using var trans = TransHelper.NewScope();
        _orderOpRepo.SaveChanges(orderOp);
        _opCaseRepo.SaveChanges(opCase);
        trans.Complete();

        return Task.FromResult(RespondSuccess(orderOp));
    }

    private RegModel LoadReg(string id)
    {
        var reg = _regRepo.LoadEntity(RegModel.Key(id))
            .GetValueOrThrow("Reg ID invalid");
        if (!reg.IsAktif)
            throw new ArgumentException($"Registrasi '{id}' tidak aktif");
        return reg;
    }

    private LayananType LoadLayanan(string id) =>
        _layananRepo.LoadEntity(LayananType.Key(id))
            .GetValueOrThrow("Layanan Pengorder ID invalid");

    private PpaType LoadDokter(string id, string message = "Dokter ID invalid") =>
        _dokterRepo.LoadEntity(PpaType.Key(id))
            .GetValueOrThrow(message);

    private Icd10Type LoadIcd(string code) =>
        _icdRepo.LoadEntity(Icd10Type.Key(code))
            .GetValueOrThrow("Diagnosa Code invalid");

    private TarifType LoadTarif(string id) =>
        _tarifRepo.LoadEntity(TarifType.Key(id))
            .GetValueOrThrow("Tarif ID invalid");

    private OrderOpView? FindExistingOrder(string regId, DateTime occurredAt)
    {
        var list = _orderOpRepo.ListData(new Periode(occurredAt));
        return list.FirstOrDefault(o => o.RegId == regId);
    }

    //  DOMAIN BUILDER
    private static OrderOpModel CreateOrder(
        RegModel reg,
        LayananType layanan,
        PpaType dokterPengorder,
        Icd10Type icd,
        PpaType? dokterOperator,
        TarifType? tarif,
        OkCreateOrderOpByRegCmd req,
        DateTime occurredAt)
    {
        var orderOp = OrderOpModel.CreateByReg(reg, layanan, dokterPengorder, req.UserId, occurredAt);

        orderOp.SetKlinis(icd, JenisOperasiType.Default, req.NamaOperasi);
        orderOp.SetUrgencyLevel(req.UrgencyLevel);
        orderOp.SetProsedur(req.NamaOperasi, req.ProsedurTambahan, req.KlasifikasiOperasi);
        orderOp.SetRencanaAnestesiDanPerawatan(req.AnestesiPlan, req.PostOpCareUnit);

        var specialEquipment = string.IsNullOrWhiteSpace(req.SpecialEquipment) ? "-" : req.SpecialEquipment;

        if (dokterOperator != null)
        {
            orderOp.SetDokterOperator(dokterOperator);
            orderOp.OperationalRequest(
                dokterOperator,
                req.EstimasiDurasiInMinutes,
                req.PreferedDate,
                specialEquipment,
                occurredAt);
        }
        else
        {
            orderOp.OperationalRequest(
                req.EstimasiDurasiInMinutes,
                req.PreferedDate,
                specialEquipment,
                occurredAt);
        }

        if (tarif != null)
        {
            orderOp.SetTarif(tarif);
        }
        else
        {
            orderOp.SetTarif(TarifReff.Default);
        }

        if (req.ListResource != null)
        {
            foreach (var item in req.ListResource)
            {
                orderOp.AddResource(item.ResourceType, item.ItemName, item.Jumlah, item.Satuan, item.Keterangan);
            }
        }

        return orderOp;
    }

    //  RESPONSE BUILDER
    private static OkCreateOrderOpByRegResponse RespondDuplicated(
        OkCreateOrderOpByRegCmd req, 
        OrderOpView existing)
    {
        return new OkCreateOrderOpByRegResponse(
            OrderOpId: "-",
            IsDuplicated: true,
            IsForceCreate: false,
            Existing: new OkCreateOrderOpByRegResponseContent(
                existing.RegId,
                existing.NamaOperasi,
                existing.PreferedDate.ToString("yyyy-MM-dd")),
            Requested: new OkCreateOrderOpByRegResponseContent(
                req.RegId,
                req.NamaOperasi,
                req.PreferedDate.ToString("yyyy-MM-dd"))
        );
    }

    private static OkCreateOrderOpByRegResponse RespondSuccess(OrderOpModel orderOp)
    {
        return new OkCreateOrderOpByRegResponse(
            OrderOpId: orderOp.OrderOpId,
            IsDuplicated: false,
            IsForceCreate: true,
            Existing: new OkCreateOrderOpByRegResponseContent("-", "-", "3000-01-01"),
            Requested: new OkCreateOrderOpByRegResponseContent(
                orderOp.Reg.RegId,
                orderOp.NamaOperasi,
                orderOp.PreferedDate.ToString("yyyy-MM-dd"))
        );
    }
}
