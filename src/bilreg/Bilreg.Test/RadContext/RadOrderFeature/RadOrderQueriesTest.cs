using Bilreg.Application.RadContext.RadOrderFeature;
using Bilreg.Application.RadContext.RadOrderFeature.UseCases;
using Bilreg.Domain.RadContext.RadOrderFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.RadContext.RadOrderFeature;

public class RadOrderQueriesTest
{
    public class RadOrderGetHandlerTest
    {
        private readonly Mock<IRadOrderDal> _orderDalMock = new();
        private readonly Mock<IRadOrderItemDal> _itemDalMock = new();
        private readonly RadOrderGetHandler _sut;

        public RadOrderGetHandlerTest()
        {
            _sut = new RadOrderGetHandler(_orderDalMock.Object, _itemDalMock.Object);
        }

        [Fact]
        public async Task Handle_OrderFound_ReturnsDetailWithHeaderAndItems()
        {
            // Arrange
            var header = new RadOrderDto
            {
                OrderId = "RDO2611000001",
                OrderNo = "RAD26110001",
                Modality = "CR",
                RadOrderStatus = (int)RadOrderStatusEnum.Requested,
                RegId = "REG001",
                PatientName = "Pasien Uji"
            };

            var items = new List<RadOrderItemDto>
            {
                new("RDO2611000001", 1, "TRF01", "Thorax AP", "CR", (int)RadUrgencyEnum.Rutin),
                new("RDO2611000001", 2, "TRF02", "Thorax Lateral", "CR", (int)RadUrgencyEnum.Rutin)
            };

            _orderDalMock.Setup(x => x.GetData(It.Is<IRadOrderKey>(k => k.OrderId == "RDO2611000001")))
                .Returns(header);
            _itemDalMock.Setup(x => x.ListData(It.Is<IRadOrderKey>(k => k.OrderId == "RDO2611000001")))
                .Returns(items);

            var query = new RadOrderGetQuery("RDO2611000001");

            // Act
            var result = await _sut.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.Header.Should().Be(header);
            result.Items.Should().BeEquivalentTo(items);
        }

