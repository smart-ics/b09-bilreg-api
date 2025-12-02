using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.Helpers;
using CommunityToolkit.Diagnostics;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record AntrianGetNumberAvailableForHidokCommand(string DokterHidokId, string TglAntrianYmd, string JamMulai): IRequest<AntrianGetNumberAvailableForHidokResponse>;

public record AntrianGetNumberAvailableForHidokResponse(int NextAvailableQueueNumber);

public class AntrianGetNumberAvailableForHidokhandler : IRequestHandler<AntrianGetNumberAvailableForHidokCommand, AntrianGetNumberAvailableForHidokResponse>
{
    private readonly IPpaRepo _ppaRepo;
    private readonly ISequencer _sequencer;
    private readonly IPpaMapHidokRepo _ppaMapHidokRepo;
    private const string FORMAT_TGL_YMD = "yyyy-MM-dd";
    public AntrianGetNumberAvailableForHidokhandler(
        IPpaRepo ppaRepo,
        ISequencer sequencer,
        IPpaMapHidokRepo ppaMapHidokRepo)
    {
        _ppaRepo = ppaRepo;
        _sequencer = sequencer;
        _ppaMapHidokRepo = ppaMapHidokRepo;
    }

    public Task<AntrianGetNumberAvailableForHidokResponse> Handle(AntrianGetNumberAvailableForHidokCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotEmpty(request.TglAntrianYmd);
        Guard.IsTrue(request.TglAntrianYmd.IsValidTgl(FORMAT_TGL_YMD));
        var dokterHidok = _ppaMapHidokRepo.LoadEntity(PpaMapHidokType.Key(request.DokterHidokId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Mapping Dokter {request.DokterHidokId} not found")
            );
        var dokter = _ppaRepo.LoadEntity(PpaType.Key(dokterHidok.PpaId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Dokter {dokterHidok.PpaId} not found")
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
