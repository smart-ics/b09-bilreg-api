using Bilreg.Application.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienUpdateAdminInfoCmd(string PasienId, 
    //
    string KelurahanId, string NoKartuKeluarga,
    string Email, string NoHp,
    //
    string NamaKeluarga, string Relasi, string NoTelpKeluarga,
    string Alamat1Keluarga, string Alamat2Keluarga) : IRequest;

public class PasienUpdateAdminInfoCmdHandler : IRequestHandler<PasienUpdateAdminInfoCmd>
{
    private readonly IPasienRepo _pasienRepo;
    private readonly IKelurahanRepo _kelurahanRepo;

    public PasienUpdateAdminInfoCmdHandler(IPasienRepo pasienRepo, 
        IKelurahanRepo kelurahanRepo)
    {
        _pasienRepo = pasienRepo;
        _kelurahanRepo = kelurahanRepo;
    }

    public Task Handle(PasienUpdateAdminInfoCmd request, CancellationToken cancellationToken)
    {
        var pasien = _pasienRepo.LoadEntity(PasienModel.Key(request.PasienId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Pasien id {request.PasienId} not found")
            );
            
        var kelurahan = _kelurahanRepo.LoadEntity(KelurahanType.Key(request.KelurahanId))
            .Match(
                onSome: x => x,
                onNone: () => throw new ArgumentException("Invalid Kelurahan ID"));
        var kartuKeluarga = IdentitasType.Kk(request.NoKartuKeluarga);
        var email = new ContactType(JenisContactEnum.Email, request.Email);
        var noHp = new ContactType(JenisContactEnum.Mobile, request.NoHp);
        var noTelpKeluarga = new ContactType(JenisContactEnum.Phone, request.NoTelpKeluarga);
        var keluarga = new PasienKeluargaType(request.NamaKeluarga, request.Relasi, noTelpKeluarga,
            new AlamatType([request.Alamat1Keluarga, request.Alamat2Keluarga, "-"], "-", "-"));
        pasien.UpdateAdminInfo(kelurahan, kartuKeluarga, email, noHp, keluarga);

        _pasienRepo.SaveChanges(pasien);
        return Task.CompletedTask;
    }
}

