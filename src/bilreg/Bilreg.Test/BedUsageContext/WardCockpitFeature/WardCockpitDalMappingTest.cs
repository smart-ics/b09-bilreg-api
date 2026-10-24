using Bilreg.Application.BedUsageContext.WardCockpitFeature;
using Bilreg.Infrastructure.BedUsageContext.WardCockpitFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.BedUsageContext.WardCockpitFeature;

public class WardCockpitDalMappingTest
{
    private readonly DateTime _now = new(2026, 10, 24, 10, 0, 0);

    [Fact]
    public void BuildOverview_WithEmptyData_ReturnsZeroKpis()
    {
        var bangsal = new RawBangsalRow("BGS-01", "Bangsal Dahlia", "KLS-03", "Kelas III");

        var result = WardCockpitDal.BuildOverview(bangsal, [], [], [], _now);

        result.Bangsal.BangsalId.Should().Be("BGS-01");
        result.Bangsal.BangsalName.Should().Be("Bangsal Dahlia");
        result.Bangsal.KelasId.Should().Be("KLS-03");
        result.Bangsal.KelasName.Should().Be("Kelas III");
        result.Kpi.TotalBed.Should().Be(0);
        result.Kpi.Occupied.Should().Be(0);
        result.Kpi.Available.Should().Be(0);
        result.Kpi.Cleaning.Should().Be(0);
        result.Kpi.Maintenance.Should().Be(0);
        result.Kpi.BorPercentage.Should().Be(0.0);
        result.Kpi.WaitingListCount.Should().Be(0);
        result.Rooms.Should().BeEmpty();
        result.WaitingList.Should().BeEmpty();
    }

    [Fact]
    public void BuildOverview_WithActiveOccupancy_CalculatesLosAndAgeAndGender()
    {
        var bangsal = new RawBangsalRow("BGS-01", "Bangsal Dahlia", "KLS-03", "Kelas III");

        var roomBedRows = new List<RawRoomBedRow>
        {
            new(
                RoomId: "ROOM-101",
                RoomName: "Kamar 101",
                KelasKamar: "Kelas III",
                BedId: "BED-101-A",
                BedCode: "101-A",
                IsBedActive: true,
                PakaiBedId: "PKB-001",
                RegId: "REG-001",
                MrNo: "MR-001",
                NamaPasien: "Ahmad Wijaya",
                Gender: "L",
                TglLahir: new DateTime(1981, 5, 12),
                TglIn: "2026-10-21",
                JamIn: "08:30:00",
                DpjpName: "dr. Sutomo, Sp.PD",
                PenjaminName: "BPJS Kesehatan",
                BillingKelasName: "Kelas III",
                TransaksiKelasId: "KLS-03",
                RegKelasId: "KLS-03",
                PakaiBedPurpose: 1
            )
        };

        var result = WardCockpitDal.BuildOverview(bangsal, roomBedRows, [], [], _now);

        result.Kpi.TotalBed.Should().Be(1);
        result.Kpi.Occupied.Should().Be(1);
        result.Kpi.Available.Should().Be(0);
        result.Kpi.BorPercentage.Should().Be(100.0);

        var room = result.Rooms.Should().ContainSingle().Subject;
        room.RoomId.Should().Be("ROOM-101");
        room.RoomName.Should().Be("Kamar 101");
        room.GenderRestriction.Should().Be("Male");

        var bed = room.Beds.Should().ContainSingle().Subject;
        bed.BedId.Should().Be("BED-101-A");
        bed.Status.Should().Be("Occupied");
        bed.CurrentOccupancy.Should().NotBeNull();
        bed.CurrentOccupancy!.PakaiBedId.Should().Be("PKB-001");
        bed.CurrentOccupancy.NamaPasien.Should().Be("Ahmad Wijaya");
        bed.CurrentOccupancy.Umur.Should().Be("45 Thn");
        bed.CurrentOccupancy.LosDays.Should().Be(3);
        bed.CurrentOccupancy.IsTitipKelas.Should().BeFalse();
        bed.CurrentOccupancy.IsRawatGabung.Should().BeFalse();
    }

