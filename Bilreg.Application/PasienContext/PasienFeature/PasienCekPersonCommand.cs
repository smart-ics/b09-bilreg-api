using Bilreg.Domain.BillContext.TindakanSub.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienCekPersonCommand(string PasienId, string PasienName, 
    string TglLahir, string Gender) : IRequest<PasienCekPersonResponse>, IPasienKey;

public record PasienCekPersonResponse(string PasienId, string PasienName,
    string TglLahir, string Gender, bool IsMatch);

public class PasienCekPersonHandler : IRequestHandler<PasienCekPersonCommand, PasienCekPersonResponse>
{
    private readonly IPasienRepo _pasienRepo;

    public PasienCekPersonHandler(IPasienRepo pasienRepo)
    {
        _pasienRepo = pasienRepo;
    }

    public Task<PasienCekPersonResponse> Handle(PasienCekPersonCommand request, CancellationToken cancellationToken)
    {
        var pasien = _pasienRepo.LoadEntity(PasienModel.Key(request.PasienId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Pasien id {request.PasienId} not found")
            );
        bool isMatch =
            string.Equals(pasien.Person.PersonName, request.PasienName, StringComparison.OrdinalIgnoreCase) &&
            pasien.Person.TglLahir.ToString("yyyy-MM-dd") == request.TglLahir &&
            pasien.Person.Gender == request.Gender;

        var response = new PasienCekPersonResponse(pasien.PasienId, pasien.Person.PersonName,
            pasien.Person.TglLahir.ToString("yyyy-MM-dd"), pasien.Person.Gender, isMatch);

        return Task.FromResult(response);
    }
}