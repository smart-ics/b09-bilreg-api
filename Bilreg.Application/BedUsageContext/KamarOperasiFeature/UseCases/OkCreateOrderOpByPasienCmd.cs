using Bilreg.Application.AdmisiContext.PetugasMedisFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkCreateOrderOpByPasienCmd(
    string PasienId, string DiagCode, string JenisOperasiId, string NamaOperasi,
    string DokterDpjpId, int EstimasiDurasiInMinutes, string PreferedDate, 
    string SpecialEquipment, string UserId) :
        IRequest<OkCreateOrderOpByPasienResponse>;

public record OkCreateOrderOpByPasienResponse(string OrderOpId);

public class OkCreateOrderOpByPasienHandler : IRequestHandler<OkCreateOrderOpByPasienCmd, OkCreateOrderOpByPasienResponse>
{
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IPasienRepo _pasienRepo;
    private readonly IIcd10Repo _icdRepo;
    private readonly IJenisOperasiRepo _jenisOperasiRepo;
    private readonly IPetugasMedisRepo _dokterRepo;

    public OkCreateOrderOpByPasienHandler(IOrderOpRepo orderOpRepo,
        IPasienRepo pasienRepo,
        IIcd10Repo icdRepo,
        IJenisOperasiRepo jenisOperasiRepo,
        IPetugasMedisRepo dokterRepo)
    {
        _orderOpRepo = orderOpRepo;
        _pasienRepo = pasienRepo;
        _icdRepo = icdRepo;
        _jenisOperasiRepo = jenisOperasiRepo;
        _dokterRepo = dokterRepo;
    }

    public Task<OkCreateOrderOpByPasienResponse> Handle(OkCreateOrderOpByPasienCmd request, CancellationToken cancellationToken)
    {
        var pasien = _pasienRepo.LoadEntity(PasienModel.Key(request.PasienId))
            .GetValueOrThrow("Pasien ID Invalid");
        var icd = _icdRepo.LoadEntity(Icd10Type.Key(request.DiagCode))
            .GetValueOrThrow("Diagnosa Code invalid");
        var jenisOperasi = _jenisOperasiRepo.LoadEntity(JenisOperasiType.Key(request.JenisOperasiId))
            .GetValueOrThrow("Jenis Operasi ID invalid");
        var dokter = _dokterRepo.LoadEntity(PetugasMedisType.Key(request.DokterDpjpId))
            .GetValueOrThrow("Dokter DPJP ID invalid");

        var orderOp = OrderOpModel.CreateByPasien(pasien, request.UserId);
        orderOp.SetKlinis(icd, jenisOperasi, request.NamaOperasi);
        orderOp.OperationalRequest(dokter, request.EstimasiDurasiInMinutes,
            request.PreferedDate.ToDate(DateFormatEnum.YMD), request.SpecialEquipment);

        _orderOpRepo.SaveChanges(orderOp);

        return Task.FromResult(new OkCreateOrderOpByPasienResponse(orderOp.OrderOpId));
    }
}
