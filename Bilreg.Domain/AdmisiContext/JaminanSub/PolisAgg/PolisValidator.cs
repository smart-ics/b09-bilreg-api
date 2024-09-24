using System.Data;
using FluentValidation;

namespace Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;

public class PolisValidator : AbstractValidator<PolisModel>
{
    public PolisValidator()
    {
        RuleFor(x => x.PolisId).NotEmpty();

        RuleFor(x => x.NoPolis).NotEmpty();

        RuleFor(x => x.AtasNama).NotEmpty();

        RuleFor(x => x.TipeJaminanId).NotEmpty();
        RuleFor(x => x.TipeJaminanName).NotEmpty();
        RuleFor(x => x.KelasId).NotEmpty();
        RuleFor(x => x.KelasName).NotEmpty();
        
        RuleFor(x => x.ListCover)
            .Must((hdr, listDtl) => listDtl.All(dtl => dtl.PolisId == hdr.PolisId));

        RuleForEach(x => x.ListCover)
            .SetValidator(new PolisCoverValidator());
    }
}

public class PolisCoverValidator : AbstractValidator<PolisCoverModel>
{
    public PolisCoverValidator()
    {
        RuleFor(x => x.PolisId).NotEmpty();
        RuleFor(x => x.PasienId).NotEmpty();
        RuleFor(x => x.PasienName).NotEmpty();
        
        var validStatus = new[] { "P", "S", "I", "A", "O", "X" };
        RuleFor(x => x.Status).Must(status => validStatus.Contains(status));
        RuleFor(x => x.StatusDesc).NotEmpty();
    }
}