    [Fact]
    public void BuildOverview_WithTitipKelasAndRawatGabung_FlagsCorrectly()
    {
        var bangsal = new RawBangsalRow("BGS-01", "Bangsal Dahlia", "KLS-03", "Kelas III");

        var roomBedRows = new List<RawRoomBedRow>
        {
            new(
                RoomId: "ROOM-102",
                RoomName: "Kamar 102",
                KelasKamar: "Kelas III",
                BedId: "BED-102-A",
                BedCode: "102-A",
                IsBedActive: true,
                PakaiBedId: "PKB-002",
                RegId: "REG-002",
                MrNo: "MR-002",
                NamaPasien: "Siti Rahma",
                Gender: "P",
                TglLahir: new DateTime(1996, 10, 24),
                TglIn: "2026-10-24",
                JamIn: "06:00:00",
                DpjpName: "dr. SpOG",
                PenjaminName: "Umum",
                BillingKelasName: "Kelas II",
                TransaksiKelasId: "KLS-03",
                RegKelasId: "KLS-02", // Titip kelas (KLS-03 vs KLS-02)
                PakaiBedPurpose: 4     // Rooming-in
            )
        };

        var result = WardCockpitDal.BuildOverview(bangsal, roomBedRows, [], [], _now);

        var room = result.Rooms.Should().ContainSingle().Subject;
        room.GenderRestriction.Should().Be("Female");

        var bed = room.Beds.Should().ContainSingle().Subject;
        bed.Status.Should().Be("Occupied");
        bed.CurrentOccupancy!.IsTitipKelas.Should().BeTrue();
        bed.CurrentOccupancy.IsRawatGabung.Should().BeTrue();
        bed.CurrentOccupancy.Umur.Should().Be("30 Thn");
        bed.CurrentOccupancy.LosDays.Should().Be(0);
    }

    [Fact]
    public void BuildOverview_WithBedOperationalStates_ResolvesCleaningAndMaintenance()
    {
        var bangsal = new RawBangsalRow("BGS-01", "Bangsal Dahlia", "KLS-03", "Kelas III");

        var roomBedRows = new List<RawRoomBedRow>
        {
            new("ROOM-103", "Kamar 103", "Kelas III", "BED-103-A", "103-A", true, null, null, null, null, null, null, null, null, null, null, null, null, null, 0),
            new("ROOM-103", "Kamar 103", "Kelas III", "BED-103-B", "103-B", true, null, null, null, null, null, null, null, null, null, null, null, null, null, 0),
            new("ROOM-103", "Kamar 103", "Kelas III", "BED-103-C", "103-C", true, null, null, null, null, null, null, null, null, null, null, null, null, null, 0)
        };

        var opRows = new List<RawBedOperationalRow>
        {
            // BED-103-A: CleaningRequired
            new(
                BedId: "BED-103-A",
                CurrentReadiness: 2, // CleaningRequired
                IsBlocked: false,
                CurrentRestrictionType: 1, // Cleaning
                CurrentBlockerReason: null,
                UpdDate: _now.AddMinutes(-30)
            ),
            // BED-103-B: Blocked / Maintenance
            new(
                BedId: "BED-103-B",
                CurrentReadiness: 4, // Blocked
                IsBlocked: true,
                CurrentRestrictionType: 3, // Maintenance
                CurrentBlockerReason: "Perbaikan pipa oksigen",
                UpdDate: _now.AddHours(-2)
            )
            // BED-103-C: No entry -> Available
        };

        var result = WardCockpitDal.BuildOverview(bangsal, roomBedRows, opRows, [], _now);

        result.Kpi.TotalBed.Should().Be(3);
        result.Kpi.Occupied.Should().Be(0);
        result.Kpi.Cleaning.Should().Be(1);
        result.Kpi.Maintenance.Should().Be(1);
        result.Kpi.Available.Should().Be(1);

        var beds = result.Rooms.Single().Beds.ToList();
        var bedA = beds.Single(x => x.BedId == "BED-103-A");
        bedA.Status.Should().Be("Cleaning");
        bedA.OperationalState.ReadinessStatus.Should().Be("CleaningRequired");
        bedA.OperationalState.BlockerType.Should().Be("Cleaning");
        bedA.OperationalState.DirtySince.Should().Be(_now.AddMinutes(-30));

        var bedB = beds.Single(x => x.BedId == "BED-103-B");
        bedB.Status.Should().Be("Maintenance");
        bedB.OperationalState.ReadinessStatus.Should().Be("Blocked");
        bedB.OperationalState.BlockerType.Should().Be("Maintenance");
        bedB.OperationalState.MaintenanceNote.Should().Be("Perbaikan pipa oksigen");

        var bedC = beds.Single(x => x.BedId == "BED-103-C");
        bedC.Status.Should().Be("Available");
        bedC.OperationalState.ReadinessStatus.Should().Be("Ready");
        bedC.OperationalState.BlockerType.Should().Be("None");
    }

