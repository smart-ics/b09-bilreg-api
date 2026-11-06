using System.Reflection;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.InventoryContext.PakaiBrgFeature;

public class PakaiBrgModelTests
{
    private static LayananReff SampleLayanan() => new("L01", "Poli Penyakit Dalam");
    private static BrgReff SampleBrg(string id = "B001", string name = "Spuit 3cc") => new(id, name);
    private static SatuanReff SampleSatuan(string id = "PCS", string name = "Pieces") => new(id, name);

    private static PakaiBrgItemModel CreateSampleItem(
        int noUrut = 1,
        decimal qty = 2,
        decimal hpp = 5000,
        string brgId = "B001",
        string noBatch = "BATCH01",
        string keterangan = "Item note")
    {
        return PakaiBrgItemModel.Create(
            noUrut,
            SampleBrg(brgId),
            SampleSatuan(),
            stokCurrent: 10,
            qty: qty,
            noBatch: noBatch,
            tglEd: new DateTime(2027, 12, 31),
            hpp: hpp,
            keterangan: keterangan);
    }

    [Fact]
    public void GivenValidData_WhenCreate_ThenInitializesCorrectlyWithCompactId()
    {
        // Arrange
        var tgl = new DateTime(2026, 10, 23, 10, 0, 0);
        var item1 = CreateSampleItem(1, qty: 2, hpp: 5000);
        var item2 = CreateSampleItem(2, qty: 3, hpp: 10000, brgId: "B002");

        // Act
        var doc = PakaiBrgModel.Create(tgl, SampleLayanan(), "petugas01", "Keperluan IGD", [item1, item2]);

        // Assert
        doc.Should().BeAssignableTo<IPakaiBrgKey>();
        doc.PakaiBrgId.Should().StartWith("PK");
        doc.PakaiBrgId.Length.Should().Be(10);
        doc.TglPakai.Should().Be(tgl);
        doc.Layanan.LayananId.Should().Be("L01");
        doc.PetugasId.Should().Be("petugas01");
        doc.Keterangan.Should().Be("Keperluan IGD");
        doc.IsVoid.Should().BeFalse();
        doc.TglVoid.Should().Be(AppConst.DEF_DATE);
        doc.PetugasVoidId.Should().BeEmpty();
        doc.AlasanVoid.Should().BeEmpty();
        doc.ListItem.Should().HaveCount(2);
        doc.TotalNilai.Should().Be(2 * 5000 + 3 * 10000); // 10000 + 30000 = 40000
    }

    [Fact]
    public void GivenExplicitId_WhenCreate_ThenPreservesSpecifiedId()
    {
        // Arrange
        var item = CreateSampleItem();

        // Act
        var doc = PakaiBrgModel.Create(DateTime.Now, SampleLayanan(), "petugas01", "Note", [item], pakaiBrgId: "PK26100001");

        // Assert
        doc.PakaiBrgId.Should().Be("PK26100001");
    }

    [Fact]
    public void GivenItemsWithoutNoUrut_WhenCreate_ThenSequencesNoUrutCorrectly()
    {
        // Arrange
        var item1 = PakaiBrgItemModel.Create(0, SampleBrg("B1"), SampleSatuan(), 10, 1, "B1", DateTime.Today, 1000, "");
        var item2 = PakaiBrgItemModel.Create(0, SampleBrg("B2"), SampleSatuan(), 10, 2, "B2", DateTime.Today, 2000, "");

        // Act
        var doc = PakaiBrgModel.Create(DateTime.Now, SampleLayanan(), "petugas01", "Note", [item1, item2]);

        // Assert
        doc.ListItem[0].NoUrut.Should().Be(1);
        doc.ListItem[1].NoUrut.Should().Be(2);
    }

    [Fact]
    public void GivenSubTotalCalculation_WhenCreateItem_ThenCalculatesAccurately()
    {
        // Arrange & Act
        var item = PakaiBrgItemModel.Create(1, SampleBrg(), SampleSatuan(), 25, 3.5m, "BATCH123", DateTime.Today, 12500m, "Catatan");

        // Assert
        item.SubTotal.Should().Be(3.5m * 12500m);
        item.IsVoid.Should().BeFalse();
    }

