using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;
using FluentAssertions;

namespace Bilreg.Test.PaymentContext.TrsBillingFeature;

public class TaRegistrasi3DtoTest
{
    [Fact]
    public void FromModel_MapsPaymentAndCoa()
    {
        var payment = new TataRekeningPaymentType(
            PaymentType.ByKas,
            50_000m,
            20_000m,
            new CoaType("REK-001", "Rekening Kas"));

        var dto = TaRegistrasi3Dto.FromModel("REG-001", payment);

        dto.fs_kd_reg.Should().Be("REG-001");
        dto.fs_kd_bayar.Should().Be("BYKAS");
        dto.fn_jasa.Should().Be(50_000m);
        dto.fn_obat.Should().Be(20_000m);
        dto.fs_kd_rek.Should().Be("REK-001");
    }

    [Fact]
    public void ToModel_RoundTripsCoaId()
    {
        var dto = new TaRegistrasi3Dto(
            "REG-001",
            "BYKAS",
            "Bayar Pribadi",
            50_000m,
            20_000m,
            "REK-001",
            false);

        var model = dto.ToModel();

        model.Payment.PaymentId.Should().Be("BYKAS");
        model.NilaiJasa.Should().Be(50_000m);
        model.NilaiObat.Should().Be(20_000m);
        model.Coa.CoaId.Should().Be("REK-001");
    }

    [Fact]
    public void ToModel_WhenCoaEmpty_UsesDefault()
    {
        var dto = new TaRegistrasi3Dto("REG-001", "BYPRI", "", 10_000m, 0m, "", false);

        var model = dto.ToModel();

        model.Coa.Should().Be(CoaType.Default);
        model.Payment.PaymentName.Should().Be(PaymentType.ByPri.PaymentName);
    }
}
