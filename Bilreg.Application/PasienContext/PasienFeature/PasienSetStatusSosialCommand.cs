using Bilreg.Application.PasienContext.StatusSosialFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using MediatR;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienSetStatusSosialCommand(string PasienId, string StatusKawinId,
    string AgamaId, string SukuId, string PekerjaanId,
    string PendidikanId) : IRequest, IPasienKey;

public class PasienSetStatusSosialHandler : IRequestHandler<PasienSetStatusSosialCommand>
{
    private readonly IPasienRepo _pasienRepo;
    private readonly IStatusKawinDkDal _statisKawinDal;
    private readonly IAgamaDal _agamaDal;
    private readonly ISukuDal _sukuDal;
    private readonly IPekerjaanDkDal _pekerjaanDal;
    private readonly IPendidikanDkDal _pendidikanDkDal;
    public PasienSetStatusSosialHandler(IPasienRepo pasienRepo,
        IStatusKawinDkDal statisKawinDal,
        IAgamaDal agamaDal,
        ISukuDal sukuDal,
        IPekerjaanDkDal pekerjaanDal,
        IPendidikanDkDal pendidikanDkDal)
    {
        _pasienRepo = pasienRepo;
        _statisKawinDal = statisKawinDal;
        _agamaDal = agamaDal;
        _sukuDal = sukuDal;
        _pekerjaanDal = pekerjaanDal;
        _pendidikanDkDal = pendidikanDkDal;
    }

    public Task Handle(PasienSetStatusSosialCommand request, CancellationToken cancellationToken)
    {
        var pasien = _pasienRepo.LoadEntity(PasienModel.Key(request.PasienId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Pasien id {request.PasienId} not found")
            );

        var statusKawinDk = _statisKawinDal.GetData(StatusKawinDkType.Key(request.StatusKawinId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Status Kawin id {request.StatusKawinId} not found")
            );
        
        var agama = _agamaDal.GetData(AgamaType.Key(request.AgamaId))
            .Match(
                onSome: x => x,
                onNone : () => throw new KeyNotFoundException($"Agama Id {request.AgamaId} not found")
            );
        
        var suku = _sukuDal.GetData(SukuType.Key(request.SukuId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Agama Id {request.AgamaId} not found")
            );

        var pekerjaan = _pekerjaanDal.GetData(PekerjaanDkType.Key(request.PekerjaanId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Pekerjaan Id {request.PekerjaanId} not found")
            );

        var pendidikan = _pendidikanDkDal.GetData(PendidikanDkType.Key(request.PendidikanId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Pendidikan Id {request.PendidikanId} not found")
            );


        pasien.SetStatusSosial(statusKawinDk, agama, suku, pekerjaan, pendidikan);
        _pasienRepo.SaveChanges(pasien);

        return Task.CompletedTask;
    }
}
