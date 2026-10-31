using Bilreg.Domain.AdmisiContext.RegFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class InpatientAgePolicyTest
{
    [Theory]
    [InlineData("2026-10-01", "2026-10-01", true)]  // 0 hari
    [InlineData("2026-10-01", "2026-10-15", true)]  // 14 hari
    [InlineData("2026-10-01", "2026-10-31", true)]  // Hari terakhir sebelum tepat 1 bulan
    [InlineData("2026-10-01", "2026-11-01", false)] // Tepat 1 bulan
    [InlineData("2026-10-01", "2026-11-02", false)] // Lebih dari 1 bulan
    [InlineData("2026-01-31", "2026-02-27", true)]  // Akhir Januari ke Februari (< 1 bulan)
    [InlineData("2026-01-31", "2026-02-28", false)] // Akhir Januari ke 28 Feb (tepat 1 bulan kalender)
    [InlineData("1990-05-20", "2026-10-31", false)] // Pasien dewasa
    [InlineData("3000-01-01", "2026-10-31", false)] // Tanggal sentinel default
    [InlineData("2026-11-10", "2026-10-31", false)] // Tanggal lahir di masa depan
    public void GivenBirthDateAndRegDate_WhenIsPerinatalAgeEvaluated_ThenReturnsExpectedResult(
        string tglLahirStr, string regDateStr, bool expected)
    {
        var tglLahir = DateOnly.Parse(tglLahirStr);
        var regDate = DateOnly.Parse(regDateStr);

        var actual = InpatientAgePolicy.IsPerinatalAge(tglLahir, regDate);

        actual.Should().Be(expected);
    }

    [Fact]
    public void GivenPerinatalAge_WhenResolveJenisInap_ThenAlwaysForcesPerinatalRegardlessOfPayload()
    {
        var tglLahir = new DateOnly(2026, 10, 20);
        var regDate = new DateOnly(2026, 10, 31); // 11 hari (< 1 bulan)

        InpatientAgePolicy.ResolveJenisInap(tglLahir, regDate, null)
            .Should().Be(InpatientAgePolicy.JENIS_INAP_PERINATAL);
        InpatientAgePolicy.ResolveJenisInap(tglLahir, regDate, "1")
            .Should().Be(InpatientAgePolicy.JENIS_INAP_PERINATAL);
        InpatientAgePolicy.ResolveJenisInap(tglLahir, regDate, "2")
            .Should().Be(InpatientAgePolicy.JENIS_INAP_PERINATAL);
        InpatientAgePolicy.ResolveJenisInap(tglLahir, regDate, "3")
            .Should().Be(InpatientAgePolicy.JENIS_INAP_PERINATAL);
    }

    [Fact]
    public void GivenNonPerinatalAge_WhenResolveJenisInap_ThenResolvesCorrectlyAccordingToSop()
    {
        var tglLahir = new DateOnly(1995, 5, 20);
        var regDate = new DateOnly(2026, 10, 31); // Dewasa

        // Default Umum (1) jika null, kosong, atau "1"
        InpatientAgePolicy.ResolveJenisInap(tglLahir, regDate, null)
            .Should().Be(InpatientAgePolicy.JENIS_INAP_UMUM);
        InpatientAgePolicy.ResolveJenisInap(tglLahir, regDate, "")
            .Should().Be(InpatientAgePolicy.JENIS_INAP_UMUM);
        InpatientAgePolicy.ResolveJenisInap(tglLahir, regDate, "1")
            .Should().Be(InpatientAgePolicy.JENIS_INAP_UMUM);

        // Obstetri (2) jika dipilih klien
        InpatientAgePolicy.ResolveJenisInap(tglLahir, regDate, "2")
            .Should().Be(InpatientAgePolicy.JENIS_INAP_OBSTETRI);

        // Jika klien mengirim "3" untuk orang dewasa, SOP memaksa fallback ke Umum (1)
        InpatientAgePolicy.ResolveJenisInap(tglLahir, regDate, "3")
            .Should().Be(InpatientAgePolicy.JENIS_INAP_UMUM);
    }
}