    [Fact]
    public void BuildOverview_WithWaitingList_MapsCitoUrgentAndWaitTime()
    {
        var bangsal = new RawBangsalRow("BGS-01", "Bangsal Dahlia", "KLS-03", "Kelas III");

        var wlRows = new List<RawWaitingListRow>
        {
            new(
                WaitingListId: "WL-001",
                RegId: "REG-010",
                MrNo: "MR-010",
                NamaPasien: "Budi Santoso",
                Gender: "L",
                TglLahir: new DateTime(1974, 3, 10),
                ClinicalUrgency: 2, // Emergency / Cito
                ArrivalSourceType: 0, // Igd
                ReferringUnitName: "IGD Ponek",
                RequestedKelasName: "Kelas III",
                PlannedBedId: "BED-101-B",
                SpecialRequirements: "Perlu Oksigen Sentral",
                CrtDate: _now.AddMinutes(-45)
            ),
            new(
                WaitingListId: "WL-002",
                RegId: "REG-011",
                MrNo: "MR-011",
                NamaPasien: "Dewi Lestari",
                Gender: "P",
                TglLahir: new DateTime(2000, 1, 1),
                ClinicalUrgency: 1, // Urgent
                ArrivalSourceType: 1, // Rjl
                ReferringUnitName: "",
                RequestedKelasName: "Kelas III",
                PlannedBedId: "",
                SpecialRequirements: "",
                CrtDate: _now.AddMinutes(-120)
            )
        };

        var result = WardCockpitDal.BuildOverview(bangsal, [], [], wlRows, _now);

        result.Kpi.WaitingListCount.Should().Be(2);

        var list = result.WaitingList.ToList();
        list.Should().HaveCount(2);

        var item1 = list[0];
        item1.WaitingListId.Should().Be("WL-001");
        item1.NamaPasien.Should().Be("Budi Santoso");
        item1.ClinicalUrgency.Should().Be("Cito");
        item1.ReferralSource.Should().Be("IGD Ponek");
        item1.ElapsedWaitMinutes.Should().Be(45);
        item1.Umur.Should().Be("52 Thn");

        var item2 = list[1];
        item2.WaitingListId.Should().Be("WL-002");
        item2.ClinicalUrgency.Should().Be("Urgent");
        item2.ReferralSource.Should().Be("Poliklinik");
        item2.ElapsedWaitMinutes.Should().Be(120);
    }

