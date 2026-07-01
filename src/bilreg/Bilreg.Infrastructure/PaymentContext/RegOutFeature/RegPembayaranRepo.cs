using Bilreg.Application.PaymentContext.RegOutFeature.RegOutAgg;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.RegOutFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.PaymentContext.RegOutFeature;

public class RegPembayaranRepo : IRegPembayaranRepo
{
    private readonly ITataRekeningRepo _tataRekeningRepo;

    public RegPembayaranRepo(ITataRekeningRepo tataRekeningRepo)
    {
        _tataRekeningRepo = tataRekeningRepo;
    }

    public void Insert(RegPembayaranType model)
    {
        var payment = ToTataRekeningPayment(model);
        var key = RegModel.Key(model.RegId);
        var updated = _tataRekeningRepo.LoadEntity(key)
            .Match(
                onSome: existing => TataRekeningModelRebuilder.WithPayments(
                    existing,
                    existing.ListPayment.Append(payment)),
                onNone: () => TataRekeningModelRebuilder.WithPayments(
                    TataRekeningModel.Create(model.RegId),
                    [payment]));

        _tataRekeningRepo.SaveChanges(updated);
    }

    public void Update(RegPembayaranType model)
    {
        var key = RegModel.Key(model.RegId);
        var existing = _tataRekeningRepo.LoadEntity(key)
            .GetValueOrThrow($"Tata Rekening '{model.RegId}' tidak ditemukan.");

        var payment = ToTataRekeningPayment(model);
        var updatedPayments = existing.ListPayment
            .Select(p => string.Equals(p.Payment.PaymentId, model.CaraBayarId, StringComparison.Ordinal)
                ? payment
                : p)
            .ToList();

        if (updatedPayments.All(p => !string.Equals(p.Payment.PaymentId, model.CaraBayarId, StringComparison.Ordinal)))
            updatedPayments.Add(payment);

        var updated = TataRekeningModelRebuilder.WithPayments(existing, updatedPayments);
        _tataRekeningRepo.SaveChanges(updated);
    }

    public IEnumerable<RegPembayaranType> ListData(IRegKey key)
    {
        return _tataRekeningRepo.LoadEntity(key)
            .Match(
                onSome: model => model.ListPayment.Select(p => ToRegPembayaran(key.RegId, p)),
                onNone: () => Enumerable.Empty<RegPembayaranType>());
    }

    private static TataRekeningPaymentType ToTataRekeningPayment(RegPembayaranType model) =>
        new(
            new PaymentType(model.CaraBayarId, model.CaraBayarName, ResolveIsTipeJaminan(model.CaraBayarId)),
            model.NilaiJasa,
            model.NilaiObat,
            CoaType.Default);

    private static RegPembayaranType ToRegPembayaran(string regId, TataRekeningPaymentType payment) =>
        new(
            regId,
            payment.Payment.PaymentId,
            payment.Payment.PaymentName,
            payment.NilaiJasa,
            payment.NilaiObat,
            payment.NilaiJasa + payment.NilaiObat);

    private static bool ResolveIsTipeJaminan(string paymentId) =>
        paymentId is not ("BYKAS" or "BYPRI" or "BYDPU" or "BYVCH" or "BYDPK");
}
