using Bilreg.Application.ApotekContext.PenjualanFeature;
using Bilreg.Domain.ApotekContext.PenjualanFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.ApotekContext.PenjualanFeature;

public class InMemoryPenjualanRepo : IPenjualanRepo
{
    public Dictionary<string, PenjualanModel> Store { get; } = new();

    public void SaveChanges(PenjualanModel model)
    {
        Store[model.PenjualanId] = model;
    }

    public MayBe<PenjualanModel> LoadEntity(IPenjualanKey key)
    {
        return Store.TryGetValue(key.PenjualanId, out var model)
            ? MayBe.From(model)
            : MayBe<PenjualanModel>.None;
    }

    public IReadOnlyList<PenjualanModel> ListByReg(string regId)
    {
        return Store.Values
            .Where(x => x.RegId == regId && !x.IsVoid)
            .OrderByDescending(x => x.PenjualanDate)
            .ToList();
    }

    public IReadOnlyList<PenjualanModel> ListWorklist(DateOnly businessDate)
    {
        return Store.Values
            .Where(x => DateOnly.FromDateTime(x.PenjualanDate) == businessDate && !x.IsVoid)
            .OrderBy(x => x.PenjualanDate)
            .ThenBy(x => x.PenjualanTime)
            .ToList();
    }

    public IReadOnlyList<PenjualanModel> ListByResep(string resepId)
    {
        return Store.Values
            .Where(x => x.ResepId == resepId && !x.IsVoid)
            .ToList();
    }
}

public class FakeTrkTarifPricingPort : ITrkTarifPricingPort
{
    public Dictionary<string, TrkTarifItemPricingResult> CustomPrices { get; } = new();
    public decimal DefaultHargaSatuan { get; set; } = 15000m;
    public decimal DefaultDiskon { get; set; } = 0m;
    public decimal DefaultBiaya_r { get; set; } = 1000m;
    public decimal DefaultBiaya_R { get; set; } = 3000m;
    public decimal DefaultHna { get; set; } = 12000m;
    public decimal DefaultMargin { get; set; } = 25m;

    public Task<TrkTarifItemPricingResult> CalculateItemPriceAsync(
        TrkTarifItemPricingRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CalculateItemPrice(request));
    }

    public TrkTarifItemPricingResult CalculateItemPrice(TrkTarifItemPricingRequest request)
    {
        if (CustomPrices.TryGetValue(request.BrgId, out var result))
            return result;

        return new TrkTarifItemPricingResult(
            request.BrgId,
            request.TipeJaminanId,
            DefaultHargaSatuan,
            DefaultDiskon,
            DefaultBiaya_r,
            DefaultBiaya_R,
            DefaultHna,
            DefaultMargin,
            IsFixedPriceApplied: false,
            IsHetCapped: false,
            HetNominal: 0m);
    }

    public TrkTarifRoundingResult CalculateRounding(
        decimal totalBeforeRounding,
        int roundingIncrement = 100,
        int roundingMode = 1)
    {
        if (roundingIncrement <= 0)
            return new TrkTarifRoundingResult(totalBeforeRounding, totalBeforeRounding, 0);

        decimal rounded;
        if (roundingMode == 2)
            rounded = Math.Ceiling(totalBeforeRounding / roundingIncrement) * roundingIncrement;
        else
            rounded = Math.Round(totalBeforeRounding / roundingIncrement, MidpointRounding.AwayFromZero) * roundingIncrement;

        return new TrkTarifRoundingResult(totalBeforeRounding, rounded, rounded - totalBeforeRounding);
    }
}

public class FakeTrkBillingChargePort : ITrkBillingChargePort
{
    public List<TrkBillingChargeRequest> ChargedRequests { get; } = new();
    public List<(string ChargeId, string ActorId, string Reason)> CancelledCharges { get; } = new();
    private int _sequence = 1;

    public Task<string> PostBillingChargeAsync(
        TrkBillingChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(PostBillingCharge(request));
    }

    public string PostBillingCharge(TrkBillingChargeRequest request)
    {
        ChargedRequests.Add(request);
        var chargeId = $"TB{DateTime.Now:yyyyMMdd}{_sequence++:D4}";
        return chargeId;
    }

    public Task CancelBillingChargeAsync(
        string chargeId,
        string actorId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        CancelBillingCharge(chargeId, actorId, reason);
        return Task.CompletedTask;
    }

    public void CancelBillingCharge(string chargeId, string actorId, string reason)
    {
        CancelledCharges.Add((chargeId, actorId, reason));
    }
}

public class FakeTataRekeningPermissionPort : ITataRekeningInvoicePermissionPort
{
    public bool AllowModificationResult { get; set; } = true;
    public bool AllowVoidResult { get; set; } = true;

    public bool AllowsModification(string tataRekeningChargeId) => AllowModificationResult;
    public bool AllowsVoid(PenjualanModel model) => AllowVoidResult;
}

public class FakePaymentClearancePort : IPaymentClearancePort
{
    public Dictionary<string, PaymentClearanceEvidence> Clearances { get; } = new();
    public bool DefaultIsCleared { get; set; } = true;

    public PaymentClearanceEvidence? Load(string paymentClearanceReff)
    {
        if (Clearances.TryGetValue(paymentClearanceReff, out var ev))
            return ev;

        if (DefaultIsCleared)
            return new PaymentClearanceEvidence(paymentClearanceReff, DateTime.Now, 50000m, "KASIR-01", "CASH");

        return null;
    }


    public bool IsCleared(string paymentClearanceReff)
    {
        if (Clearances.ContainsKey(paymentClearanceReff))
            return true;

        return DefaultIsCleared;
    }
}

public class FakeFornasCheckPort : IFornasCheckPort
{
    public HashSet<string> NonFornasItemIds { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsFornas(string regId, string brgId)
    {
        return !NonFornasItemIds.Contains(brgId);
    }
}

public class FakeRegRepo : Bilreg.Application.AdmisiContext.RegFeature.IRegRepo
{
    public Dictionary<string, Bilreg.Domain.AdmisiContext.RegFeature.RegModel> Store { get; } = new();

    public void SaveChanges(Bilreg.Domain.AdmisiContext.RegFeature.RegModel model) => Store[model.RegId] = model;

    public MayBe<Bilreg.Domain.AdmisiContext.RegFeature.RegModel> LoadEntity(Bilreg.Domain.AdmisiContext.RegFeature.IRegKey key)
        => Store.TryGetValue(key.RegId, out var m) ? MayBe.From(m) : MayBe<Bilreg.Domain.AdmisiContext.RegFeature.RegModel>.None;

    public void Delete(Bilreg.Domain.AdmisiContext.RegFeature.IRegKey key) => Store.Remove(key.RegId);

    public IEnumerable<Bilreg.Application.AdmisiContext.RegFeature.RegView> ListData(
        Nuna.Lib.ValidationHelper.Periode filter1,
        Bilreg.Domain.AdmisiContext.LayananFeature.ILayananKey filter2)
        => Enumerable.Empty<Bilreg.Application.AdmisiContext.RegFeature.RegView>();

    public bool IsPasienAktifReg(Bilreg.Domain.PasienContext.PasienFeature.IPasienKey pasien) => true;
}
