using Bilreg.Application.PaymentContext.KasirFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.UseCases;

public class GetAntreanOrderKasirQueryTest
{
    private readonly Mock<IKasirOrderInboxDal> _inboxDalMock;
    private readonly Mock<ITglJamProvider> _tglJamProviderMock;
    private readonly GetAntreanOrderKasirHandler _sut;
    private readonly GetAntreanOrderKasirValidator _validator;

    public GetAntreanOrderKasirQueryTest()
    {
        _inboxDalMock = new Mock<IKasirOrderInboxDal>();
        _tglJamProviderMock = new Mock<ITglJamProvider>();
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 11, 10, 8, 0, 0));
        _sut = new GetAntreanOrderKasirHandler(_inboxDalMock.Object, _tglJamProviderMock.Object);
        _validator = new GetAntreanOrderKasirValidator();
    }

    [Fact]
    public async Task GivenNoKeyword_WhenHandle_ThenReturnsAllInboxItems()
    {
        // Arrange
        var items = new List<OrderKasirItemDto>
        {
            new("BL260001", "PENERIMAAN", "Order Pembayaran Tagihan", "RG001", "RM001", "Budi Santoso", 150_000m, "MENUNGGU_BAYAR", "2026-11-07"),
            new("DP260001", "PENERIMAAN", "Order Pembayaran Deposit", "RG002", "RM002", "Siti Rahma", 500_000m, "MENUNGGU_BAYAR", "2026-11-07"),
            new("BL260002", "PENGELUARAN", "Pengembalian Kelebihan Bayar / Refund", "RG003", "RM003", "Ahmad Fauzi", 50_000m, "MENUNGGU_PENGELUARAN", "2026-11-07")
        };
        _inboxDalMock.Setup(x => x.ListOrderInboxAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var query = new GetAntreanOrderKasirQuery();

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].OrderId.Should().Be("BL260001");
        result[1].OrderId.Should().Be("DP260001");
        result[2].OrderId.Should().Be("BL260002");
    }

    [Fact]
    public async Task GivenKeywordMatchingOrderId_WhenHandle_ThenReturnsFilteredItems()
    {
        // Arrange
        var items = new List<OrderKasirItemDto>
        {
            new("BL260001", "PENERIMAAN", "Order Pembayaran Tagihan", "RG001", "RM001", "Budi Santoso", 150_000m, "MENUNGGU_BAYAR", "2026-11-07"),
            new("DP260001", "PENERIMAAN", "Order Pembayaran Deposit", "RG002", "RM002", "Siti Rahma", 500_000m, "MENUNGGU_BAYAR", "2026-11-07")
        };
        _inboxDalMock.Setup(x => x.ListOrderInboxAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var query = new GetAntreanOrderKasirQuery("DP26");

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].OrderId.Should().Be("DP260001");
        result[0].PasienName.Should().Be("Siti Rahma");
    }

    [Fact]
    public async Task GivenKeywordMatchingPasienName_WhenHandle_ThenReturnsFilteredItems()
    {
        // Arrange
        var items = new List<OrderKasirItemDto>
        {
            new("BL260001", "PENERIMAAN", "Order Pembayaran Tagihan", "RG001", "RM001", "Budi Santoso", 150_000m, "MENUNGGU_BAYAR", "2026-11-07"),
            new("DP260001", "PENERIMAAN", "Order Pembayaran Deposit", "RG002", "RM002", "Siti Rahma", 500_000m, "MENUNGGU_BAYAR", "2026-11-07")
        };
        _inboxDalMock.Setup(x => x.ListOrderInboxAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var query = new GetAntreanOrderKasirQuery("Budi");

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].PasienName.Should().Be("Budi Santoso");
    }

    [Fact]
    public async Task GivenKeywordMatchingUnitName_WhenHandle_ThenReturnsFilteredItems()
    {
        // Arrange
        var items = new List<OrderKasirItemDto>
        {
            new("BL260001", "PENERIMAAN", "Order Pembayaran Tagihan", "RG001", "RM001", "Budi Santoso", 150_000m, "MENUNGGU_BAYAR", "2026-11-07", "POLIKLINIK PENYAKIT DALAM", "BPJS KESEHATAN"),
            new("DP260001", "PENERIMAAN", "Order Pembayaran Deposit", "RG002", "RM002", "Siti Rahma", 500_000m, "MENUNGGU_BAYAR", "2026-11-07", "IGD UMUM", "UMUM/MANDIRI")
        };
        _inboxDalMock.Setup(x => x.ListOrderInboxAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var query = new GetAntreanOrderKasirQuery("Penyakit Dalam");

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].OrderId.Should().Be("BL260001");
        result[0].UnitName.Should().Be("POLIKLINIK PENYAKIT DALAM");
    }

    [Fact]
    public async Task GivenKeywordMatchingPenjaminName_WhenHandle_ThenReturnsFilteredItems()
    {
        // Arrange
        var items = new List<OrderKasirItemDto>
        {
            new("BL260001", "PENERIMAAN", "Order Pembayaran Tagihan", "RG001", "RM001", "Budi Santoso", 150_000m, "MENUNGGU_BAYAR", "2026-11-07", "POLIKLINIK PENYAKIT DALAM", "BPJS KESEHATAN"),
            new("DP260001", "PENERIMAAN", "Order Pembayaran Deposit", "RG002", "RM002", "Siti Rahma", 500_000m, "MENUNGGU_BAYAR", "2026-11-07", "IGD UMUM", "UMUM/MANDIRI")
        };
        _inboxDalMock.Setup(x => x.ListOrderInboxAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var query = new GetAntreanOrderKasirQuery("BPJS");

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].OrderId.Should().Be("BL260001");
        result[0].PenjaminName.Should().Be("BPJS KESEHATAN");
    }

    [Fact]
    public async Task GivenKeywordNotMatching_WhenHandle_ThenReturnsEmpty()
    {
        // Arrange
        var items = new List<OrderKasirItemDto>
        {
            new("BL260001", "PENERIMAAN", "Order Pembayaran Tagihan", "RG001", "RM001", "Budi Santoso", 150_000m, "MENUNGGU_BAYAR", "2026-11-07")
        };
        _inboxDalMock.Setup(x => x.ListOrderInboxAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var query = new GetAntreanOrderKasirQuery("NonExistent");

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenNoDate_WhenHandle_ThenUsesTglJamProviderDate()
    {
        // Arrange
        _inboxDalMock.Setup(x => x.ListOrderInboxAsync("2026-11-10", It.IsAny<CancellationToken>())).ReturnsAsync(new List<OrderKasirItemDto>());

        var query = new GetAntreanOrderKasirQuery();

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        _inboxDalMock.Verify(x => x.ListOrderInboxAsync("2026-11-10", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenExplicitDate_WhenHandle_ThenUsesExplicitDate()
    {
        // Arrange
        var items = new List<OrderKasirItemDto>
        {
            new("BL260001", "PENERIMAAN", "Order Pembayaran Tagihan", "RG001", "RM001", "Budi Santoso", 150_000m, "MENUNGGU_BAYAR", "2026-11-05")
        };
        _inboxDalMock.Setup(x => x.ListOrderInboxAsync("2026-11-05", It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var query = new GetAntreanOrderKasirQuery(Date: "2026-11-05");

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        _inboxDalMock.Verify(x => x.ListOrderInboxAsync("2026-11-05", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenCancellationToken_WhenHandle_ThenForwardsCancellationTokenToDal()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        _inboxDalMock
            .Setup(x => x.ListOrderInboxAsync("2026-11-10", token))
            .ReturnsAsync(new List<OrderKasirItemDto>());

        var query = new GetAntreanOrderKasirQuery();

        // Act
        await _sut.Handle(query, token);

        // Assert
        _inboxDalMock.Verify(x => x.ListOrderInboxAsync("2026-11-10", token), Times.Once);
    }

    [Fact]
    public async Task GivenCancelledToken_WhenDalThrowsOperationCanceled_ThenPropagatesException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var token = cts.Token;

        _inboxDalMock
            .Setup(x => x.ListOrderInboxAsync("2026-11-10", token))
            .ThrowsAsync(new OperationCanceledException(token));

        var query = new GetAntreanOrderKasirQuery();

        // Act
        var act = () => _sut.Handle(query, token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GivenDalReturnsNull_WhenHandle_ThenReturnsEmpty()
    {
        // Arrange
        _inboxDalMock
            .Setup(x => x.ListOrderInboxAsync("2026-11-10", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<OrderKasirItemDto>)null!);

        var query = new GetAntreanOrderKasirQuery();

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenNullQuery_WhenHandle_ThenThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void GivenValidQuery_WhenValidate_ThenPasses()
    {
        var query = new GetAntreanOrderKasirQuery("Test");
        var result = _validator.Validate(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GivenOverlyLongKeyword_WhenValidate_ThenFails()
    {
        var query = new GetAntreanOrderKasirQuery(new string('A', 101));
        var result = _validator.Validate(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Keyword");
    }
}
