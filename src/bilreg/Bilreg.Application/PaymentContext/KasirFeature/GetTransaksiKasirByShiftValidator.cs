using FluentValidation;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public class GetTransaksiKasirByShiftValidator : AbstractValidator<GetTransaksiKasirByShiftQuery>
{
    public GetTransaksiKasirByShiftValidator()
    {
        RuleFor(x => x.ShiftId)
            .NotEmpty().WithMessage("ShiftId wajib diisi.");
    }
}