    [Fact]
    public void GivenLongStrings_WhenCreate_ThenTruncatesSafelyToColumnLimits()
    {
        // Arrange
        var longKeterangan40 = new string('A', 50); // header fs_keterangan1 max 40
        var longItemKeterangan30 = new string('B', 45); // item fs_keterangan max 30
        var longBatch15 = new string('C', 20); // fs_no_batch max 15

        var item = CreateSampleItem(keterangan: longItemKeterangan30, noBatch: longBatch15);

        // Act
        var doc = PakaiBrgModel.Create(DateTime.Now, SampleLayanan(), "user1", longKeterangan40, [item]);

        // Assert
        doc.Keterangan.Length.Should().Be(40);
        doc.Keterangan.Should().Be(new string('A', 40));

        item.Keterangan.Length.Should().Be(30);
        item.Keterangan.Should().Be(new string('B', 30));

        item.NoBatch.Length.Should().Be(15);
        item.NoBatch.Should().Be(new string('C', 15));
    }

    [Fact]
    public void GivenInvalidCreationArguments_WhenInstantiate_ThenThrowsExpectedExceptions()
    {
        var item = CreateSampleItem();

        // 1. Invalid Layanan
        var actNullLayanan = () => PakaiBrgModel.Create(DateTime.Now, null!, "user1", "", [item]);
        actNullLayanan.Should().Throw<ArgumentNullException>();

        var actEmptyLayanan = () => PakaiBrgModel.Create(DateTime.Now, new LayananReff("", "Test"), "user1", "", [item]);
        actEmptyLayanan.Should().Throw<ArgumentException>().WithMessage("*Layanan*");

        var actDashLayanan = () => PakaiBrgModel.Create(DateTime.Now, new LayananReff("-", "Test"), "user1", "", [item]);
        actDashLayanan.Should().Throw<ArgumentException>().WithMessage("*Layanan*");

        // 2. Invalid Petugas
        var actEmptyPetugas = () => PakaiBrgModel.Create(DateTime.Now, SampleLayanan(), "  ", "", [item]);
        actEmptyPetugas.Should().Throw<ArgumentException>();

        // 3. Empty / Null Items
        var actEmptyItems = () => PakaiBrgModel.Create(DateTime.Now, SampleLayanan(), "user1", "", []);
        actEmptyItems.Should().Throw<ArgumentException>().WithMessage("*minimal 1 item*");

        var actNullItems = () => PakaiBrgModel.Create(DateTime.Now, SampleLayanan(), "user1", "", null!);
        actNullItems.Should().Throw<ArgumentNullException>();

        // 4. Item Invariants: Qty <= 0
        var actZeroQty = () => CreateSampleItem(qty: 0);
        actZeroQty.Should().Throw<ArgumentException>().WithMessage("*Qty*");

        var actNegativeQty = () => CreateSampleItem(qty: -1);
        actNegativeQty.Should().Throw<ArgumentException>().WithMessage("*Qty*");

        // 5. Item Invariants: Hpp < 0
        var actNegativeHpp = () => CreateSampleItem(hpp: -100);
        actNegativeHpp.Should().Throw<ArgumentException>().WithMessage("*HPP*");

        // 6. Item Invariants: Null / empty Brg
        var actNullBrg = () => PakaiBrgItemModel.Create(1, null!, SampleSatuan(), 10, 1, "", DateTime.Today, 100, "");
        actNullBrg.Should().Throw<ArgumentNullException>();

        var actEmptyBrgId = () => PakaiBrgItemModel.Create(1, new BrgReff("", "Brg"), SampleSatuan(), 10, 1, "", DateTime.Today, 100, "");
        actEmptyBrgId.Should().Throw<ArgumentException>();

        // 7. Item Invariants: Null Satuan
        var actNullSatuan = () => PakaiBrgItemModel.Create(1, SampleBrg(), null!, 10, 1, "", DateTime.Today, 100, "");
        actNullSatuan.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GivenActiveDocument_WhenVoid_ThenUpdatesHeaderAndMarksAllItemsVoid()
    {
        // Arrange
        var item1 = CreateSampleItem(1, qty: 2, hpp: 5000);
        var item2 = CreateSampleItem(2, qty: 1, hpp: 10000, brgId: "B002");
        var doc = PakaiBrgModel.Create(DateTime.Now, SampleLayanan(), "user1", "Operasional", [item1, item2]);
        var tglVoid = new DateTime(2026, 10, 23, 14, 30, 0);

        // Act
        doc.Void("petugasVoid1", "Salah input kuantitas", tglVoid);

        // Assert
        doc.IsVoid.Should().BeTrue();
        doc.PetugasVoidId.Should().Be("petugasVoid1");
        doc.AlasanVoid.Should().Be("Salah input kuantitas");
        doc.TglVoid.Should().Be(tglVoid);

        doc.ListItem.Should().AllSatisfy(item => item.IsVoid.Should().BeTrue());
    }

    [Fact]
    public void GivenLongAlasanVoid_WhenVoid_ThenTruncatesSafelyTo40Chars()
    {
        // Arrange
        var item = CreateSampleItem();
        var doc = PakaiBrgModel.Create(DateTime.Now, SampleLayanan(), "user1", "Operasional", [item]);
        var longAlasan = new string('V', 60);

        // Act
        doc.Void("petugasVoid", longAlasan);

        // Assert
        doc.AlasanVoid.Length.Should().Be(40);
        doc.AlasanVoid.Should().Be(new string('V', 40));
    }

    [Fact]
    public void GivenActiveDocument_WhenVoidWithEmptyAlasanOrPetugas_ThenThrowsArgumentException()
    {
        // Arrange
        var item = CreateSampleItem();
        var doc = PakaiBrgModel.Create(DateTime.Now, SampleLayanan(), "user1", "Operasional", [item]);

        // Act & Assert
        var actEmptyAlasan = () => doc.Void("petugasVoid", "   ");
        actEmptyAlasan.Should().Throw<ArgumentException>().WithMessage("*Alasan pembatalan*");

        var actNullAlasan = () => doc.Void("petugasVoid", null!);
        actNullAlasan.Should().Throw<ArgumentException>().WithMessage("*Alasan pembatalan*");

        var actEmptyPetugas = () => doc.Void("   ", "Alasan valid");
        actEmptyPetugas.Should().Throw<ArgumentException>().WithMessage("*Petugas pembatal*");
    }

    [Fact]
    public void GivenVoidedDocument_WhenVoidAgain_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var item = CreateSampleItem();
        var doc = PakaiBrgModel.Create(DateTime.Now, SampleLayanan(), "user1", "Operasional", [item]);
        doc.Void("petugasVoid", "Alasan awal");

        // Act & Assert
        var actDoubleVoid = () => doc.Void("petugasVoid2", "Alasan kedua");
        actDoubleVoid.Should().Throw<InvalidOperationException>().WithMessage("*sudah dibatalkan*");
    }

