using MediatR;

namespace Bilreg.Application.ChargeContext.TindakanFeature.UseCases;

public record TdkUpdatePetugasCmd(
    string TindakanId,
    string PpaId,
    string UserId = ""
) : IRequest<TdkUpdatePetugasResponse>;

public record TdkUpdatePetugasResponse(
    string TindakanId,
    string PpaId,
    string DokterName
);

public class TdkUpdatePetugasHandler : IRequestHandler<TdkUpdatePetugasCmd, TdkUpdatePetugasResponse>
{
    private readonly ITindakanRepo _tindakanRepo;

    public TdkUpdatePetugasHandler(ITindakanRepo tindakanRepo)
    {
        _tindakanRepo = tindakanRepo;
    }

    public Task<TdkUpdatePetugasResponse> Handle(TdkUpdatePetugasCmd request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TindakanId))
            throw new ArgumentException("TindakanId tidak boleh kosong");

        if (string.IsNullOrWhiteSpace(request.PpaId))
            throw new ArgumentException("PpaId dokter tidak boleh kosong");

        _tindakanRepo.UpdatePetugas(request.TindakanId, request.PpaId, request.UserId);
        var dokterName = _tindakanRepo.GetPetugasName(request.PpaId);

        return Task.FromResult(new TdkUpdatePetugasResponse(
            request.TindakanId,
            request.PpaId,
            dokterName
        ));
    }
}
