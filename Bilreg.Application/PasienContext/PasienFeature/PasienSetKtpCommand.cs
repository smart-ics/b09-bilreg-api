using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienSetKtpCommand(string PasienId, string Nik, string Alamta1, string Alamat2,
    string Kota, string KodePos, string Rt, string Rw, string KelurahanId) : 
    IRequest<PasienSetKptRespons>, IPasienKey;


public record PasienSetKptRespons(string PasienId);

public class PasienSetKtpHandler : IRequestHandler<PasienSetKtpCommand,  PasienSetKptRespons>
{
    private readonly IPasienRepo _repo;
    public PasienSetKtpHandler(IPasienRepo repo)
    {
        _repo = repo;
    }

    public Task<PasienSetKptRespons> Handle(PasienSetKtpCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
