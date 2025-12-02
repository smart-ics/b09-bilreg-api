using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.Helpers;
using CommunityToolkit.Diagnostics;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record AntrianGetNumberAvailableForHidokCommand(string DokterId, string TglAntrianYmd, string JamMulai): IRequest<AntrianGetNumberAvailableForHidokResponse>;

public record AntrianGetNumberAvailableForHidokResponse(int NextAvailableQueueNumber);

public class AntrianGetNumberAvailableForHidokhandler : IRequestHandler<AntrianGetNumberAvailableForHidokCommand, AntrianGetNumberAvailableForHidokResponse>
{
    private readonly IPpaRepo _ppaRepo;
    private readonly ISequencer _sequencer;
    private const string FORMAT_TGL_YMD = "yyyy-MM-dd";
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
        Guard.IsNotEmpty(request.TglAntrianYmd);
        Guard.IsTrue(request.TglAntrianYmd.IsValidTgl(FORMAT_TGL_YMD));
        var dokter = _ppaRepo.LoadEntity(PpaType.Key(request.DokterId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Dokter {request.DokterId} not found")
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
