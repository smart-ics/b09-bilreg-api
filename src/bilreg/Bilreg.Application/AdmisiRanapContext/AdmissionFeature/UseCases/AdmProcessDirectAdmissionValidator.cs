using FluentValidation;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public class AdmProcessDirectAdmissionValidator : AbstractValidator<AdmProcessDirectAdmissionCmd>
{
    public AdmProcessDirectAdmissionValidator()
    {
        RuleFor(x => x.PasienId)
            .NotEmpty().WithMessage("PasienId tidak boleh kosong.");

        RuleFor(x => x.KelasDkId)
            .NotEmpty().WithMessage("KelasDkId tidak boleh kosong.");

        RuleFor(x => x.BangsalId)
            .NotEmpty().WithMessage("BangsalId tidak boleh kosong.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId tidak boleh kosong.");

        RuleFor(x => x.Registration)
            .NotNull().WithMessage("Registration tidak boleh kosong.");

        When(x => x.Registration is not null, () =>
        {
            RuleFor(x => x.Registration.DokterId)
                .NotEmpty().WithMessage("DokterId tidak boleh kosong.");

            RuleFor(x => x.Registration.TipeJaminanId)
                .NotEmpty().WithMessage("TipeJaminanId tidak boleh kosong.");

            RuleFor(x => x.Registration.CaraMasukDkId)
                .NotEmpty().WithMessage("CaraMasukDkId tidak boleh kosong.");

            RuleFor(x => x.Registration.ProsedurMasukInapId)
                .NotEmpty().WithMessage("ProsedurMasukInapId tidak boleh kosong.");
        });
    }
}
