using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;

namespace Bilreg.Test.AdmisiRanapContext.ReservationFeature;

public class ReservationModelTest
{
    private static PasienReff SamplePasien() =>
        new("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L");

    private static KelasReff SampleKelas() => new("K1", "Kelas 1");

    private static BangsalReff SampleBangsal() => new("B1", "Bangsal A");

    [Fact]
    public void DT_RS_01_GivenElectivePath_WhenCreate_ThenReserved()
    {
        var reservation = ReservationModel.Create(
            SamplePasien(),
            new DateTime(2026, 8, 1),
            SampleKelas(),
            SampleBangsal(),
            "user1");

        reservation.ReservationStatus.Should().Be(ReservationStatusEnum.Reserved);
        reservation.ReservationId.Should().StartWith("RSV");
    }

    [Fact]
    public void GivenReserved_WhenMaintain_ThenMaintained()
    {
        var reservation = ReservationModel.Create(
            SamplePasien(),
            new DateTime(2026, 8, 1),
            SampleKelas(),
            SampleBangsal(),
            "user1");

        var maintained = reservation.Maintain(
            new DateTime(2026, 8, 5),
            new KelasReff("K2", "Kelas 2"),
            new BangsalReff("B2", "Bangsal B"),
            "user1");

        maintained.ReservationStatus.Should().Be(ReservationStatusEnum.Maintained);
        maintained.KelasRawat.KelasId.Should().Be("K2");
    }

    [Fact]
    public void GivenMaintained_WhenRealize_ThenRealized()
    {
        var maintained = ReservationModel.Create(
                SamplePasien(),
                new DateTime(2026, 8, 1),
                SampleKelas(),
                SampleBangsal(),
                "user1")
            .Maintain(new DateTime(2026, 8, 1), SampleKelas(), SampleBangsal(), "user1");

        var realized = maintained.Realize("RG00000001", "user1");

        realized.ReservationStatus.Should().Be(ReservationStatusEnum.Realized);
        realized.RealizedRegId.Should().Be("RG00000001");
    }

    [Fact]
    public void GivenRealized_WhenRealizeAgain_ThenThrows()
    {
        var realized = ReservationModel.Create(
                SamplePasien(),
                new DateTime(2026, 8, 1),
                SampleKelas(),
                SampleBangsal(),
                "user1")
            .Maintain(new DateTime(2026, 8, 1), SampleKelas(), SampleBangsal(), "user1")
            .Realize("RG00000001", "user1");

        var act = () => realized.Realize("RG00000002", "user1");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GivenReserved_WhenCancel_ThenCancelled()
    {
        var reservation = ReservationModel.Create(
            SamplePasien(),
            new DateTime(2026, 8, 1),
            SampleKelas(),
            SampleBangsal(),
            "user1");

        var cancelled = reservation.Cancel("user1");

        cancelled.ReservationStatus.Should().Be(ReservationStatusEnum.Cancelled);
    }
}
