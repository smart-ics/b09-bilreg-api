using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class AdmUpdateInpatientJaminanHandlerTest
{
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly Mock<ITipeJaminanRepo> _tipeJaminanRepoMock = new();
    private readonly Mock<IPolisRepo> _polisRepoMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();
    private readonly Mock<IRegAktifRepo> _regAktifRepoMock = new();

    private readonly AdmUpdateInpatientJaminanHandler _sut;

    public AdmUpdateInpatientJaminanHandlerTest()
    {
        _sut = new AdmUpdateInpatientJaminanHandler(
            _regRepoMock.Object,
            _tipeJaminanRepoMock.Object,
            _polisRepoMock.Object,
            _auditRepoMock.Object,
            TestTglJamProvider.Instance,
            _regAktifRepoMock.Object);
    }

    [Fact]
    public async Task UT01_GivenValidRequestForBayarSendiri_WhenHandled_UpdatesToUmumAndSavesAuditWithoutBilling()
    {
        // Arrange
        var pasienReff = new PasienReff("P001", "Pasien A", new DateOnly(1990, 1, 1), "L");
        var reg = CreateSampleReg("RG001", pasienReff, tipeJaminanId: "BPJS01", polisId: "POL01");

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "RG001")))
            .Returns(MayBe.From(reg));

        var tipeUmum = TipeJaminanType.BayarSendiri;
        _tipeJaminanRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITipeJaminanKey>(k => k.TipeJaminanId == "00000")))
            .Returns(MayBe.From(tipeUmum));

        var cmd = new AdmUpdateInpatientJaminanCmd(
            RegId: "RG001",
            TipeJaminanId: "00000",
            PolisId: "-",
            SjpNo: null,
            PesertaJaminanId: null,
            SjpId: null,
            UserId: "user1");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        reg.TipeJaminan.TipeJaminanId.Should().Be("00000");
        reg.Polis.PolisId.Should().Be("-");
        _polisRepoMock.Verify(x => x.ListData(It.IsAny<IPasienKey>()), Times.Never);
        _polisRepoMock.Verify(x => x.LoadEntity(It.IsAny<IPolisKey>()), Times.Never);

        _regRepoMock.Verify(x => x.SaveChanges(reg), Times.Once);
        _regAktifRepoMock.Verify(x => x.SaveChanges(It.Is<RegAktifModel>(a => a.RegId == "RG001" && a.TipeJaminan.TipeJaminanId == "00000")), Times.Once);
        _auditRepoMock.Verify(x => x.SaveChanges(It.Is<AuditLog>(a =>
            a.ActionType == "UPDATE" &&
            a.EntityName == nameof(RegModel) &&
            a.EntityId == "RG001" &&
            a.UserId == "user1")), Times.Once);
    }

    [Fact]
    public async Task UT02_GivenValidRequestForAsuransiWithEligibility_WhenHandled_UpdatesJaminanPolisEligibilityAndSaves()
    {
        // Arrange
        var pasienReff = new PasienReff("P001", "Pasien A", new DateOnly(1990, 1, 1), "L");
        var reg = CreateSampleReg("RG001", pasienReff, tipeJaminanId: "00000", polisId: "-");

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "RG001")))
            .Returns(MayBe.From(reg));

        var tipeBpjs = new TipeJaminanType("BPJS01", "BPJS Kesehatan", true,
            new JaminanReff("JMN01", "BPJS"), new CaraBayarDkType("2", "Asuransi"));
        _tipeJaminanRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITipeJaminanKey>(k => k.TipeJaminanId == "BPJS01")))
            .Returns(MayBe.From(tipeBpjs));

        var polisView = new PolisView("POL01", "000112233", "Pasien A",
            pasienReff, tipeBpjs.ToReff(), new KelasReff("K1", "Kelas 1"), new DateOnly(2028, 1, 1));
        _polisRepoMock
            .Setup(x => x.ListData(It.Is<IPasienKey>(p => p.PasienId == "P001")))
            .Returns(new[] { polisView });

        var cover = new PolisCoverModel("POL01", pasienReff, StatusPesertaType.Create("P"));
        var polis = new PolisModel("POL01", "000112233", "Pasien A",
            tipeBpjs.ToReff(), new KelasReff("K1", "Kelas 1"), new DateOnly(2028, 1, 1),
            isCoverRajal: true, listCover: [cover]);
        _polisRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPolisKey>(k => k.PolisId == "POL01")))
            .Returns(MayBe.From(polis));

        var cmd = new AdmUpdateInpatientJaminanCmd(
            RegId: "RG001",
            TipeJaminanId: "BPJS01",
            PolisId: "POL01",
            SjpNo: "0001R0010525V000001",
            PesertaJaminanId: "000112233",
            SjpId: "SJP99",
            UserId: "user1");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        reg.TipeJaminan.TipeJaminanId.Should().Be("BPJS01");
        reg.Polis.PolisId.Should().Be("POL01");
        reg.Eligibility.SjpNo.Should().Be("0001R0010525V000001");
        reg.Eligibility.PesertaJaminanId.Should().Be("000112233");
        reg.Eligibility.SjpId.Should().Be("SJP99");

        _regRepoMock.Verify(x => x.SaveChanges(reg), Times.Once);
        _regAktifRepoMock.Verify(x => x.SaveChanges(It.Is<RegAktifModel>(a => a.RegId == "RG001" && a.TipeJaminan.TipeJaminanId == "BPJS01")), Times.Once);
        _auditRepoMock.Verify(x => x.SaveChanges(It.Is<AuditLog>(a =>
            a.ActionType == "UPDATE" &&
            a.EntityName == nameof(RegModel) &&
            a.EntityId == "RG001")), Times.Once);
    }

    [Fact]
    public async Task UT03_GivenPartialEligibilityFields_WhenHandled_SetsEligibilityCorrectly()
    {
        // Arrange
        var pasienReff = new PasienReff("P001", "Pasien A", new DateOnly(1990, 1, 1), "L");
        var reg = CreateSampleReg("RG001", pasienReff, tipeJaminanId: "00000", polisId: "-");

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "RG001")))
            .Returns(MayBe.From(reg));

        var tipeBpjs = new TipeJaminanType("BPJS01", "BPJS Kesehatan", true,
            new JaminanReff("JMN01", "BPJS"), new CaraBayarDkType("2", "Asuransi"));
        _tipeJaminanRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITipeJaminanKey>(k => k.TipeJaminanId == "BPJS01")))
            .Returns(MayBe.From(tipeBpjs));

        var polisView = new PolisView("POL01", "000112233", "Pasien A",
            pasienReff, tipeBpjs.ToReff(), new KelasReff("K1", "Kelas 1"), new DateOnly(2028, 1, 1));
        _polisRepoMock
            .Setup(x => x.ListData(It.Is<IPasienKey>(p => p.PasienId == "P001")))
            .Returns(new[] { polisView });

        var cover = new PolisCoverModel("POL01", pasienReff, StatusPesertaType.Create("P"));
        var polis = new PolisModel("POL01", "000112233", "Pasien A",
            tipeBpjs.ToReff(), new KelasReff("K1", "Kelas 1"), new DateOnly(2028, 1, 1),
            isCoverRajal: true, listCover: [cover]);
        _polisRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPolisKey>(k => k.PolisId == "POL01")))
            .Returns(MayBe.From(polis));

        var cmd = new AdmUpdateInpatientJaminanCmd(
            RegId: "RG001",
            TipeJaminanId: "BPJS01",
            PolisId: "POL01",
            SjpNo: "0001R0010525V000001",
            PesertaJaminanId: null,
            SjpId: null,
            UserId: "user1");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        reg.Eligibility.SjpNo.Should().Be("0001R0010525V000001");
        reg.Eligibility.PesertaJaminanId.Should().Be(string.Empty);
        reg.Eligibility.SjpId.Should().Be(string.Empty);
    }

    [Theory]
    [InlineData("", "00000", "user1")]
    [InlineData("   ", "00000", "user1")]
    [InlineData("RG001", "", "user1")]
    [InlineData("RG001", "   ", "user1")]
    [InlineData("RG001", "00000", "")]
    [InlineData("RG001", "00000", "   ")]
    public async Task UT04_GivenInvalidGuards_WhenHandled_ThrowsArgumentException(string regId, string tipeJaminanId, string userId)
    {
        // Arrange
        var cmd = new AdmUpdateInpatientJaminanCmd(regId, tipeJaminanId, "-", null, null, null, userId);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UT05_GivenRegNotFound_WhenHandled_ThrowsKeyNotFoundException()
    {
        // Arrange
        _regRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<RegModel>.None);

        var cmd = new AdmUpdateInpatientJaminanCmd("RG999", "00000", "-", null, null, null, "user1");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Registrasi 'RG999' tidak ditemukan*");
    }

    [Fact]
    public async Task UT06_GivenRegNotAktif_WhenHandled_ThrowsInvalidOperationException()
    {
        // Arrange
        var pasienReff = new PasienReff("P001", "Pasien A", new DateOnly(1990, 1, 1), "L");
        var reg = CreateSampleReg("RG001", pasienReff, isAktif: false);

        _regRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));

        var cmd = new AdmUpdateInpatientJaminanCmd("RG001", "00000", "-", null, null, null, "user1");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Registrasi 'RG001' sudah tidak aktif*");
    }

    [Fact]
    public async Task UT07_GivenTipeJaminanNotFound_WhenHandled_ThrowsKeyNotFoundException()
    {
        // Arrange
        var pasienReff = new PasienReff("P001", "Pasien A", new DateOnly(1990, 1, 1), "L");
        var reg = CreateSampleReg("RG001", pasienReff, isAktif: true);

        _regRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));

        _tipeJaminanRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITipeJaminanKey>()))
            .Returns(MayBe<TipeJaminanType>.None);

        var cmd = new AdmUpdateInpatientJaminanCmd("RG001", "TJ_INVALID", "-", null, null, null, "user1");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Tipe Jaminan 'TJ_INVALID' tidak ditemukan*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task UT08_GivenNonBayarSendiriWithEmptyPolisId_WhenHandled_ThrowsArgumentException(string? emptyPolisId)
    {
        // Arrange
        var pasienReff = new PasienReff("P001", "Pasien A", new DateOnly(1990, 1, 1), "L");
        var reg = CreateSampleReg("RG001", pasienReff, isAktif: true);

        _regRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));

        var tipeBpjs = new TipeJaminanType("BPJS01", "BPJS Kesehatan", true,
            new JaminanReff("JMN01", "BPJS"), new CaraBayarDkType("2", "Asuransi"));
        _tipeJaminanRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITipeJaminanKey>()))
            .Returns(MayBe.From(tipeBpjs));

        var cmd = new AdmUpdateInpatientJaminanCmd("RG001", "BPJS01", emptyPolisId!, null, null, null, "user1");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*PolisId wajib diisi*");
    }

    [Fact]
    public async Task UT09_GivenPolisNotOwnedByPatient_WhenHandled_ThrowsArgumentException()
    {
        // Arrange
        var pasienReff = new PasienReff("P001", "Pasien A", new DateOnly(1990, 1, 1), "L");
        var reg = CreateSampleReg("RG001", pasienReff, isAktif: true);

        _regRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));

        var tipeBpjs = new TipeJaminanType("BPJS01", "BPJS Kesehatan", true,
            new JaminanReff("JMN01", "BPJS"), new CaraBayarDkType("2", "Asuransi"));
        _tipeJaminanRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITipeJaminanKey>()))
            .Returns(MayBe.From(tipeBpjs));

        // Patient P001 only has POL02, not POL01
        var otherPolisView = new PolisView("POL02", "000999", "Pasien Lain",
            pasienReff, tipeBpjs.ToReff(), new KelasReff("K1", "Kelas 1"), new DateOnly(2028, 1, 1));
        _polisRepoMock
            .Setup(x => x.ListData(It.Is<IPasienKey>(p => p.PasienId == "P001")))
            .Returns(new[] { otherPolisView });

        var cmd = new AdmUpdateInpatientJaminanCmd("RG001", "BPJS01", "POL01", null, null, null, "user1");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*tidak memiliki polis 'POL01'*");
    }

    [Fact]
    public async Task UT10_GivenPolisNotFoundInLoadEntity_WhenHandled_ThrowsKeyNotFoundException()
    {
        // Arrange
        var pasienReff = new PasienReff("P001", "Pasien A", new DateOnly(1990, 1, 1), "L");
        var reg = CreateSampleReg("RG001", pasienReff, isAktif: true);

        _regRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));

        var tipeBpjs = new TipeJaminanType("BPJS01", "BPJS Kesehatan", true,
            new JaminanReff("JMN01", "BPJS"), new CaraBayarDkType("2", "Asuransi"));
        _tipeJaminanRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITipeJaminanKey>()))
            .Returns(MayBe.From(tipeBpjs));

        var polisView = new PolisView("POL01", "000112233", "Pasien A",
            pasienReff, tipeBpjs.ToReff(), new KelasReff("K1", "Kelas 1"), new DateOnly(2028, 1, 1));
        _polisRepoMock
            .Setup(x => x.ListData(It.Is<IPasienKey>(p => p.PasienId == "P001")))
            .Returns(new[] { polisView });

        _polisRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPolisKey>(k => k.PolisId == "POL01")))
            .Returns(MayBe<PolisModel>.None);

        var cmd = new AdmUpdateInpatientJaminanCmd("RG001", "BPJS01", "POL01", null, null, null, "user1");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Polis 'POL01' tidak ditemukan*");
    }

    [Fact]
    public async Task UT11_GivenPolisTipeJaminanMismatch_WhenHandled_ThrowsArgumentExceptionFromDomain()
    {
        // Arrange
        var pasienReff = new PasienReff("P001", "Pasien A", new DateOnly(1990, 1, 1), "L");
        var reg = CreateSampleReg("RG001", pasienReff, isAktif: true);

        _regRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));

        var tipeBpjs = new TipeJaminanType("BPJS01", "BPJS Kesehatan", true,
            new JaminanReff("JMN01", "BPJS"), new CaraBayarDkType("2", "Asuransi"));
        _tipeJaminanRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITipeJaminanKey>()))
            .Returns(MayBe.From(tipeBpjs));

        var polisView = new PolisView("POL01", "000112233", "Pasien A",
            pasienReff, tipeBpjs.ToReff(), new KelasReff("K1", "Kelas 1"), new DateOnly(2028, 1, 1));
        _polisRepoMock
            .Setup(x => x.ListData(It.Is<IPasienKey>(p => p.PasienId == "P001")))
            .Returns(new[] { polisView });

        // Polis actually belongs to a DIFFERENT TipeJaminan "ASR01"
        var differentTipe = new TipeJaminanReff("ASR01", "Asuransi Lain");
        var cover = new PolisCoverModel("POL01", pasienReff, StatusPesertaType.Create("P"));
        var polis = new PolisModel("POL01", "000112233", "Pasien A",
            differentTipe, new KelasReff("K1", "Kelas 1"), new DateOnly(2028, 1, 1),
            isCoverRajal: true, listCover: [cover]);

        _polisRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPolisKey>(k => k.PolisId == "POL01")))
            .Returns(MayBe.From(polis));

        var cmd = new AdmUpdateInpatientJaminanCmd("RG001", "BPJS01", "POL01", null, null, null, "user1");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Polis tidak sesuai dengan tipe jaminan*");
    }

    [Fact]
    public async Task UT12_GivenPolisCoverDoesNotContainPatient_WhenHandled_ThrowsArgumentExceptionFromDomain()
    {
        // Arrange
        var pasienReff = new PasienReff("P001", "Pasien A", new DateOnly(1990, 1, 1), "L");
        var reg = CreateSampleReg("RG001", pasienReff, isAktif: true);

        _regRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));

        var tipeBpjs = new TipeJaminanType("BPJS01", "BPJS Kesehatan", true,
            new JaminanReff("JMN01", "BPJS"), new CaraBayarDkType("2", "Asuransi"));
        _tipeJaminanRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITipeJaminanKey>()))
            .Returns(MayBe.From(tipeBpjs));

        var polisView = new PolisView("POL01", "000112233", "Pasien Lain",
            pasienReff, tipeBpjs.ToReff(), new KelasReff("K1", "Kelas 1"), new DateOnly(2028, 1, 1));
        _polisRepoMock
            .Setup(x => x.ListData(It.Is<IPasienKey>(p => p.PasienId == "P001")))
            .Returns(new[] { polisView });

        // Polis cover contains P999 instead of P001
        var otherPatient = new PasienReff("P999", "Pasien Lain", new DateOnly(1980, 1, 1), "P");
        var cover = new PolisCoverModel("POL01", otherPatient, StatusPesertaType.Create("P"));
        var polis = new PolisModel("POL01", "000112233", "Pasien Lain",
            tipeBpjs.ToReff(), new KelasReff("K1", "Kelas 1"), new DateOnly(2028, 1, 1),
            isCoverRajal: true, listCover: [cover]);

        _polisRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPolisKey>(k => k.PolisId == "POL01")))
            .Returns(MayBe.From(polis));

        var cmd = new AdmUpdateInpatientJaminanCmd("RG001", "BPJS01", "POL01", null, null, null, "user1");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*tidak meng-cover pasien*");
    }

    [Fact]
    public async Task UT13_GivenNullRegAktifRepo_WhenHandled_SucceedsWithoutThrowing()
    {
        // Arrange
        var sutWithoutRegAktif = new AdmUpdateInpatientJaminanHandler(
            _regRepoMock.Object,
            _tipeJaminanRepoMock.Object,
            _polisRepoMock.Object,
            _auditRepoMock.Object,
            TestTglJamProvider.Instance,
            regAktifRepo: null);

        var pasienReff = new PasienReff("P001", "Pasien A", new DateOnly(1990, 1, 1), "L");
        var reg = CreateSampleReg("RG001", pasienReff, isAktif: true);

        _regRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));

        _tipeJaminanRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITipeJaminanKey>()))
            .Returns(MayBe.From(TipeJaminanType.BayarSendiri));

        var cmd = new AdmUpdateInpatientJaminanCmd("RG001", "00000", "-", null, null, null, "user1");

        // Act
        var act = () => sutWithoutRegAktif.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        _regRepoMock.Verify(x => x.SaveChanges(reg), Times.Once);
    }

    private static RegModel CreateSampleReg(
        string regId = "RG001",
        PasienReff? pasien = null,
        bool isAktif = true,
        string tipeJaminanId = "00000",
        string polisId = "-")
    {
        pasien ??= new PasienReff("P001", "Pasien A", new DateOnly(1990, 1, 1), "L");
        return new RegModel(
            regId,
            new DateOnly(2025, 5, 1),
            new AuditInfoType("user1", new DateTime(2025, 5, 1, 8, 0, 0)),
            isAktif ? AuditInfoType.Default : new AuditInfoType("user1", new DateTime(2025, 5, 2, 8, 0, 0)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegInap,
            pasien,
            new TipeJaminanReff(tipeJaminanId, "Tipe " + tipeJaminanId),
            new PolisReff(polisId, "NO-" + polisId, pasien.PasienName),
            new KelasReff("K1", "Kelas 1"),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            new PpaReff("DK1", "Dr. Satu"),
            new LayananReff("L1", "Bangsal Layanan"),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            [],
            null,
            null,
            JenisInapType.Umum,
            "");
    }
}
