using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienSetDataKeluargaCommand(string PasienId,
    string KeluargaName, string Relasi, int JenisContact, string ContactDetil,
    string AlamatKeluarga1, string AlamatKeluarga2, string AlamatKeluarga3,
    string KotaKeluarga, string KodePosKeluarga) : IRequest, IPasienKey;

public class PasienSetDataKeluargaHandler : IRequestHandler<PasienSetDataKeluargaCommand>
{
    private readonly IPasienRepo _pasienRepo;

    public PasienSetDataKeluargaHandler(IPasienRepo pasienRepo)
    {
        _pasienRepo = pasienRepo;
    }

    public Task Handle(PasienSetDataKeluargaCommand request, CancellationToken cancellationToken)
    {
        var pasien = _pasienRepo.LoadEntity(PasienModel.Key(request.PasienId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Pasien id {request.PasienId} not found")
            );

        var contact = new ContactType((JenisContactEnum)request.JenisContact, request.ContactDetil);
        var alamat = new AlamatType([request.AlamatKeluarga1, request.AlamatKeluarga2, request.AlamatKeluarga3], 
            request.KotaKeluarga, request.KodePosKeluarga);
        var keluarga = new PasienKeluargaType(request.KeluargaName, request.Relasi,
            contact, alamat);

        _pasienRepo.SaveChanges(pasien);
        return Task.CompletedTask;
    }
}