    [Fact]
    public void GivenPersistedData_WhenRehydrate_ThenRestoresExactState()
    {
        // Arrange
        var tglPakai = new DateTime(2026, 10, 20);
        var tglVoid = new DateTime(2026, 10, 21);
        var item = PakaiBrgItemModel.Rehydrate(
            1, SampleBrg(), SampleSatuan(), 15, 4, "BATCH99", new DateTime(2028, 1, 1), 7500, 30000, "Item note", isVoid: true);

        // Act
        var doc = PakaiBrgModel.Rehydrate(
            "PK99999999", tglPakai, SampleLayanan(), "user1", "Ket pakai", isVoid: true, tglVoid, "userVoid", "Alasan void", [item]);

        // Assert
        doc.PakaiBrgId.Should().Be("PK99999999");
        doc.TglPakai.Should().Be(tglPakai);
        doc.IsVoid.Should().BeTrue();
        doc.TglVoid.Should().Be(tglVoid);
        doc.PetugasVoidId.Should().Be("userVoid");
        doc.AlasanVoid.Should().Be("Alasan void");
        doc.TotalNilai.Should().Be(30000);
        doc.ListItem[0].IsVoid.Should().BeTrue();
    }

    [Fact]
    public void GivenKeyMethod_WhenCalled_ThenReturnsProperIPakaiBrgKey()
    {
        var key = PakaiBrgModel.Key("PK12345678");
        key.PakaiBrgId.Should().Be("PK12345678");
    }

    [Fact]
    public void AC06_NonPatientOperationalBurden_NoPatientOrRegistrationAttributesInAggregate()
    {
        // Arrange
        var forbiddenTokens = new[] { "Reg", "Pasien", "Patient" };
        var types = new[] { typeof(PakaiBrgModel), typeof(PakaiBrgItemModel), typeof(IPakaiBrgKey) };

        // Act & Assert
        foreach (var type in types)
        {
            var members = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Select(m => m.Name)
                .ToList();

            foreach (var token in forbiddenTokens)
            {
                members.Where(m => m.Contains(token, StringComparison.OrdinalIgnoreCase))
                    .Should().BeEmpty(because: $"{type.Name} must not contain {token} references (AC-06 Non-Patient Operational Burden)");
            }
        }
    }
}