        [Fact]
        public async Task Handle_OrderNotFound_ReturnsNull()
        {
            // Arrange
            _orderDalMock.Setup(x => x.GetData(It.IsAny<IRadOrderKey>()))
                .Returns((RadOrderDto?)null);

            var query = new RadOrderGetQuery("RDO-NONEXISTENT");

            // Act
            var result = await _sut.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeNull();
            _itemDalMock.Verify(x => x.ListData(It.IsAny<IRadOrderKey>()), Times.Never);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Handle_InvalidOrderId_ThrowsArgumentException(string? invalidOrderId)
        {
            // Arrange & Act
            var act = () => _sut.Handle(new RadOrderGetQuery(invalidOrderId!), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task Handle_NullRequest_ThrowsArgumentNullException()
        {
            // Arrange & Act
            var act = () => _sut.Handle(null!, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }
    }

    public class RadOrderWorklistHandlerTest
    {
        private readonly Mock<IRadOrderDal> _orderDalMock = new();
        private readonly RadOrderWorklistHandler _sut;

        public RadOrderWorklistHandlerTest()
        {
            _sut = new RadOrderWorklistHandler(_orderDalMock.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_CallsGetWorklist_AndReturnsResult()
        {
            // Arrange
            var testDate = new DateTime(2026, 11, 24);
            var modality = "CR";
            var status = (int)RadOrderStatusEnum.Requested;

            var worklist = new List<RadOrderWorklistView>
            {
                new("RDO001", "RAD001", testDate, "CR", 1, 1, "MR01", "Pasien A", "dr. Budi", "Poli Penyakit Dalam", 2, true),
                new("RDO002", "RAD002", testDate, "CR", 1, 0, "MR02", "Pasien B", "dr. Siti", "Poli Bedah", 1, false)
            };

            _orderDalMock.Setup(x => x.GetWorklist(testDate, modality, status))
                .Returns(worklist);

            var query = new RadOrderWorklistQuery(testDate, modality, status);

            // Act
            var result = await _sut.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(worklist);
            _orderDalMock.Verify(x => x.GetWorklist(testDate, modality, status), Times.Once);
        }

        [Fact]
        public async Task Handle_RequestWithoutStatus_PassesNullStatusToDal()
        {
            // Arrange
            var testDate = new DateTime(2026, 11, 24);
            var modality = "CT";

            var worklist = new List<RadOrderWorklistView>
            {
                new("RDO003", "RAD003", testDate, "CT", 2, 0, "MR03", "Pasien C", "dr. Budi", "IGD", 1, false)
            };

            _orderDalMock.Setup(x => x.GetWorklist(testDate, modality, null))
                .Returns(worklist);

            var query = new RadOrderWorklistQuery(testDate, modality, null);

            // Act
            var result = await _sut.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(worklist);
            _orderDalMock.Verify(x => x.GetWorklist(testDate, modality, null), Times.Once);
        }

        [Fact]
        public async Task Handle_NullRequest_ThrowsArgumentNullException()
        {
            // Arrange & Act
            var act = () => _sut.Handle(null!, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }
    }

    public class RadTarifModalityLookupHandlerTest
    {
        private readonly Mock<IRadTarifModalityDal> _dalMock = new();
        private readonly RadTarifModalityLookupHandler _sut;

        public RadTarifModalityLookupHandlerTest()
        {
            _sut = new RadTarifModalityLookupHandler(_dalMock.Object);
        }

        [Fact]
        public async Task Handle_ModalitySpecified_CallsListByModality_AndReturnsResult()
        {
            // Arrange
            var items = new List<RadTarifModalityDto>
            {
                new("TRF_CT01", "CT Scan Kepala", "CT", true, "SYS", DateTime.Now, "SYS", DateTime.Now),
                new("TRF_CT02", "CT Scan Abdomen", "CT", true, "SYS", DateTime.Now, "SYS", DateTime.Now)
            };

            _dalMock.Setup(x => x.ListByModality("CT"))
                .Returns(items);

            var query = new RadTarifModalityLookupQuery("CT");

            // Act
            var result = await _sut.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(items);
            _dalMock.Verify(x => x.ListByModality("CT"), Times.Once);
            _dalMock.Verify(x => x.ListActive(), Times.Never);
        }

        [Fact]
        public async Task Handle_ModalityNull_CallsListActive_AndReturnsResult()
        {
            // Arrange
            var allActive = new List<RadTarifModalityDto>
            {
                new("TRF_CR01", "Thorax AP", "CR", true, "SYS", DateTime.Now, "SYS", DateTime.Now),
                new("TRF_CT01", "CT Scan Kepala", "CT", true, "SYS", DateTime.Now, "SYS", DateTime.Now)
            };

            _dalMock.Setup(x => x.ListActive())
                .Returns(allActive);

            var query = new RadTarifModalityLookupQuery(null);

            // Act
            var result = await _sut.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(allActive);
            _dalMock.Verify(x => x.ListActive(), Times.Once);
            _dalMock.Verify(x => x.ListByModality(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ModalityWhitespace_CallsListActive_AndReturnsResult()
        {
            // Arrange
            var allActive = new List<RadTarifModalityDto>
            {
                new("TRF_CR01", "Thorax AP", "CR", true, "SYS", DateTime.Now, "SYS", DateTime.Now)
            };

            _dalMock.Setup(x => x.ListActive())
                .Returns(allActive);

            var query = new RadTarifModalityLookupQuery("   ");

            // Act
            var result = await _sut.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(allActive);
            _dalMock.Verify(x => x.ListActive(), Times.Once);
            _dalMock.Verify(x => x.ListByModality(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_NullRequest_ThrowsArgumentNullException()
        {
            // Arrange & Act
            var act = () => _sut.Handle(null!, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }
    }
}
