using Bilreg.Application.PasienContext.DemografiSub.KelurahanAgg;
using Bilreg.Application.PasienContext.StatusSosialSub.AgamaAgg;
using Bilreg.Application.PasienContext.StatusSosialSub.PekerjaanDkAgg;
using Bilreg.Application.PasienContext.StatusSosialSub.PendidikanDkAgg;
using Bilreg.Application.PasienContext.StatusSosialSub.StatusKawinDkAgg;
using Bilreg.Application.PasienContext.StatusSosialSub.SukuAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.AgamaAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.PekerjaanDkAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.PendidikanDkAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.StatusKawinDkAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.SukuAgg;
using MediatR;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public record PasienUpdateCommand(
    string PasienId,
    AddressType Address,
    string KelurahanId,
    IdentityType Identity,
    ContactType Contact,
    KeluargaType Keluarga,
    string StatusKawinId,
    string AgamaId,
    string SukuId,
    string PendidikanDkId,
    string PekerjaanDkId
    ) : IRequest;

public class PasienUpdateHandler : IRequestHandler<PasienUpdateCommand>
{
    private readonly IPasienDal _pasienDal;
    private readonly IKelurahanDal _kelurahanDal;
    private readonly IStatusKawinDkDal _statusKawinDkDal;
    private readonly IAgamaDal _agamaDal;
    private readonly ISukuDal _sukuDal;
    private readonly IPendidikanDkDal _pendidikanDkDal;
    private readonly IPekerjaanDkDal _pekerjaanDkDal;
    
    
    private readonly IPasienWriter _writer;
    private const string FORMAT_TGL_YMD = "yyyy-MM-dd";

    public PasienUpdateHandler(IPasienDal pasienDal, 
        IKelurahanDal kelurahanDal, IPasienWriter writer, 
        IStatusKawinDkDal statusKawinDkDal, IAgamaDal agamaDal, 
        ISukuDal sukuDal, IPendidikanDkDal pendidikanDkDal, 
        IPekerjaanDkDal pekerjaanDkDal)
    {
        _pasienDal = pasienDal;
        _kelurahanDal = kelurahanDal;
        _writer = writer;
        _statusKawinDkDal = statusKawinDkDal;
        _agamaDal = agamaDal;
        _sukuDal = sukuDal;
        _pendidikanDkDal = pendidikanDkDal;
        _pekerjaanDkDal = pekerjaanDkDal;
    }

    public Task Handle(PasienUpdateCommand request, CancellationToken cancellationToken)
    {
        var pasien = _pasienDal
            .GetData2(new PasienKey(request.PasienId))
            .OrThrowNotFoundException()
            .Value;

        var kelurahan = _kelurahanDal
            .GetData(new KelurahanKey(request.KelurahanId))
            ?? throw new KeyNotFoundException($"Kelurahan id : '{request.KelurahanId}' not found");
        pasien.SetAdministrativeInfo(request.Address, kelurahan, request.Identity, 
            request.Contact, request.Keluarga);

        var statusKawin = _statusKawinDkDal
            .GetData2(new StatusKawinDkKey(request.StatusKawinId))
            .OrMapEmptyTo(StatusKawinDkModel.Default)
            .OrThrowNotFoundException()
            .Value;

        var agama = _agamaDal
            .GetData2(new AgamaKey(request.AgamaId))
            .OrMapEmptyTo(AgamaModel.Default)
            .OrThrowNotFoundException()
            .Value;

        var suku = _sukuDal
            .GetData2(new SukuKey(request.SukuId))
            .OrMapEmptyTo(SukuModel.Default)
            .OrThrowNotFoundException()
            .Value;
        
        var pendidikan = _pendidikanDkDal
            .GetData2(new PendidikanDkKey(request.PendidikanDkId))
            .OrMapEmptyTo(PendidikanDkModel.Default)
            .OrThrowNotFoundException()
            .Value;

        var pekerjaan = _pekerjaanDkDal
            .GetData2(new PekerjaanDkKey(request.PekerjaanDkId))
            .OrMapEmptyTo(PekerjaanDkModel.Default)
            .OrThrowNotFoundException()
            .Value;
        
        pasien.SetStatusSosial(statusKawin, agama, suku, pekerjaan, pendidikan);

        _writer.Save(pasien);
        return Task.CompletedTask;
    }
}