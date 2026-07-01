using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.Shared.Helpers;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.DataTypeExtension;
using System.Text.RegularExpressions;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienSearchQuery(string Keyword) :IRequest<IEnumerable<PasienSearchResponse>> ;
public record PasienSearchResponse(string PasienId, bool IsActive, PersonInfoType Person);
public class PasienSearchHandler : IRequestHandler<PasienSearchQuery, IEnumerable<PasienSearchResponse>>
{
    private readonly IPasienRepo _pasienRepo;
    private readonly IPolisRepo _polisRepo;
    private readonly IRegRepo _regRepo;
    private readonly IPasienRepo _pasieRepo;
    private const int LIMIT_CONTER = 200;

    public PasienSearchHandler(IPasienRepo pasienRepo,
        IPolisRepo polisRepo,
        IRegRepo regRepo,
        IPasienRepo pasieRepo)
    {
        _pasienRepo = pasienRepo;
        _polisRepo = polisRepo;
        _regRepo = regRepo;
        _pasieRepo = pasieRepo;
    }

    public Task<IEnumerable<PasienSearchResponse>> Handle(PasienSearchQuery request, CancellationToken cancellationToken)
    {
        var isNik = IsKeywordNik(request.Keyword);
        var isNoPesertaBpjs = IsNoPesertaBpjs(request.Keyword);
        var isReg = IsRegistrasi(request.Keyword);


        List<PasienPersonView> datas = new();
        switch (true)
        {
            case var _ when isNik:
                var pasienNik = GetPasienByNik(request.Keyword);
                if (!pasienNik.IsEmpty)
                    datas.Add(pasienNik);
                break;

            case var _ when isNoPesertaBpjs:
                var pasienPeserta = GetPasienByNoPesertaBpjs(request.Keyword);
                if (!pasienPeserta.IsEmpty)
                    datas.Add(pasienPeserta);
                break;

            case var _ when isReg:
                var reg = GetRegistrasi(request.Keyword);
                if(reg.IsActive)
                    datas.Add(reg);
                break;

            default:
                var resultNamaTglLahir = _pasienRepo.SearchPasien(request.Keyword)?.ToList() ?? [];
                datas.AddRange(resultNamaTglLahir);
                break;
        }

        if (datas.Count > LIMIT_CONTER)
            throw new TooManyResultsException(LIMIT_CONTER, "Gunakan keyword search lebih spesifik");

        var result = datas.Select(x => new PasienSearchResponse(x.PasienId, x.IsActive, x.Person));
        return Task.FromResult(result.AsEnumerable());
    }


    #region PRIVATE-HELPER
    private static bool IsKeywordNik(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return false;

        // harus 16 digit angka
        if (Regex.IsMatch(keyword, @"^\d{16}$"))
            return true;
        return false;
    }
    private static bool IsNoPesertaBpjs(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return false;

        // harus 13 digit angka
        if (Regex.IsMatch(keyword, @"^\d{13}$"))
            return true;
        return false;
    }
    private static bool IsRegistrasi(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return false;
        if(keyword.Left(2) == "RG" && keyword.Trim().Length == 10)
            return true;
        return false;
    }
    private PasienPersonView GetPasienByNik(string nik)
    {
        var pasienPersonViewDefault = new PasienPersonView("-", false, PersonInfoType.Default);
        var resultNik = _pasienRepo.GetDataByNik(nik).GetValueOrDefault(pasienPersonViewDefault);
        return resultNik;
    }
    private PasienPersonView GetPasienByNoPesertaBpjs(string noPerseta)
    {
        var polis = _polisRepo.GetDataByNoPeserta(noPerseta);
        
        var peserta = polis.ListCover.FirstOrDefault(x => x.Status.StatusCode == "P") ??
            new PolisCoverModel("-", 
                new PasienReff("-", "-", new DateOnly(3000,1,1), "-"), 
                new StatusPesertaType("-", "-"));
        var pasien = _pasienRepo.LoadEntity(PasienModel.Key(peserta.Pasien.PasienId))
            .GetValueOrDefault(PasienModel.Default);
        var result = new PasienPersonView(
            pasien.PasienId, pasien.IsAktif, pasien.Person);
        return result;
    }

    private PasienPersonView GetRegistrasi(string regId)
    {
        var pasienPersonViewDefault = new PasienPersonView("-", false, PersonInfoType.Default);
        var reg = _regRepo.LoadEntity(RegModel.Key(regId)).GetValueOrDefault(RegModel.Default);
        var pasien = reg.RegId == "-"
            ? PasienModel.Default
            : _pasienRepo.LoadEntity(PasienModel.Key(reg.Pasien.PasienId))
                .GetValueOrDefault(PasienModel.Default);

        var resultData = new PasienPersonView(reg.Pasien.PasienId, pasien.IsAktif, pasien.Person);
        var result = reg.RegId == "-" ? pasienPersonViewDefault : resultData;

        return result;

    }
    #endregion
}


