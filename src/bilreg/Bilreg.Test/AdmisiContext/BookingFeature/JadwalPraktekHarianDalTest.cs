using Bilreg.Infrastructure.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.AdmisiContext.BookingFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.BookingFeature;

public class JadwalPraktekHarianDalTest
{
    private readonly JadwalPraktekHarianDal _sut = new(ConnStringHelper.GetTestEnv());

    private static JadwalPraktekHarianDto Faker(string id = "JPH99999999") =>
        new(id, "JADW001", new DateTime(2026, 7, 15), "DR001", "LY001", "R001",
            "08:00", "12:00", 30, "{}", "ACTIVE", "MANUAL", null,
            "test", DateTime.Now, "test", DateTime.Now,
            "Dr", "Layanan", "", "", "GS01", "Group", "Ruang", "P");

    [Fact(Skip = "Requires BILRG_JadwalPraktekHarian table in test database (run M2 migration)")]
    public void UT01_InsertAndGetData_RoundTrip()
    {
        using var trans = TransHelper.NewScope();
        var dto = Faker();
        _sut.Insert(dto);
        var loaded = _sut.GetData(JadwalPraktekHarianType.Key(dto.JadwalPraktekHarianId));
        loaded.JadwalPraktekHarianId.Should().Be(dto.JadwalPraktekHarianId);
        loaded.DokterId.Should().Be(dto.DokterId);
    }
}
