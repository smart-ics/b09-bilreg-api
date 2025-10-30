using Bilreg.Application.PasienContext.DemografiFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienSetKtpCommand(string PasienId, string Nik, 
    string AlamtaKtp1, string AlamatKtp2, string AlamatKtp3,
    string Gender, string TempatLahir, string TglLahir, string Agama, string StatusKawin,
    string KotaKtp, string KodePosKtp, string Rt, string Rw, string KelurahanKtpId) : 
    IRequest, IPasienKey;



public class PasienSetKtpHandler : IRequestHandler<PasienSetKtpCommand>
{
    private readonly IPasienRepo _pasienRepo;
    private readonly IKelurahanRepo _kelurahanRepo;
    public PasienSetKtpHandler(IPasienRepo pasienRepo, 
        IKelurahanRepo kelurahanRepo)
    {
        _pasienRepo = pasienRepo;
        _kelurahanRepo = kelurahanRepo;
    }

    public Task Handle(PasienSetKtpCommand request, CancellationToken cancellationToken)
    {
        var pasien = _pasienRepo.LoadEntity(PasienModel.Key(request.PasienId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Pasien id {request.PasienId} not found")
            );

        var tglLahir = request.TglLahir.ToDate("yyyy-MM-dd");
        DateOnly tgllahirDate = DateOnly.FromDateTime(tglLahir);
        var person = new PersonInfoType(pasien.Person.PersonName, tgllahirDate,
            request.Gender, pasien.Person.Alamat, pasien.Person.Contact, pasien.Person.Identity);

        var alamat = new AlamatType([request.AlamtaKtp1, request.AlamatKtp2, request.AlamatKtp3], 
            request.KotaKtp, request.KodePosKtp);
        var kelurahan = _kelurahanRepo.LoadEntity(KelurahanType.Key(request.KelurahanKtpId))
            .Match(
                onSome: x => x,
                onNone: () => throw new ArgumentException("Invalid Kelurahan KTP")); 
        
        var ktp = new KtpType(request.Nik, alamat, request.Rt, request.Rw, kelurahan);
        
        pasien.SetPersonInfo(person, request.TempatLahir);
        pasien.SetDataKtp(ktp);

        _pasienRepo.SaveChanges(pasien);
        return Task.CompletedTask;
    }
}
