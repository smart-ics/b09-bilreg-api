using Bilreg.Application.RadContext.RadJadwalFeature;
using Bilreg.Application.RadContext.RadOrderFeature;
using Bilreg.Domain.RadContext.RadJadwalFeature;
using Bilreg.Domain.RadContext.RadOrderFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.RadContext.RadJadwalFeature;

public class RadJadwalQueriesTest
{
    public class RadJadwalGetHandlerTest
    {
        private readonly Mock<IRadJadwalDal> _jadwalDalMock = new();
        private readonly RadJadwalGetHandler _sut;

        public RadJadwalGetHandlerTest()
        {
            _sut = new RadJadwalGetHandler(_jadwalDalMock.Object);
        }

        [Fact]
        public async Task Handle_WhenFound_ReturnsHeaderAndOrderedRescheduleHistory()
        {
            var header = new RadJadwalDto() with
            {
                JadwalRadId = "JAD0000000001",
                ScheduleNo = "SCH26110001",
                OrderId = "RDO0000000001",
                Status = (int)RadJadwalStatusEnum.Scheduled
            };

            var history = new List<RadJadwalRescheduleDto>
            {
                new() { RescheduleId = "RSC02", JadwalRadId = "JAD0000000001", RescheduleIndex = 2, Reason = "Geser 2" },
                new() { RescheduleId = "RSC01", JadwalRadId = "JAD0000000001", RescheduleIndex = 1, Reason = "Geser 1" }
            };

            _jadwalDalMock.Setup(x => x.GetData(It.Is<IRadJadwalKey>(k => k.JadwalRadId == "JAD0000000001")))
                .Returns(header);
            _jadwalDalMock.Setup(x => x.ListRescheduleHistory("JAD0000000001"))
                .Returns(history);

            var result = await _sut.Handle(new RadJadwalGetQuery("JAD0000000001"), CancellationToken.None);

            result.Should().NotBeNull();
            result!.Header.Should().Be(header);
            result.RescheduleHistory.Select(x => x.RescheduleIndex).Should().Equal(1, 2);
        }

        [Fact]
        public async Task Handle_WhenNotFound_ReturnsNull()
        {
            _jadwalDalMock.Setup(x => x.GetData(It.IsAny<IRadJadwalKey>()))
                .Returns((RadJadwalDto?)null);

            var result = await _sut.Handle(new RadJadwalGetQuery("JAD-404"), CancellationToken.None);

            result.Should().BeNull();
            _jadwalDalMock.Verify(x => x.ListRescheduleHistory(It.IsAny<string>()), Times.Never);
        }
    }

    public class RadJadwalWorklistHandlerTest
    {
        private readonly Mock<IRadJadwalDal> _jadwalDalMock = new();
        private readonly Mock<IRadOrderDal> _orderDalMock = new();
        private readonly RadJadwalWorklistHandler _sut;

        public RadJadwalWorklistHandlerTest()
        {
            _sut = new RadJadwalWorklistHandler(_jadwalDalMock.Object, _orderDalMock.Object);
        }

        [Fact]
        public async Task Handle_StatusNull_ReturnsOrderedSchedulesAndDeduplicatedPendingOrders()
        {
            var day1 = new DateTime(2026, 11, 26);
            var day2 = new DateTime(2026, 11, 27);

            var schedules = new List<RadJadwalDto>
            {
                new() { JadwalRadId = "JAD02", ScheduleDate = day2, StartTime = new TimeSpan(9, 0, 0) },
                new() { JadwalRadId = "JAD01", ScheduleDate = day1, StartTime = new TimeSpan(8, 0, 0) }
            };

            _jadwalDalMock
                .Setup(d => d.ListByDateRange(day1, day2, "CT", null, null))
                .Returns(schedules);

            var order1 = new RadOrderWorklistView("RDO01", "RAD01", day1, "CT", 1, 0, "MR01", "Pasien 1", "DR01", "Poli", 1, false);
            var order2 = new RadOrderWorklistView("RDO02", "RAD02", day2, "CT", 1, 0, "MR02", "Pasien 2", "DR01", "Poli", 1, false);

            _orderDalMock.Setup(d => d.GetWorklist(day1, "CT", (int)RadOrderStatusEnum.Requested))
                .Returns([order1]);
            _orderDalMock.Setup(d => d.GetWorklist(day2, "CT", (int)RadOrderStatusEnum.Requested))
                .Returns([order1, order2]);

            var result = await _sut.Handle(new RadJadwalWorklistQuery(day1, day2, "CT", null), CancellationToken.None);

            result.Schedules.Select(s => s.JadwalRadId).Should().Equal("JAD01", "JAD02");
            result.PendingOrders.Select(o => o.OrderId).Should().Equal("RDO01", "RDO02");
        }

