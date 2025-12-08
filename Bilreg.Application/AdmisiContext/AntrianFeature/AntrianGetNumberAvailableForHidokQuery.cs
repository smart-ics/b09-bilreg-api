using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record AntrianGetNumberAvailableForHidokCommand(string DokterHidokId, string TglAntrianYmd, string JamMulai): IRequest<AntrianGetNumberAvailableForHidokResponse>;

public record AntrianGetNumberAvailableForHidokResponse(int NextAvailableQueueNumber);

public class AntrianGetNumberAvailableForHidokhandler : IRequestHandler<AntrianGetNumberAvailableForHidokCommand, AntrianGetNumberAvailableForHidokResponse>
{
    private readonly IPpaRepo _ppaRepo;
    private readonly ISequencer _sequencer;
    public AntrianGetNumberAvailableForHidokhandler(
        IPpaRepo ppaRepo,
        ISequencer sequencer)
    {
        _ppaRepo = ppaRepo;
        _sequencer = sequencer;
    }

    public Task<AntrianGetNumberAvailableForHidokResponse> Handle(AntrianGetNumberAvailableForHidokCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrEmpty(request.DokterHidokId);
        Guard.Against.InvalidDateFormat(request.TglAntrianYmd, nameof(request.TglAntrianYmd));
        Guard.Against.InvalidTimeFormat(request.JamMulai, nameof(request.JamMulai));


        var finder = new ContactFinder(JenisContactEnum.Email, request.DokterHidokId);
        var dokter = _ppaRepo.LoadEntity(finder)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Dokter {request.DokterHidokId} not found")
            );

        // BUILD
        DateOnly tglAntrian = DateOnly.ParseExact(request.TglAntrianYmd, 
            "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        
        var sequenceTag = AntrianModel.GenSequenceTag(tglAntrian, dokter);   
        var availableQueueNumber = _sequencer.GetNextNoUrut(sequenceTag);
        
        // RETURN
        var result = new AntrianGetNumberAvailableForHidokResponse(availableQueueNumber); 
        return Task.FromResult(result);
    }   
}