    [Fact]
    public void BuildBedDetail_WithOccupiedBed_MapsPatientPenjaminDpjpAndTabs()
    {
        var bedRow = new RawBedDetailRow(
            BedId: "BED-101-A",
            BedCode: "101-A",
            RoomName: "Kamar 101",
            KelasKamar: "Kelas III",
            PakaiBedId: "PKB-20261024-001",
            RegId: "REG-20261024-005",
            MrNo: "MR-001234",
            NamaPasien: "Ahmad Wijaya",
            Gender: "L",
            TglLahir: new DateTime(1981, 5, 12),
            TglIn: "2026-10-21",
            JamIn: "08:30:00",
            TarifPerHari: 250000m,
            TipeJaminanId: "BPJS",
            PenjaminName: "BPJS Kesehatan",
            DokterId: "DOK-01",
            DpjpName: "dr. Sutomo, Sp.PD",
            BillingKelasName: "Kelas III"
        );

        var tindakanRows = new List<RawTindakanRow>
        {
            new(
                TglJam: new DateTime(2026, 10, 23, 10, 0, 0),
                ItemTindakan: "Pemasangan Infus & Cairan RL",
                Petugas: "Ns. Ratna",
                Biaya: 125000m
            )
        };

        var mutasiRows = new List<RawMutasiRow>
        {
            new(
                TglIn: "2026-10-21",
                JamIn: "08:30:00",
                TglOut: null,
                JamOut: null,
                PetugasIn: "Ns. Ratna",
                PetugasOut: null,
                RoomName: "Kamar 101",
                BedCode: "101-A"
            )
        };

        var result = WardCockpitDal.BuildBedDetail(bedRow, "00012345678", tindakanRows, mutasiRows, _now);

        result.Should().NotBeNull();
        result.BedInfo.BedId.Should().Be("BED-101-A");
        result.BedInfo.BedCode.Should().Be("101-A");
        result.BedInfo.RoomName.Should().Be("Kamar 101");
        result.BedInfo.KelasKamar.Should().Be("Kelas III");

        result.Occupancy.Should().NotBeNull();
        result.Occupancy!.PakaiBedId.Should().Be("PKB-20261024-001");
        result.Occupancy.RegId.Should().Be("REG-20261024-005");
        result.Occupancy.CheckInTime.Should().Be(new DateTime(2026, 10, 21, 8, 30, 0));
        result.Occupancy.LosDays.Should().Be(3);
        result.Occupancy.BillingKelas.Should().Be("Kelas III");

        result.Occupancy.Patient.MrNo.Should().Be("MR-001234");
        result.Occupancy.Patient.NamaPasien.Should().Be("Ahmad Wijaya");
        result.Occupancy.Patient.Gender.Should().Be("Laki-laki");
        result.Occupancy.Patient.TglLahir.Should().Be("1981-05-12");
        result.Occupancy.Patient.Umur.Should().Be("45 Tahun");

        result.Occupancy.Penjamin.TipeJaminanId.Should().Be("BPJS");
        result.Occupancy.Penjamin.TipeJaminanName.Should().Be("BPJS Kesehatan");
        result.Occupancy.Penjamin.NoKartu.Should().Be("00012345678");

        result.Occupancy.Dpjp.DokterId.Should().Be("DOK-01");
        result.Occupancy.Dpjp.DokterName.Should().Be("dr. Sutomo, Sp.PD");

        result.TabTindakan.Should().HaveCount(1);
        var tdk = result.TabTindakan.First();
        tdk.TglJam.Should().Be(new DateTime(2026, 10, 23, 10, 0, 0));
        tdk.ItemTindakan.Should().Be("Pemasangan Infus & Cairan RL");
        tdk.Petugas.Should().Be("Ns. Ratna");
        tdk.Biaya.Should().Be(125000m);

        result.TabRoomCharge.Should().NotBeNull();
        result.TabRoomCharge!.TarifPerHari.Should().Be(250000m);
        result.TabRoomCharge.AkumulasiHari.Should().Be(3);
        result.TabRoomCharge.EstimasiTotalSewa.Should().Be(750000m);
        result.TabRoomCharge.CutOffTime.Should().Be("12:00");

        result.TabRiwayatMutasi.Should().HaveCount(1);
        var mut = result.TabRiwayatMutasi.First();
        mut.TglJam.Should().Be(new DateTime(2026, 10, 21, 8, 30, 0));
        mut.Kegiatan.Should().Be("Check-in Kamar 101 (101-A)");
        mut.Petugas.Should().Be("Ns. Ratna");
    }

