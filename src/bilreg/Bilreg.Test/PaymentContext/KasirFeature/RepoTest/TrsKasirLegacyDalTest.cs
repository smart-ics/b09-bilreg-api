using Bilreg.Application.PaymentContext.KasirFeature;
using Bilreg.Infrastructure.PaymentContext.KasirFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.RepoTest;

public class TrsKasirLegacyDalTest
{
    private readonly TrsKasirLegacyDal _sut;

    public TrsKasirLegacyDalTest()
    {
        _sut = new TrsKasirLegacyDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void Insert_HeaderAndDetails_PersistsCorrectly()
    {
        var trsKasirId = "DX" + Guid.NewGuid().ToString("N")[..8].ToUpper();
        var regId = "RG" + Guid.NewGuid().ToString("N")[..8].ToUpper();

        var hdr = new TrsKasirLegacyHdrDto(
            trsKasirId: trsKasirId,
            tglTrs: "2026-11-11",
            jamTrs: "14:00:00",
            petugasId: "KASIR01",
            regId: regId,
            sumKas: 100_000m,
            sumBank: 150_000m,
            sumKartuKredit: 0m,
            sumAll: 250_000m,
            kdRekKas: "1110201",
            kdRekBank: "1110301",
            kdRekDeposit: "4210000",
            genKeu: 5,
            crtUsr: "KASIR01"
        );

        var details = new List<TrsKasirLegacyDtlDto>
        {
            new(
                trsKasirId: trsKasirId,
                noUrut: 1,
                trsBillId: "BL01378027",
                layananId: "2RJ05",
                keterangan: "Pemeriksaan Dokter Spesialis",
                nilaiBill: 100_000m,
                nilaiKas: 100_000m,
                nilaiBank: 0m,
                nilaiKartuKredit: 0m,
                isJasa: true
            ),
            new(
                trsKasirId: trsKasirId,
                noUrut: 2,
                trsBillId: "BL01378028",
                layananId: "4OB01",
                keterangan: "Resep Farmasi",
                nilaiBill: 150_000m,
                nilaiKas: 0m,
                nilaiBank: 150_000m,
                nilaiKartuKredit: 0m,
                isObat: true
            )
        };

        try
        {
            // Act
            _sut.Insert(hdr, details);

            var savedHdr = _sut.GetHeader(trsKasirId);
            var savedDtls = _sut.ListDetails(trsKasirId).ToList();

            // Assert
            savedHdr.Should().NotBeNull();
            savedHdr!.TrsKasirId.Should().Be(trsKasirId);
            savedHdr.TglTrs.Should().Be("2026-11-11");
            savedHdr.JamTrs.Should().Be("14:00:00");
            savedHdr.PetugasId.Should().Be("KASIR01");
            savedHdr.RegId.Should().Be(regId);
            savedHdr.SumKas.Should().Be(100_000m);
            savedHdr.SumBank.Should().Be(150_000m);
            savedHdr.SumKartuKredit.Should().Be(0m);
            savedHdr.SumAll.Should().Be(250_000m);
            savedHdr.KdRekKas.Should().Be("1110201");
            savedHdr.KdRekBank.Should().Be("1110301");
            savedHdr.KdRekDeposit.Should().Be("4210000");
            savedHdr.TglVoid.Should().Be("3000-01-01");
            savedHdr.JamVoid.Should().Be("00:00:00");
            savedHdr.PetugasVoid.Should().Be(string.Empty);

            savedDtls.Should().HaveCount(2);
            savedDtls[0].NoUrut.Should().Be(1);
            savedDtls[0].TrsBillId.Should().Be("BL01378027");
            savedDtls[0].LayananId.Should().Be("2RJ05");
            savedDtls[0].Keterangan.Should().Be("Pemeriksaan Dokter Spesialis");
            savedDtls[0].NilaiBill.Should().Be(100_000m);
            savedDtls[0].NilaiKas.Should().Be(100_000m);
            savedDtls[0].NilaiBank.Should().Be(0m);
            savedDtls[0].IsVoid.Should().BeFalse();
            savedDtls[0].IsJasa.Should().BeTrue();
            savedDtls[0].IsObat.Should().BeFalse();

            savedDtls[1].NoUrut.Should().Be(2);
            savedDtls[1].TrsBillId.Should().Be("BL01378028");
            savedDtls[1].LayananId.Should().Be("4OB01");
            savedDtls[1].NilaiBill.Should().Be(150_000m);
            savedDtls[1].NilaiKas.Should().Be(0m);
            savedDtls[1].NilaiBank.Should().Be(150_000m);
            savedDtls[1].IsVoid.Should().BeFalse();
            savedDtls[1].IsJasa.Should().BeFalse();
            savedDtls[1].IsObat.Should().BeTrue();
        }
        finally
        {
            _sut.Delete(trsKasirId);
        }
    }

    [Fact]
    public void Void_UpdatesHeaderAndDetailsVoidStatus()
    {
        var trsKasirId = "DX" + Guid.NewGuid().ToString("N")[..8].ToUpper();
        var regId = "RG" + Guid.NewGuid().ToString("N")[..8].ToUpper();

        var hdr = new TrsKasirLegacyHdrDto(
            trsKasirId: trsKasirId,
            tglTrs: "2026-11-11",
            jamTrs: "10:00:00",
            petugasId: "KASIR01",
            regId: regId,
            sumKas: 50_000m,
            sumBank: 0m,
            sumKartuKredit: 0m,
            sumAll: 50_000m,
            kdRekKas: "1110202",
            kdRekBank: ""
        );

        var details = new List<TrsKasirLegacyDtlDto>
        {
            new(
                trsKasirId: trsKasirId,
                noUrut: 1,
                trsBillId: "BL001",
                layananId: "L01",
                keterangan: "Uji Void",
                nilaiBill: 50_000m,
                nilaiKas: 50_000m,
                nilaiBank: 0m,
                nilaiKartuKredit: 0m
            )
        };

        try
        {
            _sut.Insert(hdr, details);

            // Act
            _sut.Void(trsKasirId, "2026-11-11", "10:15:30", "SPV_BOB");

            // Assert
            var voidedHdr = _sut.GetHeader(trsKasirId);
            voidedHdr.Should().NotBeNull();
            voidedHdr!.TglVoid.Should().Be("2026-11-11");
            voidedHdr.JamVoid.Should().Be("10:15:30");
            voidedHdr.PetugasVoid.Should().Be("SPV_BOB");
            voidedHdr.UpdTgl.Should().Be("2026-11-11");
            voidedHdr.UpdJam.Should().Be("10:15:30");
            voidedHdr.UpdUsr.Should().Be("SPV_BOB");

            var voidedDtls = _sut.ListDetails(trsKasirId).ToList();
            voidedDtls.Should().HaveCount(1);
            voidedDtls[0].IsVoid.Should().BeTrue();
        }
        finally
        {
            _sut.Delete(trsKasirId);
        }
    }

    [Fact]
    public void Insert_StringLengthTruncation_HandlesOverlengthGracefully()
    {
        var rawId = "DX" + Guid.NewGuid().ToString("N").ToUpper(); // > 10 chars
        var expectedId = rawId[..10];

        var hdr = new TrsKasirLegacyHdrDto(
            trsKasirId: rawId,
            tglTrs: "2026-11-11",
            jamTrs: "11:22:33",
            petugasId: "SUPER_LONG_USER_ID_OVER_10_CHARS",
            regId: "REG_EXTRA_LONG_1234567890",
            sumKas: 10_000m,
            sumBank: 0m,
            sumKartuKredit: 0m,
            sumAll: 10_000m,
            kdRekKas: "REK_KAS_LONGER_THAN_20_CHARACTERS_XYZ",
            kdRekBank: ""
        );

        var details = new List<TrsKasirLegacyDtlDto>
        {
            new(
                trsKasirId: rawId,
                noUrut: 1,
                trsBillId: "BILL_ID_OVER_15_CHARS_LONG",
                layananId: "LAYANAN_OVER_5",
                keterangan: "A very long description that easily exceeds the maximum column limit of 60 characters in legacy table",
                nilaiBill: 10_000m,
                nilaiKas: 10_000m,
                nilaiBank: 0m,
                nilaiKartuKredit: 0m
            )
        };

        try
        {
            // Act
            _sut.Insert(hdr, details);

            // Assert
            var savedHdr = _sut.GetHeader(expectedId);
            savedHdr.Should().NotBeNull();
            savedHdr!.TrsKasirId.Should().Be(expectedId);
            savedHdr.PetugasId.Length.Should().BeLessOrEqualTo(10);
            savedHdr.RegId.Length.Should().BeLessOrEqualTo(10);
            savedHdr.KdRekKas.Length.Should().BeLessOrEqualTo(20);

            var savedDtls = _sut.ListDetails(expectedId).ToList();
            savedDtls.Should().HaveCount(1);
            savedDtls[0].TrsBillId.Length.Should().BeLessOrEqualTo(15);
            savedDtls[0].LayananId.Length.Should().BeLessOrEqualTo(5);
            savedDtls[0].Keterangan.Length.Should().BeLessOrEqualTo(60);
        }
        finally
        {
            _sut.Delete(expectedId);
        }
    }

    [Fact]
    public void GetHeader_NonExistentId_ReturnsNull()
    {
        var result = _sut.GetHeader("DXNONEXIST");
        result.Should().BeNull();
    }

    [Fact]
    public void Delete_RemovesHeaderAndDetails()
    {
        var trsKasirId = "DX" + Guid.NewGuid().ToString("N")[..8].ToUpper();
        var hdr = new TrsKasirLegacyHdrDto(
            trsKasirId: trsKasirId,
            tglTrs: "2026-11-11",
            jamTrs: "12:00:00",
            petugasId: "ADM",
            regId: "RG001",
            sumKas: 10_000m,
            sumBank: 0m,
            sumKartuKredit: 0m,
            sumAll: 10_000m,
            kdRekKas: "1110201",
            kdRekBank: ""
        );
        var details = new List<TrsKasirLegacyDtlDto>
        {
            new(trsKasirId, 1, "BL01", "L01", "Ket", 10_000m, 10_000m, 0m, 0m)
        };

        _sut.Insert(hdr, details);
        _sut.GetHeader(trsKasirId).Should().NotBeNull();
        _sut.ListDetails(trsKasirId).Should().NotBeEmpty();

        // Act
        _sut.Delete(trsKasirId);

        // Assert
        _sut.GetHeader(trsKasirId).Should().BeNull();
        _sut.ListDetails(trsKasirId).Should().BeEmpty();
    }
}
