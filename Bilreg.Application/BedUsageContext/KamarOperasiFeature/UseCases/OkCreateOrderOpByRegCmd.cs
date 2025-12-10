using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkCreateOrderOpByRegCmd(
    string RegId, string DiagCode, string JenisOperasiId, string NamaOperasi, 
    string DokterDpjpId, int EstimasiDurasiInMinutes, string PreferedDate,
    string SpecialEquipment, string UserId, bool IsForceCreate)
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
    private readonly IIcd10Repo _icdRepo;
    private readonly IJenisOperasiRepo _jenisOperasiRepo;
    private readonly IPpaRepo _dokterRepo;
    private readonly IOpCaseRepo _opCaseRepo;

    public OkCreateOrderOpByRegHandler(
        IOrderOpRepo orderOpRepo,
        IRegRepo regRepo,
        IIcd10Repo icdRepo,
        IJenisOperasiRepo jenisOperasiRepo,
        IPpaRepo dokterRepo,
        IOpCaseRepo opCaseRepo)
    {
        _orderOpRepo = orderOpRepo;
        _regRepo = regRepo;
        _icdRepo = icdRepo;
        _jenisOperasiRepo = jenisOperasiRepo;
        _dokterRepo = dokterRepo;
        _opCaseRepo = opCaseRepo;
    }

    public Task<OkCreateOrderOpByRegResponse> Handle(
        OkCreateOrderOpByRegCmd request, 
        CancellationToken cancellationToken)
    {
        //  GUARD
        if (!request.IsForceCreate)
        {
            var existing = FindExistingOrder(request.RegId);
            if (existing is not null)
                return Task.FromResult(RespondDuplicated(request, existing));
        }

        //  BUILD
        var reg = LoadReg(request.RegId);
        var icd = LoadIcd(request.DiagCode);
        var jenisOp = LoadJenisOperasi(request.JenisOperasiId);
        var dokter = LoadDokter(request.DokterDpjpId);
        var orderOp = CreateOrder(reg, icd, jenisOp, dokter, request);
        var opCase = OpCaseModel.Create(orderOp);

        //  WRITE
        using var trans = TransHelper.NewScope();
        _orderOpRepo.SaveChanges(orderOp);
        _opCaseRepo.SaveChanges(opCase);
        trans.Complete();
        return Task.FromResult(RespondSuccess(orderOp));
    }

    private RegModel LoadReg(string id) =>
        _regRepo.LoadEntity(RegModel.Key(id))
            .GetValueOrThrow("Reg ID invalid");

    private Icd10Type LoadIcd(string code) =>
        _icdRepo.LoadEntity(Icd10Type.Key(code))
            .GetValueOrThrow("Diagnosa Code invalid");

    private JenisOperasiType LoadJenisOperasi(string id) =>
        _jenisOperasiRepo.LoadEntity(JenisOperasiType.Key(id))
            .GetValueOrThrow("Jenis Operasi ID invalid");

    private PpaType LoadDokter(string id) =>
        _dokterRepo.LoadEntity(PpaType.Key(id))
            .GetValueOrThrow("Dokter DPJP ID invalid");

    private OrderOpView? FindExistingOrder(string regId)
    {
        var list = _orderOpRepo.ListData(new Periode(DateTime.Now));
        return list.FirstOrDefault(o => o.RegId == regId);
    }

    //  DOMAIN BUILDER
    private static OrderOpModel CreateOrder(
        RegModel reg,
        Icd10Type icd,
        JenisOperasiType jenisOp,
        PpaType dokter,
        OkCreateOrderOpByRegCmd req)
    {
        var orderOp = OrderOpModel.CreateByReg(reg, req.UserId);

        orderOp.SetKlinis(icd, jenisOp, req.NamaOperasi);
        orderOp.OperationalRequest(
            dokter,
            req.EstimasiDurasiInMinutes,
            req.PreferedDate.ToDate(DateFormatEnum.YMD),
            req.SpecialEquipment);

        return orderOp;
    }

    //  RESPONSE BUILDER
    private OkCreateOrderOpByRegResponse RespondDuplicated(
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
                req.PreferedDate)
        );
    }

    private OkCreateOrderOpByRegResponse RespondSuccess(OrderOpModel orderOp)
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
