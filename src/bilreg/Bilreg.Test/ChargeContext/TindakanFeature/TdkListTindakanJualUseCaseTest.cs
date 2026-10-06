using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.ChargeContext.TindakanFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.ChargeContext.TindakanFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

// M03-F01 P3-S04 — carry batal marker on visit-scoped tindakan listing response
public class TdkListTindakanJualUseCaseTest
{
    private readonly Mock<ITindakanRepo> _tdkRepoMock = new();

    [Fact]
    public async Task Handle_WhenValidRegId_ReturnsListingWithBatalStatusPerItem()
    {
        // Arrange
        var regReff = new RegReff("REG001", "PAS001", "Pasien A");
        var lynReff = new LayananReff("LAY01", "Poli Penyakit Dalam");

        var activeItem = new TindakanJualView(
            TransaksiId: "TDK001",
            TransaksiDate: new DateTime(2026, 10, 3, 9, 30, 0),
            OrderTrasaksi: "ORD001",
            Reg: regReff,
            Layanan: lynReff,
            DiskripsiId: "TAR001",
            DiskripsiName: "Konsultasi Dokter",
            Qty: 1,
            Total: 150000m,
            Tipe: "TINDAKAN",
            IsBatal: false);

        var voidedItem = new TindakanJualView(
            TransaksiId: "TDK002",
            TransaksiDate: new DateTime(2026, 10, 3, 9, 45, 0),
            OrderTrasaksi: "ORD002",
            Reg: regReff,
            Layanan: lynReff,
            DiskripsiId: "TAR002",
            DiskripsiName: "EKG",
            Qty: 1,
            Total: 75000m,
            Tipe: "TINDAKAN",
            IsBatal: true);

        _tdkRepoMock
            .Setup(x => x.ListDataTdkJual(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns([activeItem, voidedItem]);

        var sut = new TdkListTindakanJualHandler(_tdkRepoMock.Object);

        // Act
        var result = (await sut.Handle(new TdkListTindakanJualQuery("REG001"), CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(2);

        var first = result[0];
        first.TransaksiId.Should().Be("TDK001");
        first.TransaksiDate.Should().Be("2026-10-03 09:30:00");
        first.TotalNilai.Should().Be(150000m);
        first.IsBatal.Should().BeFalse();

        var second = result[1];
        second.TransaksiId.Should().Be("TDK002");
        second.TransaksiDate.Should().Be("2026-10-03 09:45:00");
        second.TotalNilai.Should().Be(75000m);
        second.IsBatal.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WhenRegIdEmpty_ThrowsArgumentException(string? invalidRegId)
    {
        // Arrange
        var sut = new TdkListTindakanJualHandler(_tdkRepoMock.Object);

        // Act
        var act = () => sut.Handle(new TdkListTindakanJualQuery(invalidRegId!), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void TindakanJualDto_ToView_WhenNotVoided_SetsIsBatalFalse()
    {
        // Arrange: default VodDate is 3000-01-01 and empty VodUser
        var dto = new TindakanJualDto(
            TransaksiId: "TDK001",
            TransaksiDate: new DateTime(2026, 10, 3, 10, 0, 0),
            OrderTransaksiId: "ORD001",
            RegId: "REG001",
            PasienId: "PAS001",
            PasienName: "Pasien A",
            LayananId: "LAY01",
            LayananName: "Poli A",
            DiskripsiId: "TAR001",
            DiskripsiName: "Tindakan A",
            Qty: 1,
            Total: 100000m,
            Tipe: "TINDAKAN",
            CrtUser: "USR01",
            CrtDate: new DateTime(2026, 10, 3, 10, 0, 0),
            UpdUser: "",
            UpdDate: new DateTime(3000, 1, 1),
            VodUser: "",
            VodDate: new DateTime(3000, 1, 1));

        // Act
        var view = dto.ToView();

        // Assert
        view.IsBatal.Should().BeFalse();
        view.TransaksiId.Should().Be("TDK001");
        view.Total.Should().Be(100000m);
    }

    [Fact]
    public void TindakanJualDto_ToView_WhenVoidedByVodDate_SetsIsBatalTrue()
    {
        // Arrange: VodDate has an actual date earlier than 3000-01-01
        var dto = new TindakanJualDto(
            TransaksiId: "TDK002",
            TransaksiDate: new DateTime(2026, 10, 3, 10, 0, 0),
            OrderTransaksiId: "ORD002",
            RegId: "REG001",
            PasienId: "PAS001",
            PasienName: "Pasien A",
            LayananId: "LAY01",
            LayananName: "Poli A",
            DiskripsiId: "TAR002",
            DiskripsiName: "Tindakan B",
            Qty: 1,
            Total: 200000m,
            Tipe: "TINDAKAN",
            CrtUser: "USR01",
            CrtDate: new DateTime(2026, 10, 3, 10, 0, 0),
            UpdUser: "",
            UpdDate: new DateTime(3000, 1, 1),
            VodUser: "USR02",
            VodDate: new DateTime(2026, 10, 3, 11, 0, 0));

        // Act
        var view = dto.ToView();

        // Assert
        view.IsBatal.Should().BeTrue();
        view.TransaksiId.Should().Be("TDK002");
    }

    [Fact]
    public void TindakanJualDto_ToView_WhenVoidedByVodUser_SetsIsBatalTrue()
    {
        // Arrange: VodUser is set even if VodDate was default
        var dto = new TindakanJualDto(
            TransaksiId: "TDK003",
            TransaksiDate: new DateTime(2026, 10, 3, 10, 0, 0),
            OrderTransaksiId: "ORD003",
            RegId: "REG001",
            PasienId: "PAS001",
            PasienName: "Pasien A",
            LayananId: "LAY01",
            LayananName: "Poli A",
            DiskripsiId: "TAR003",
            DiskripsiName: "Tindakan C",
            Qty: 1,
            Total: 50000m,
            Tipe: "TINDAKAN",
            CrtUser: "USR01",
            CrtDate: new DateTime(2026, 10, 3, 10, 0, 0),
            UpdUser: "",
            UpdDate: new DateTime(3000, 1, 1),
            VodUser: "USR02",
            VodDate: new DateTime(3000, 1, 1));

        // Act
        var view = dto.ToView();

        // Assert
        view.IsBatal.Should().BeTrue();
    }
}
