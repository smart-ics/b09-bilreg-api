using Bilreg.Application.PasienContext.DemografiFeature;
using Bilreg.Application.PasienContext.StatusSosialFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using MediatR;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienSetDemografiCommand(string PasienId, 
    string StatusKawinId, string AgamaId, 
    string SukuId, string PekerjaanId,
    string PendidikanId,
    string KelurahanId, string NoKartuKeluarga,
    string Email, string NoHp,
    string NamaKeluarga, string Relasi, string NoTelpKeluarga,
    string Alamat1Keluarga, string Alamat2Keluarga
    ) : IRequest, IPasienKey, IKelurahanKey;

public class PasienSetDemografiHandler : IRequestHandler<PasienSetDemografiCommand>
{
    private PasienModel _pasien;
    private readonly IPasienRepo _pasienRepo;
    private readonly IStatusKawinDkDal _statisKawinDal;
    private readonly IAgamaDal _agamaDal;
    private readonly ISukuDal _sukuDal;
    private readonly IPekerjaanDkDal _pekerjaanDal;
    private readonly IPendidikanDkDal _pendidikanDkDal;
    private readonly IKelurahanRepo _kelurahanRepo;
    public PasienSetDemografiHandler(IPasienRepo pasienRepo,
        IStatusKawinDkDal statisKawinDal,
        IAgamaDal agamaDal,
        ISukuDal sukuDal,
        IPekerjaanDkDal pekerjaanDal,
        IPendidikanDkDal pendidikanDkDal,
        IKelurahanRepo kelurahanRepo)
    {
        _pasienRepo = pasienRepo;
        _statisKawinDal = statisKawinDal;
        _agamaDal = agamaDal;
        _sukuDal = sukuDal;
        _pekerjaanDal = pekerjaanDal;
        _pendidikanDkDal = pendidikanDkDal;
        _kelurahanRepo = kelurahanRepo;

        _pasien = PasienModel.Default;
    }

    public Task Handle(PasienSetDemografiCommand request, CancellationToken cancellationToken)
    {
        _pasien = _pasienRepo.LoadEntity(PasienModel.Key(request.PasienId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Pasien id {request.PasienId} not found")
            );

        SetAdminData(request, request.NoKartuKeluarga, request.Email, request.NoHp, request.NoTelpKeluarga, request.NamaKeluarga,
            request.Relasi, request.Alamat1Keluarga, request.Alamat2Keluarga);

        SetDemografi(request.StatusKawinId, request.AgamaId, request.SukuId, request.PekerjaanId, request.PendidikanId);

        //var statusKawinDk = _statisKawinDal.GetData(StatusKawinDkType.Key(request.StatusKawinId))
        //    .Match(
        //        onSome: x => x,
        //        onNone: () => throw new KeyNotFoundException($"Status Kawin id {request.StatusKawinId} not found")
        //    );

        //var agama = _agamaDal.GetData(AgamaType.Key(request.AgamaId))
        //    .Match(
        //        onSome: x => x,
        //        onNone : () => throw new KeyNotFoundException($"Agama Id {request.AgamaId} not found")
        //    );

        //var suku = _sukuDal.GetData(SukuType.Key(request.SukuId))
        //    .Match(
        //        onSome: x => x,
        //        onNone: () => throw new KeyNotFoundException($"Agama Id {request.AgamaId} not found")
        //    );

        //var pekerjaan = _pekerjaanDal.GetData(PekerjaanDkType.Key(request.PekerjaanId))
        //    .Match(
        //        onSome: x => x,
        //        onNone: () => throw new KeyNotFoundException($"Pekerjaan Id {request.PekerjaanId} not found")
        //    );

        //var pendidikan = _pendidikanDkDal.GetData(PendidikanDkType.Key(request.PendidikanId))
        //    .Match(
        //        onSome: x => x,
        //        onNone: () => throw new KeyNotFoundException($"Pendidikan Id {request.PendidikanId} not found")
        //    );


        //var kelurahan = _kelurahanRepo.LoadEntity(KelurahanType.Key(request.KelurahanId))
        //    .Match(
        //        onSome: x => x,
        //        onNone: () => throw new ArgumentException("Invalid Kelurahan ID"));
        //var kartuKeluarga = IdentitasType.Kk(request.NoKartuKeluarga);
        //var email = new ContactType(JenisContactEnum.Email, request.Email);
        //var noHp = new ContactType(JenisContactEnum.Mobile, request.NoHp);
        //var noTelpKeluarga = new ContactType(JenisContactEnum.Phone, request.NoTelpKeluarga);
        //var keluarga = new PasienKeluargaType(request.NamaKeluarga, request.Relasi, noTelpKeluarga,
        //    new AlamatType([request.Alamat1Keluarga, request.Alamat2Keluarga, "-"], "-", "-"));


        //pasien.UpdateAdminInfo(kelurahan, kartuKeluarga, email, noHp, keluarga);
        //pasien.SetStatusSosial(statusKawinDk, agama, suku, pekerjaan, pendidikan);

        _pasienRepo.SaveChanges(_pasien);

        return Task.CompletedTask;
    }

    private void SetDemografi(string statusKawinId, string agamaId, string sukuId, string pekerjaanId, string pendidikanId)
    {
        var statusKawinDk = _statisKawinDal.GetData(StatusKawinDkType.Key(statusKawinId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Status Kawin id {statusKawinId} not found")
            );

        var agama = _agamaDal.GetData(AgamaType.Key(agamaId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Agama Id {agamaId} not found")
            );

        var suku = _sukuDal.GetData(SukuType.Key(sukuId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Agama Id {sukuId} not found")
            );

        var pekerjaan = _pekerjaanDal.GetData(PekerjaanDkType.Key(pekerjaanId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Pekerjaan Id {pekerjaanId} not found")
            );

        var pendidikan = _pendidikanDkDal.GetData(PendidikanDkType.Key(pendidikanId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Pendidikan Id {pendidikanId} not found")
            );

        _pasien.SetStatusSosial(statusKawinDk, agama, suku, pekerjaan, pendidikan);
    }
    private void SetAdminData(IKelurahanKey kelurahanKey, string noKartuKeluarga, string email, 
        string noHp, string noTelpKeluarga, 
        string namaKeluarga, string relasi, string alamatKeluarga1, string alamatKeluarga2)
    {
        var kelurahan = _kelurahanRepo.LoadEntity(kelurahanKey)
            .Match(
                onSome: x => x,
                onNone: () => throw new ArgumentException("Invalid Kelurahan ID"));

        var kartuKeluarga = IdentitasType.Kk(noKartuKeluarga);
        var emailContact = new ContactType(JenisContactEnum.Email, email);
        var noHpContact = new ContactType(JenisContactEnum.Mobile, noHp);
        var noTelpKeluargaContact = new ContactType(JenisContactEnum.Phone, noTelpKeluarga);
        var keluarga = new PasienKeluargaType(namaKeluarga, relasi, noTelpKeluargaContact,
            new AlamatType([alamatKeluarga1, alamatKeluarga2, "-"], "-", "-"));

        _pasien.UpdateAdminInfo(kelurahan, kartuKeluarga, emailContact, noHpContact, keluarga);
    }
}
