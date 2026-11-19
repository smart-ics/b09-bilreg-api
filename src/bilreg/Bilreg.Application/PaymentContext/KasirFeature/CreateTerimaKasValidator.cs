using Bilreg.Domain.PaymentContext.KasirFeature;
using FluentValidation;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public class CreateTerimaKasValidator : AbstractValidator<CreateTerimaKasCommand>
{
    private static readonly string[] ValidPaymentMethods =
    {
        KasirConst.MetodeBayarTunai,
        KasirConst.MetodeBayarDebit,
        KasirConst.MetodeBayarKredit,
        KasirConst.MetodeBayarTransfer,
        KasirConst.MetodeBayarQris
    };

    public CreateTerimaKasValidator()
    {
        RuleFor(x => x.ShiftId)
            .NotEmpty().WithMessage("ShiftId wajib diisi.");

        RuleFor(x => x.KasirId)
            .NotEmpty().WithMessage("KasirId wajib diisi.");

        RuleFor(x => x.TotalNilai)
            .GreaterThan(0).WithMessage("Total nilai transaksi kasir harus lebih besar dari nol.");

        RuleFor(x => x.Payments)
            .NotNull().WithMessage("Rincian metode pembayaran wajib diisi minimal satu.")
            .Must(p => p != null && p.Any()).WithMessage("Rincian metode pembayaran wajib diisi minimal satu.");

        RuleForEach(x => x.Payments).ChildRules(item =>
        {
            item.RuleFor(p => p.MetodeBayar)
                .NotEmpty().WithMessage("Metode bayar wajib diisi.")
                .Must(m => ValidPaymentMethods.Contains(m?.Trim().ToUpperInvariant()))
                .WithMessage("Metode bayar tidak valid. Harus TUNAI, DEBIT, KREDIT, TRANSFER, atau QRIS.");

            item.RuleFor(p => p.NilaiBayar)
                .GreaterThan(0).WithMessage("Nilai bayar harus lebih besar dari nol.");

            item.RuleFor(p => p.NilaiKembalian)
                .GreaterThanOrEqualTo(0).WithMessage("Nilai kembalian tidak boleh negatif.");
        });

        When(x => x.Items != null && x.Items.Any(), () =>
        {
            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.BillId)
                    .NotEmpty().WithMessage("BillId item tagihan wajib diisi.");

                item.RuleFor(i => i.NilaiBayar)
                    .GreaterThanOrEqualTo(0).WithMessage("Nilai bayar item tagihan tidak boleh negatif.");
            });
        });
    }
}
