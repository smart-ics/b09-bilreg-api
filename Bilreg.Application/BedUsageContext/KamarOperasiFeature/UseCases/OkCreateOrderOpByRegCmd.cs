using Bilreg.Application.AdmisiContext.PetugasMedisFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature.UseCases
{
    public record OkCreateOrderOpByRegCmd(
        string RegId, string DiagCode, string JenisOperasiId, string NamaOperasi, 
        string DokterDpjpId, int EstimasiDurasiInMinutes, string PreferedDate,
        string SpecialEquipment, string UserId)
        : IRequest<OkCreateOrderOpByRegResponse>;

    public record OkCreateOrderOpByRegResponse(string OrderOpId);

    public class OkCreateOrderOpByRegHandler : IRequestHandler<OkCreateOrderOpByRegCmd, OkCreateOrderOpByRegResponse>
    {
        private readonly IOrderOpRepo _orderOpRepo;
        private readonly IRegRepo _regRepo;
        private readonly IIcd10Repo _icdRepo;
        private readonly IJenisOperasiRepo _jenisOperasiRepo;
        private readonly IPetugasMedisRepo _dokterRepo;

        public OkCreateOrderOpByRegHandler(IOrderOpRepo orderOpRepo,
            IRegRepo regRepo,
            IIcd10Repo icdRepo,
            IJenisOperasiRepo jenisOperasiRepo,
            IPetugasMedisRepo dokterRepo)
        {
            _orderOpRepo = orderOpRepo;
            _regRepo = regRepo;
            _icdRepo = icdRepo;
            _jenisOperasiRepo = jenisOperasiRepo;
            _dokterRepo = dokterRepo;
        }

        public Task<OkCreateOrderOpByRegResponse> Handle(OkCreateOrderOpByRegCmd request, CancellationToken cancellationToken)
        {
            var reg = _regRepo.LoadEntity(RegModel.Key(request.RegId))
                .GetValueOrThrow("Reg ID invalid");
            var icd = _icdRepo.LoadEntity(Icd10Type.Key(request.DiagCode))
                .GetValueOrThrow("Diagnosa Code invalid");
            var jenisOperasi = _jenisOperasiRepo.LoadEntity(JenisOperasiType.Key(request.JenisOperasiId))
                .GetValueOrThrow("Jenis Operasi ID invalid");
            var dokter = _dokterRepo.LoadEntity(PetugasMedisType.Key(request.DokterDpjpId))
                .GetValueOrThrow("Dokter DPJP ID invalid");

            var orderOp = OrderOpModel.CreateByReg(reg, request.UserId);
            orderOp.SetKlinis(icd, jenisOperasi,request.NamaOperasi);
            orderOp.OperationalRequest(dokter, request.EstimasiDurasiInMinutes,
                request.PreferedDate.ToDate(DateFormatEnum.YMD), request.SpecialEquipment);

            _orderOpRepo.SaveChanges(orderOp);

            return Task.FromResult(new OkCreateOrderOpByRegResponse(orderOp.OrderOpId));
        }
    }
}
