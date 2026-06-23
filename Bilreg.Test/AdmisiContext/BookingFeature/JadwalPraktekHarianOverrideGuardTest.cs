using Bilreg.Infrastructure.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.BookingFeature;

public class JadwalPraktekHarianOverrideGuardTest
{
    private readonly Mock<IBookingRepo> _bookingRepo = new();
    private readonly Mock<IAntrianRepo> _antrianRepo = new();
    private readonly Mock<IPpaRepo> _ppaRepo = new();
    private readonly JadwalPraktekHarianOverrideGuard _sut;

    public JadwalPraktekHarianOverrideGuardTest()
    {
        _sut = new JadwalPraktekHarianOverrideGuard(
            _bookingRepo.Object, _antrianRepo.Object, _ppaRepo.Object);
    }

    [Fact]
    public void UT01_ThrowsWhenActiveBookingExists()
    {
        var tgl = new DateOnly(2026, 7, 15);
        var jam = new TimeOnly(8, 0);
        var dokter = PpaType.Key("DR001");
        _bookingRepo.Setup(r => r.ListDataTglBerobat(It.IsAny<Periode>()))
            .Returns([
                new BookingView("B1", DateTime.Now, PersonInfoType.Default,
                    RegModel.Default.ToReff(), tgl, jam,
                    LayananType.Default.ToReff(), new PpaReff("DR001", "Dr"), 1)
            ]);

        var act = () => _sut.EnsureNoOperationalConflict(tgl, dokter, jam);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*booking aktif*");
    }

    [Fact]
    public void UT02_ThrowsWhenAntrianEntryExists()
    {
        var tgl = new DateOnly(2026, 7, 15);
        var jam = new TimeOnly(8, 0);
        var dokter = PpaType.Key("DR001");
        var ppa = new PpaType("DR001", "Dr", "Dr", SmfType.Default,
            GroupSpesialisType.Default, [], [], []);
        var tag = AntrianModel.GenSequenceTag(tgl, jam, ppa);

        _bookingRepo.Setup(r => r.ListDataTglBerobat(It.IsAny<Periode>())).Returns([]);
        _ppaRepo.Setup(r => r.LoadEntity(dokter)).Returns(MayBe.From(ppa));
        _antrianRepo.Setup(r => r.ListData(tgl))
            .Returns([new AntrianHeaderView("A1", "desc", tgl, jam, tag)]);

        var act = () => _sut.EnsureNoOperationalConflict(tgl, dokter, jam);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*antrian aktif*");
    }
}
