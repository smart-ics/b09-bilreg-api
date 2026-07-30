using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Bilreg.Application.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;

public record AntrianGenNewNumberCommand(string DokterHidokId, string TglAntrianYmd, string JamMulai): IRequest<AntrianGenNewNumberResponse>;

public record AntrianGenNewNumberResponse(int NewNumber);

public class AntrianGenNewNumberHandler : IRequestHandler<AntrianGenNewNumberCommand, AntrianGenNewNumberResponse>
{
    private readonly IPpaRepo _ppaRepo;
    private readonly ISequencer _sequencer;
    public AntrianGenNewNumberHandler(
        IPpaRepo ppaRepo,
        ISequencer sequencer)
    {
        _ppaRepo = ppaRepo;
        _sequencer = sequencer;
    }

    public Task<AntrianGenNewNumberResponse> Handle(AntrianGenNewNumberCommand request, CancellationToken cancellationToken)
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
        var jamPraktek = TimeOnly.Parse(request.JamMulai);   
        var sequenceTag = AntrianModel.GenSequenceTag(tglAntrian, jamPraktek, dokter);   
        var availableQueueNumber = _sequencer.GetNextNoUrut(sequenceTag);
        
        // RETURN
        var result = new AntrianGenNewNumberResponse(availableQueueNumber); 
        return Task.FromResult(result);
    }   
}
