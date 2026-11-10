using Bilreg.Application.PaymentContext.DepositFeature;
using Bilreg.Application.PaymentContext.DepositFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.DepositFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.PaymentContext.DepositFeature;

public class DepositListHandlerTest
{
    private readonly Mock<IDepositRepo> _depositRepoMock = new();
    private readonly DepositListHandler _sut;

    public DepositListHandlerTest()
    {
        _sut = new DepositListHandler(_depositRepoMock.Object);
    }

    [Fact]
    public async Task Handle_GivenDeposits_ReturnsResponsesWithCorrectPaidStatus()
    {
        // Arrange
        const string regId = "REG001";
        var query = new DepositListQry(regId);

        var audit = new AuditTrailType(
            new AuditInfoType("USER01", "2026-10-22", "10:00:00"),
            new AuditInfoType("USER01", "2026-10-22", "10:00:00"),
            new AuditInfoType("", "3000-01-01", "00:00:00"));

        var orderDeposit = new DepositModel(
            depositId: "DP-ORDER",
            depositDate: new DateTime(2026, 10, 22, 10, 0, 0),
            reg: new RegReff(regId, "MR01", "Pasien A"),
            layanan: new LayananReff("LAY01", "Rawat Inap"),
            ketarangan: "Pesanan Deposit Baru",
            nilaiDeposit: 250_000m,
            audit: audit,
            noKwitansi: string.Empty,
            petugasKasir: string.Empty,
            totalBayar: 0m);

        var paidDeposit = new DepositModel(
            depositId: "DP-PAID",
            depositDate: new DateTime(2026, 10, 22, 11, 0, 0),
            reg: new RegReff(regId, "MR01", "Pasien A"),
            layanan: new LayananReff("LAY01", "Rawat Inap"),
            ketarangan: "Setoran Kasir Lunas",
            nilaiDeposit: 500_000m,
            audit: audit,
            noKwitansi: "KW-9999",
            petugasKasir: "KASIR01",
            totalBayar: 500_000m);

        _depositRepoMock
            .Setup(x => x.ListData(query))
            .Returns(new List<DepositModel> { orderDeposit, paidDeposit });

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(2);

        // Verify Order Deposit (In-Order / Menunggu Kasir)
        var orderResponse = result.First(x => x.DepositId == "DP-ORDER");
        orderResponse.IsPaid.Should().BeFalse();
        orderResponse.TotalBayar.Should().Be(0m);
        orderResponse.PetugasKasir.Should().BeEmpty();
        orderResponse.NoKwitansi.Should().BeEmpty();
        orderResponse.NilaiDeposit.Should().Be(250_000m);

        // Verify Paid Deposit (Terbayar Kasir)
        var paidResponse = result.First(x => x.DepositId == "DP-PAID");
        paidResponse.IsPaid.Should().BeTrue();
        paidResponse.TotalBayar.Should().Be(500_000m);
        paidResponse.PetugasKasir.Should().Be("KASIR01");
        paidResponse.NoKwitansi.Should().Be("KW-9999");
        paidResponse.NilaiDeposit.Should().Be(500_000m);

        _depositRepoMock.Verify(x => x.ListData(query), Times.Once);
    }

    [Fact]
    public async Task Handle_GivenNoDeposits_ReturnsEmptyList()
    {
        // Arrange
        var query = new DepositListQry("REG_EMPTY");
        _depositRepoMock
            .Setup(x => x.ListData(query))
            .Returns(new List<DepositModel>());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