        [Fact]
        public async Task Handle_StatusCancelled_SkipsPendingOrdersLookup()
        {
            var day = new DateTime(2026, 11, 26);
            _jadwalDalMock
                .Setup(d => d.ListByDateRange(day, day, null, (int)RadJadwalStatusEnum.Cancelled, null))
                .Returns([]);

            var result = await _sut.Handle(
                new RadJadwalWorklistQuery(day, day, null, (int)RadJadwalStatusEnum.Cancelled),
                CancellationToken.None);

            result.PendingOrders.Should().BeEmpty();
            _orderDalMock.Verify(d => d.GetWorklist(It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task Handle_EndDateBeforeStartDate_ThrowsArgumentException()
        {
            var query = new RadJadwalWorklistQuery(new DateTime(2026, 11, 27), new DateTime(2026, 11, 26));

            var act = () => _sut.Handle(query, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }
    }

    public class RadJadwalCalendarTimelineHandlerTest
    {
        private readonly Mock<IRadJadwalDal> _jadwalDalMock = new();
        private readonly Mock<IRadEquipmentDal> _equipmentDalMock = new();
        private readonly RadJadwalCalendarTimelineHandler _sut;

        public RadJadwalCalendarTimelineHandlerTest()
        {
            _sut = new RadJadwalCalendarTimelineHandler(_jadwalDalMock.Object, _equipmentDalMock.Object);
        }

        [Fact]
        public async Task Handle_GroupsSlotsByEquipmentAndPreservesSlotsForInactiveEquipment()
        {
            var date = new DateTime(2026, 11, 27);
            _equipmentDalMock.Setup(e => e.ListByModality("CT", true))
                .Returns([
                    new RadEquipmentDto("EQ-CT01", "CT 64", "CT", "RM01", "Ruang CT 1", true)
                ]);

            var slotActive = new RadJadwalCalendarSlotDto() with
            {
                JadwalRadId = "JAD01",
                ScheduleNo = "SCH01",
                EquipmentId = "EQ-CT01",
                EquipmentName = "CT 64",
                Modality = "CT",
                ScheduleDate = date,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(9, 30, 0)
            };

            var slotInactive = new RadJadwalCalendarSlotDto() with
            {
                JadwalRadId = "JAD02",
                ScheduleNo = "SCH02",
                EquipmentId = "EQ-CT-OLD",
                EquipmentName = "CT Legacy",
                Modality = "CT",
                ScheduleDate = date,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(10, 30, 0)
            };

            _jadwalDalMock.Setup(d => d.ListCalendarSlots(date, "CT", null))
                .Returns([slotActive, slotInactive]);

            var result = await _sut.Handle(new RadJadwalCalendarTimelineQuery(date, "CT"), CancellationToken.None);

            result.ScheduleDate.Should().Be(date);
            result.Modality.Should().Be("CT");
            result.Equipments.Should().HaveCount(2);
            result.Equipments[0].EquipmentId.Should().Be("EQ-CT01");
            result.Equipments[0].IsActive.Should().BeTrue();
            result.Equipments[0].Slots.Should().ContainSingle(s => s.JadwalRadId == "JAD01");
            result.Equipments[1].EquipmentId.Should().Be("EQ-CT-OLD");
            result.Equipments[1].IsActive.Should().BeFalse();
            result.Equipments[1].Slots.Should().ContainSingle(s => s.JadwalRadId == "JAD02");
        }
    }

    public class RadEquipmentLookupHandlerTest
    {
        private readonly Mock<IRadEquipmentDal> _equipmentDalMock = new();
        private readonly RadEquipmentLookupHandler _sut;

        public RadEquipmentLookupHandlerTest()
        {
            _sut = new RadEquipmentLookupHandler(_equipmentDalMock.Object);
        }

        [Fact]
        public async Task Handle_WithModality_CallsListByModality()
        {
            var expected = new List<RadEquipmentDto>
            {
                new("EQ-MRI01", "MRI 1.5T", "MRI", "RM-MRI01", "Ruang MRI", true)
            };
            _equipmentDalMock.Setup(e => e.ListByModality("MR", true)).Returns(expected);

            var result = await _sut.Handle(new RadEquipmentLookupQuery("MR", true), CancellationToken.None);

            result.Should().BeEquivalentTo(expected);
            _equipmentDalMock.Verify(e => e.ListByModality("MR", true), Times.Once);
        }

        [Fact]
        public async Task Handle_WithoutModality_CallsListActiveOrListAll()
        {
            _equipmentDalMock.Setup(e => e.ListActive()).Returns([]);
            _equipmentDalMock.Setup(e => e.ListAll()).Returns([]);

            await _sut.Handle(new RadEquipmentLookupQuery(null, ActiveOnly: true), CancellationToken.None);
            await _sut.Handle(new RadEquipmentLookupQuery(null, ActiveOnly: false), CancellationToken.None);

            _equipmentDalMock.Verify(e => e.ListActive(), Times.Once);
            _equipmentDalMock.Verify(e => e.ListAll(), Times.Once);
        }
    }
}
