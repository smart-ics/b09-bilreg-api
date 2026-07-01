using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienAddContactCommand(string PasienId, int JenisContact, string DetilContact) : IRequest, IPasienKey;

public class PasienAddContactHandler : IRequestHandler<PasienAddContactCommand>
{
    private readonly IPasienRepo _pasienRepo;

    public PasienAddContactHandler(IPasienRepo pasienRepo)
    {
        _pasienRepo = pasienRepo;
    }

    public Task Handle(PasienAddContactCommand request, CancellationToken cancellationToken)
    {
        var pasien = _pasienRepo.LoadEntity(PasienModel.Key(request.PasienId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Pasien id {request.PasienId} not found")
            );

        var contact = new ContactType((JenisContactEnum)request.JenisContact, request.DetilContact);
        pasien.AddContact(contact);

        _pasienRepo.SaveChanges(pasien);
        return Task.CompletedTask;
    }
}