    [Fact]
    public void BuildBedDetail_WithFemalePatientAndTransferMutation_MapsAccurately()
    {
        var bedRow = new RawBedDetailRow(
            BedId: "BED-202-B",
            BedCode: "202-B",
            RoomName: "Kamar 202",
            KelasKamar: "Kelas I",
            PakaiBedId: "PKB-002",
            RegId: "REG-002",
            MrNo: "MR-002",
            NamaPasien: "Siti Rahma",
            Gender: "P",
            TglLahir: new DateTime(1995, 8, 20),
            TglIn: "2026-10-24",
            JamIn: "06:00:00",
            TarifPerHari: 450000m,
            TipeJaminanId: "UMUM",
            PenjaminName: "Umum / Pribadi",
            DokterId: "DOK-02",
            DpjpName: "dr. Subroto, Sp.A",
            BillingKelasName: "Kelas I"
        );

        var mutasiRows = new List<RawMutasiRow>
        {
            new(
                TglIn: "2026-10-22",
                JamIn: "10:00:00",
                TglOut: "2026-10-24",
                JamOut: "05:45:00",
                PetugasIn: "Ns. Ratna",
                PetugasOut: "Ns. Budi",
                RoomName: "Kamar 101",
                BedCode: "101-A"
            ),
            new(
                TglIn: "2026-10-24",
                JamIn: "06:00:00",
                TglOut: null,
                JamOut: null,
                PetugasIn: "Ns. Budi",
                PetugasOut: null,
                RoomName: "Kamar 202",
                BedCode: "202-B"
            )
        };

        var result = WardCockpitDal.BuildBedDetail(bedRow, null, [], mutasiRows, _now);

        result.Occupancy.Should().NotBeNull();
        result.Occupancy!.Patient.Gender.Should().Be("Perempuan");
        result.Occupancy.Patient.Umur.Should().Be("31 Tahun");
        result.Occupancy.LosDays.Should().Be(0);
        result.TabRoomCharge.Should().NotBeNull();
        result.TabRoomCharge!.AkumulasiHari.Should().Be(1); // Min 1 day
        result.TabRoomCharge.EstimasiTotalSewa.Should().Be(450000m);

        // 1st row has check-in + check-out (2 events), 2nd row has check-in (1 event) = 3 total
        result.TabRiwayatMutasi.Should().HaveCount(3);
        var mutasiList = result.TabRiwayatMutasi.ToList();
        mutasiList[0].Kegiatan.Should().Be("Check-in Kamar 101 (101-A)");
        mutasiList[0].Petugas.Should().Be("Ns. Ratna");
        mutasiList[1].Kegiatan.Should().Be("Pindah/Keluar dari Kamar 101 (101-A)");
        mutasiList[1].Petugas.Should().Be("Ns. Budi");
        mutasiList[2].Kegiatan.Should().Be("Check-in Kamar 202 (202-B)");
        mutasiList[2].Petugas.Should().Be("Ns. Budi");
    }

    [Fact]
    public void BuildBedDetail_WithUnoccupiedBed_ReturnsNullOccupancyAndEmptyTabs()
    {
        var bedRow = new RawBedDetailRow(
            BedId: "BED-301-A",
            BedCode: "301-A",
            RoomName: "Kamar 301",
            KelasKamar: "VIP",
            PakaiBedId: null,
            RegId: null,
            MrNo: null,
            NamaPasien: null,
            Gender: null,
            TglLahir: null,
            TglIn: null,
            JamIn: null,
            TarifPerHari: 750000m,
            TipeJaminanId: null,
            PenjaminName: null,
            DokterId: null,
            DpjpName: null,
            BillingKelasName: null
        );

        var result = WardCockpitDal.BuildBedDetail(bedRow, null, [], [], _now);

        result.Should().NotBeNull();
        result.BedInfo.BedId.Should().Be("BED-301-A");
        result.BedInfo.BedCode.Should().Be("301-A");
        result.BedInfo.RoomName.Should().Be("Kamar 301");
        result.BedInfo.KelasKamar.Should().Be("VIP");
        result.Occupancy.Should().BeNull();
        result.TabTindakan.Should().BeEmpty();
        result.TabRoomCharge.Should().BeNull();
        result.TabRiwayatMutasi.Should().BeEmpty();
    }
}
