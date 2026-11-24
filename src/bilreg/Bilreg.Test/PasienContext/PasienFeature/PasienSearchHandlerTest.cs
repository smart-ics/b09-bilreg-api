using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Application.Shared.Helpers;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.PasienContext.PasienFeature;

public class PasienSearchHandlerTest
{
    private readonly Mock<IPasienRepo> _pasienRepoMock = new();
    private readonly Mock<IPolisRepo> _polisRepoMock = new();
    private readonly Mock<IRegRepo> _regRepoMock = new();

    private PasienSearchHandler CreateHandler() =>
        new(
            _pasienRepoMock.Object,
            _polisRepoMock.Object,
            _regRepoMock.Object,
            _pasienRepoMock.Object
        );

    private static PersonInfoType CreateSamplePerson(string name = "Budi Santoso") =>
        new(
            name,
            new DateOnly(1990, 5, 20),
            "L",
            AlamatType.Default,
            new ContactType(JenisContactEnum.Mobile, "081234567890"),
            IdentitasType.Default
        );

    private static PasienModel CreateSamplePasien(string pasienId = "00100123456", bool isAktif = true) =>
        new(
            pasienId: pasienId,
            person: CreateSamplePerson(),
            nickName: "Budi",
            tempatLahir: "Surabaya",
            golDarah: GolDarahType.Default,
            namaIbuKandung: "Siti Aminah",
            ktp: KtpType.Default,
            kelurahan: KelurahanType.Default,
            kartuKeluarga: IdentitasType.Default,
            listContact: new List<ContactType>(),
            keluarga: PasienKeluargaType.Default,
            agama: AgamaType.Default,
            suku: SukuType.Default,
            statusKawinDk: StatusKawinDkType.Default,
            pendidikanDk: PendidikanDkType.Default,
            pekerjaanDk: PekerjaanDkType.Default,
            tglMedRec: new DateTime(2020, 1, 1),
            isAktif: isAktif
        );

    [Fact]
    public async Task Given16DigitNikKeyword_WhenFound_ThenReturnsMatchingPasien()
    {
        // Arrange
        const string nik = "3201012304900001";
        const string pasienId = "00100123456";
        var person = CreateSamplePerson();
        var personView = new PasienPersonView(pasienId, true, person);

        _pasienRepoMock
            .Setup(x => x.GetDataByNik(nik))
            .Returns(MayBe.From(personView));

        var handler = CreateHandler();
        var query = new PasienSearchQuery(nik);

        // Act
        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].PasienId.Should().Be(pasienId);
        result[0].IsActive.Should().BeTrue();
        result[0].Person.PersonName.Should().Be("Budi Santoso");
        _pasienRepoMock.Verify(x => x.GetDataByNik(nik), Times.Once);
    }

    [Fact]
    public async Task Given16DigitNikKeyword_WhenNotFound_ThenReturnsEmpty()
    {
        // Arrange
        const string nik = "3201012304900002";
        _pasienRepoMock
            .Setup(x => x.GetDataByNik(nik))
            .Returns(MayBe<PasienPersonView>.None);

        var handler = CreateHandler();
        var query = new PasienSearchQuery(nik);

        // Act
        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Given13DigitBpjsKeyword_WhenFound_ThenReturnsMatchingPasien()
    {
        // Arrange
        const string noPesertaBpjs = "0001234567890";
        const string pasienId = "00100123456";
        var pasien = CreateSamplePasien(pasienId);

        var cover = new PolisCoverModel(
            "COV-01",
            new PasienReff(pasienId, "Budi Santoso", new DateOnly(1990, 5, 20), "L"),
            new StatusPesertaType("P", "Peserta")
        );
        var polis = new PolisModel(
            "POL-01", "0001234567890", "Budi Santoso",
            TipeJaminanType.Default.ToReff(), KelasType.Default.ToReff(),
            new DateOnly(2030, 1, 1), true, new List<PolisCoverModel> { cover }
        );

        _polisRepoMock
            .Setup(x => x.GetDataByNoPeserta(noPesertaBpjs))
            .Returns(polis);

        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == pasienId)))
            .Returns(MayBe.From(pasien));

        var handler = CreateHandler();
        var query = new PasienSearchQuery(noPesertaBpjs);

        // Act
        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].PasienId.Should().Be(pasienId);
        result[0].IsActive.Should().BeTrue();
        result[0].Person.PersonName.Should().Be("Budi Santoso");
    }

    [Fact]
    public async Task Given10CharRegKeyword_WhenActiveRegistrationFound_ThenReturnsMatchingPasien()
    {
        // Arrange
        const string regId = "RG20261001";
        const string pasienId = "00100123456";
        var pasien = CreateSamplePasien(pasienId, isAktif: true);

        var reg = new RegModel(
            regId, new DateOnly(2026, 10, 1),
            AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            new PasienReff(pasienId, "Budi Santoso", new DateOnly(1990, 5, 20), "L"),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            new RegEligibilityType("-", "-", "-"),
            []
        );

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(reg));

        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == pasienId)))
            .Returns(MayBe.From(pasien));

        var handler = CreateHandler();
        var query = new PasienSearchQuery(regId);

        // Act
        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].PasienId.Should().Be(pasienId);
        result[0].IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GivenNameOrDobKeyword_WhenMatchesFound_ThenReturnsMatchingPasienList()
    {
        // Arrange
        const string keyword = "Budi 1990-05-20";
        var list = new List<PasienPersonView>
        {
            new("00100123456", true, CreateSamplePerson("Budi Santoso")),
            new("00100123457", true, CreateSamplePerson("Budi Gunawan"))
        };

        _pasienRepoMock
            .Setup(x => x.SearchPasien(keyword))
            .Returns(list);

        var handler = CreateHandler();
        var query = new PasienSearchQuery(keyword);

        // Act
        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].PasienId.Should().Be("00100123456");
        result[1].PasienId.Should().Be("00100123457");
        _pasienRepoMock.Verify(x => x.SearchPasien(keyword), Times.Once);
    }

    [Fact]
    public async Task GivenSearchReturnsMoreThanLimit_WhenHandle_ThenThrowsTooManyResultsException()
    {
        // Arrange
        const string keyword = "Budi";
        var list = Enumerable.Range(1, 201)
            .Select(i => new PasienPersonView($"00100{i:D6}", true, CreateSamplePerson($"Budi {i}")))
            .ToList();

        _pasienRepoMock
            .Setup(x => x.SearchPasien(keyword))
            .Returns(list);

        var handler = CreateHandler();
        var query = new PasienSearchQuery(keyword);

        // Act
        var act = async () => await handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<TooManyResultsException>()
            .WithMessage("*Gunakan keyword search lebih spesifik*");
    }
}
