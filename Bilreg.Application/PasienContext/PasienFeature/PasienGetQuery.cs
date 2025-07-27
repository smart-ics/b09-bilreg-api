// using Bilreg.Application.Helpers;
// using Bilreg.Application.PasienContext.ParamContext.ParamSistemAgg;
// using Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;
// using Bilreg.Domain.PasienContext.PasienFeature;
// using Bilreg.Domain.PasienContext.StatusSosialSub.AgamaAgg;
// using Bilreg.Domain.PasienContext.StatusSosialSub.PekerjaanDkAgg;
// using Bilreg.Domain.PasienContext.StatusSosialSub.PendidikanDkAgg;
// using Bilreg.Domain.PasienContext.StatusSosialSub.StatusKawinDkAgg;
// using Bilreg.Domain.PasienContext.SukuFeature;
// using CommunityToolkit.Diagnostics;
// using MediatR;
// using Nuna.Lib.ValidationHelper;
//
// namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;
//
// public record PasienGetQuery(string PasienId): IRequest<PasienGetResponse>, IPasienKey;
//
// public record PasienGetResponse(
//     string PasienId,
//     string NomorMedrec,
//     string PasienName,
//     string TempatLahir,
//     string TglLahir,
//     string NickName,
//     string Gender,
//     string IbuKandung,
//     string GolDarah,
//     AddressType AKelurahanReffahanViewType Kelurahan,
//     IdentityType Identity,
//     ContactType Contact,
//     KeluargaType Keluarga,
//     StatusKawinDkModel StatusKawin,
//     AgamaModel Agama,
//     SukuType Suku,
//     PendidikanDkModel PendidikanDk,
//     PekerjaanDkModel PekerjaanDk
// );
//
// public class PasienGetHandler: IRequestHandler<PasienGetQuery, PasienGetResponse>
// {
//     private readonly IParamSistemDal _paramSistemDal;
//     private readonly IPasienDal _pasienDal;
//     private const string KODE_RS_PARAM_KEY = "RS__XXXXXX_KODE";
//
//     public PasienGetHandler(IParamSistemDal paramSistemDal, IPasienDal pasienDal)
//     {
//         _paramSistemDal = paramSistemDal;
//         _pasienDal = pasienDal;
//     }
//
//     public Task<PasienGetResponse> Handle(PasienGetQuery request, CancellationToken cancellationToken)
//     {
//         // GUARD
//         Guard.IsTrue(request.PasienId.IsValidA(x => x.Length is 6 or 8 or 15));
//         
//         // QUERY
//         var pasienGetQuery = GetPasienId(request.PasienId);
//         var pasien = _pasienDal
//             .GetData2(pasienGetQuery)
//             .OrThrowNotFoundException()
//             .Value;
//         
//         // RESPONSE
//         var response = BuildPasienResponse(pasien);
//         return Task.FromResult(response);
//     }
//
//     private PasienGetQuery GetPasienId(string pasienId)
//     {
//         var kodeRsEncrypted = _paramSistemDal.GetData(KODE_RS_PARAM_KEY)?.Value ?? string.Empty;
//         var kodeRs = X1EncryptionHelper.DecodingNeo(kodeRsEncrypted);
//
//         return pasienId.Length switch
//         {
//             6 => new PasienGetQuery($"{kodeRs}00{pasienId}"),
//             8 => new PasienGetQuery($"{kodeRs}{pasienId}"),
//             _ => new PasienGetQuery(pasienId)
//         };
//     }
//
//     private static PasienGetResponse BuildPasienResponse(PasienModel pasien)
//     {
//         
//         return new PasienGetResponse(
//             pasien.PasienId, 
//             pasien.GetNomorMedrec(),
//             pasien.PasienName,
//             pasien.TempatLahir,
//             pasien.TglLahir.ToString(DateFormatEnum.YMD),
//             pasien.NickName,
//             pasien.Gender.ToString(),
//             pasien.IbuKandung,
//             pasien.GolDarah.ToString(),
//             pasien.Alamat,
//             pasien.Kelurahan.ToViewType(),
//             pasien.ListIdentification,
//             pasien.ListContact,
//             pasien.PasienKeluarga,
//             pasien.StatusKawin,
//             pasien.Agama,
//             pasien.Suku,
//             pasien.Pendidikan,
//             pasien.PekerjaanDk
//         );
//     }
// }