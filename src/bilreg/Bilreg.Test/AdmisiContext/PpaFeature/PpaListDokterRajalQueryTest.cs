//using Bilreg.Application.AdmisiContext.LayananFeature;
//using Bilreg.Application.AdmisiContext.PpaFeature;
//using Bilreg.Application.AdmisiContext.PpaFeature.UseCases;
//using Bilreg.Domain.AdmisiContext.LayananFeature;
//using Bilreg.Domain.AdmisiContext.PpaFeature;
//using FluentAssertions;
//using Moq;

//namespace Bilreg.Test.AdmisiContext.PpaFeature;


//public class PpaListDokterPerGroupSpesialisHandlerTests
//{
//    private readonly Mock<IPpaRepo> _ppaRepo;
//    private readonly Mock<IGroupSpesialisRepo> _groupRepo;
//    private readonly Mock<ILayananRepo> _layananRepo;
//    private readonly PpaListDokterRajalHandler _handler;

//    public PpaListDokterPerGroupSpesialisHandlerTests()
//    {
//        _ppaRepo = new Mock<IPpaRepo>();
//        _groupRepo = new Mock<IGroupSpesialisRepo>();
//        _layananRepo = new Mock<ILayananRepo>();

//        _handler = new PpaListDokterRajalHandler(
//            _ppaRepo.Object,
//            _groupRepo.Object,
//            _layananRepo.Object);
//    }

//    // ================================================================
//    // Helpers (to construct full domain models)
//    // ================================================================

//    private static GroupSpesialisType GS(string id, string name)
//        => new(id, name);

//    private static LayananType MakeLayanan(string id, string name, GroupSpesialisType gs)
//        => new(
//            id, name, true,
//            InstalasiType.Default.ToReff(),
//            LayananDkType.Default.ToReff(),
//            TipeLayananDkType.Default,
//            InstalasiDkType.RawatJalan,
//            gs
//        );

//    private static PpaLayananView MakePpa(
//        string dokterId,
//        string dokterName,
//        LayananType layanan)
//        => new(
//            dokterId,
//            dokterName,
//            layanan.ToReff()
//        );

//    // ================================================================
//    // TEST 1 — Happy path
//    // ================================================================

//    [Fact]
//    public async Task Given_ValidData_When_HandlerInvoked_Then_ShouldReturnCorrectGrouping()
//    {
//        // ARRANGE
//        var gs1 = GS("GS1", "Surgery");
//        var gs2 = GS("GS2", "Internal");

//        var layanan1 = MakeLayanan("L1", "General Surgery", gs1);
//        var layanan2 = MakeLayanan("L2", "Orthopedic", gs1);
//        var layanan3 = MakeLayanan("L3", "Cardiology", gs2);

//        var dokter1 = MakePpa("D1", "Dr One", layanan1);
//        var dokter2 = MakePpa("D2", "Dr Two", layanan2);
//        var dokter3 = MakePpa("D3", "Dr Three", layanan3);

//        _layananRepo
//            .Setup(x => x.ListData(InstalasiDkType.RawatJalan))
//            .Returns([layanan1, layanan2, layanan3]);

//        _ppaRepo
//            .Setup(x => x.ListData(ProfesiType.Dokter, It.IsAny<IEnumerable<ILayananKey>>()))
//            .Returns([dokter1, dokter2, dokter3]);

//        _groupRepo
//            .Setup(x => x.ListData())
//            .Returns([gs1, gs2]);

//        var query = new PpaListDokterRajalQuery();

//        // ACT
//        var result = (await _handler.Handle(query, CancellationToken.None)).ToList();

//        // ASSERT
//        result.Should().HaveCount(2);

//        // Surgery group
//        var surgery = result.First(x => x.GroupSpesialisId == "GS1");
//        surgery.ListDokter.Should().HaveCount(2);
//        surgery.ListDokter.Select(x => x.DokterId).Should().Contain(new[] { "D1", "D2" });

//        // Internal group
//        var internalMed = result.First(x => x.GroupSpesialisId == "GS2");
//        internalMed.ListDokter.Should().HaveCount(1);
//        internalMed.ListDokter.First().DokterId.Should().Be("D3");
//    }

//    // ================================================================
//    // TEST 2 — No layanan available
//    // ================================================================

//    [Fact]
//    public async Task Given_NoLayanan_When_HandlerInvoked_Then_ShouldReturnEmptyDoctorLists()
//    {
//        // ARRANGE
//        var gs = GS("GS1", "Surgery");

//        _layananRepo
//            .Setup(x => x.ListData(InstalasiDkType.RawatJalan))
//            .Returns(new List<LayananType>());

//        _ppaRepo
//            .Setup(x => x.ListData(ProfesiType.Dokter, It.IsAny<IEnumerable<ILayananKey>>()))
//            .Returns(new List<PpaLayananView>());

//        _groupRepo
//            .Setup(x => x.ListData())
//            .Returns(new[] { gs });

//        var query = new PpaListDokterRajalQuery();

//        // ACT
//        var result = (await _handler.Handle(query, CancellationToken.None)).ToList();

//        // ASSERT
//        result.Should().HaveCount(1);
//        result[0].ListDokter.Should().BeEmpty();
//    }

//    // ================================================================
//    // TEST 3 — No GroupSpesialis returned
//    // ================================================================

//    [Fact]
//    public async Task Given_NoGroupSpesialis_When_HandlerInvoked_Then_ShouldReturnEmptyResult()
//    {
//        // ARRANGE
//        _layananRepo
//            .Setup(x => x.ListData(InstalasiDkType.RawatJalan))
//            .Returns(new List<LayananType>());

//        _ppaRepo
//            .Setup(x => x.ListData(ProfesiType.Dokter, It.IsAny<IEnumerable<ILayananKey>>()))
//            .Returns(new List<PpaLayananView>());

//        _groupRepo
//            .Setup(x => x.ListData())
//            .Returns(new List<GroupSpesialisType>());

//        var query = new PpaListDokterRajalQuery();

//        // ACT
//        var result = await _handler.Handle(query, CancellationToken.None);

//        // ASSERT
//        result.Should().BeEmpty();
//    }
//}

