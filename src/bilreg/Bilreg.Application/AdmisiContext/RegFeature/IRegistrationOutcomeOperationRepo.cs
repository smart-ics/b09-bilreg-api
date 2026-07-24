using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public interface IRegistrationOutcomeOperationRepo
{
    bool TryFinalize(RegistrationOutcomeModel outcome,string loketKey,byte[] expectedClaimVersion,DateTime doneAt);
    bool TryFinalizeEstablishedCompatibility(RegistrationOutcomeModel outcome, AntrianEntryModel completedEntry);
    bool TryRecordLegacyEstablished(RegistrationOutcomeModel outcome);
}
