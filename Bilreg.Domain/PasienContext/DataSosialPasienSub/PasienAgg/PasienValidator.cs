using FluentValidation;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

public class PasienValidator : AbstractValidator<PasienModel>
{
    public PasienValidator()
    {
        RuleFor(x => x.PasienId).NotEmpty();
        RuleFor(x => x.PasienName).NotEmpty();
        RuleFor(x => x.TglLahir).Must(BeAValidDate);
        RuleFor(x => x.Gender).Must(BeAValidGender);
        RuleFor(x => x.TglMedrec).Must(BeAValidDate);
        RuleFor(x => x.IbuKandung).NotEmpty();
        RuleFor(x => x.GolDarah).Must(BeAValidGolDarah);

        RuleFor(x => x.StatusNikahName).NotEmpty()
            .When(x => x.StatusNikahId.IsNotEmpty());
        RuleFor(x => x.AgamaName).NotEmpty()
            .When(x => x.AgamaId.IsNotEmpty());
        RuleFor(x => x.SukuName).NotEmpty()
            .When(x => x.SukuId.IsNotEmpty());
        RuleFor(x => x.PekerjaanDkName).NotEmpty()
            .When(x => x.PekerjaanDkId.IsNotEmpty());
        RuleFor(x => x.PendidikanDkName).NotEmpty()
            .When(x => x.PendidikanDkId.IsNotEmpty());
        
        RuleFor(x => x.KelurahanName).NotEmpty()
            .When(x => x.KelurahanId.IsNotEmpty());
        RuleFor(x => x.KecamatanName).NotEmpty()
            .When(x => x.KelurahanId.IsNotEmpty());
        RuleFor(x => x.KabupatenName).NotEmpty()
            .When(x => x.KelurahanId.IsNotEmpty());
        RuleFor(x => x.PropinsiName).NotEmpty()
            .When(x => x.KelurahanId.IsNotEmpty());

        RuleFor(x => x.NomorId).NotEmpty()
            .When(x => x.JenisId.IsNotEmpty());
    }

    private static bool BeAValidDate(DateTime date)
    {
        if (date == DateTime.MinValue)
            return false;
        return date != new DateTime(3000, 1, 1);
    }

    private static bool BeAValidGender(string gender)
    {
        var validData = new[] { "L", "P", "M", "F", "W", "0", "1" };
        return validData.Contains(gender);
    }

    private static bool BeAValidGolDarah(string golDarah)
    {
        var validData = new[] { "A", "B", "AB", "O" };
        return validData.Contains(golDarah);
    }
}