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
                TglLahir: "1981-05-12",
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
                TglLahir: "1996-10-24",
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
    public void BuildOverview_WithTitipKelasFromKelas2ToKelas3_FlagsCorrectlyWithBillingKelas()
    {
        var bangsal = new RawBangsalRow("BGS-02", "Bangsal ST.RITA", "KLS-02", "Kelas II");

        var roomBedRows = new List<RawRoomBedRow>
        {
            new(
                RoomId: "ROOM-301",
                RoomName: "Kamar 301",
                KelasKamar: "Kelas III",
                BedId: "BED-301-A",
                BedCode: "301-A",
                IsBedActive: true,
                PakaiBedId: "PKB-301",
                RegId: "REG-301",
                MrNo: "MR-301",
                NamaPasien: "Budi Santoso",
                Gender: "L",
                TglLahir: "1985-05-10",
                TglIn: "2026-10-25",
                JamIn: "08:00:00",
                DpjpName: "dr. SpPD",
                PenjaminName: "BPJS",
                BillingKelasName: "Kelas II",
                TransaksiKelasId: "401",
                RegKelasId: "301",
                PakaiBedPurpose: 1
            )
        };

        var result = WardCockpitDal.BuildOverview(bangsal, roomBedRows, [], [], _now);
        var bed = result.Rooms.Single().Beds.Single();

        bed.CurrentOccupancy!.IsTitipKelas.Should().BeTrue();
        bed.CurrentOccupancy.BillingKelasName.Should().Be("Kelas II");
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
    public void BuildOverview_WhenBedIdHasTrailingSpacesOrCaseMismatch_MatchesOperationalStateAndResolvesCleaning()
    {
        var bangsal = new RawBangsalRow("BGS-01", "Bangsal Dahlia", "KLS-03", "Kelas III");

        var roomBedRows = new List<RawRoomBedRow>
        {
            new("ROOM-104", "Kamar 104", "Kelas III", "BED-104-A  ", "104-A", true, null, null, null, null, null, null, null, null, null, null, null, null, null, 0)
        };

        var opRows = new List<RawBedOperationalRow>
        {
            new(
                BedId: "bed-104-a",
                CurrentReadiness: 2, // CleaningRequired
                IsBlocked: false,
                CurrentRestrictionType: 1, // Cleaning
                CurrentBlockerReason: "Pindah kamar ke bed BED-104-B",
                UpdDate: _now.AddMinutes(-5)
            )
        };

        var result = WardCockpitDal.BuildOverview(bangsal, roomBedRows, opRows, [], _now);

        result.Kpi.Cleaning.Should().Be(1);
        result.Kpi.Available.Should().Be(0);

        var bed = result.Rooms.Single().Beds.Single();
        bed.Status.Should().Be("Cleaning");
        bed.OperationalState.ReadinessStatus.Should().Be("CleaningRequired");
        bed.OperationalState.BlockerType.Should().Be("Cleaning");
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
                TglLahir: "1974-03-10",
                ClinicalUrgency: 2, // Emergency / Cito
                ArrivalSourceType: 0, // Igd
                ReferringUnitName: "IGD Ponek",
                RequestedKelasName: "Kelas III",
                PlannedBedId: "BED-101-B",
                SpecialRequirements: "Perlu Oksigen Sentral",
                CrtDate: _now.AddMinutes(-45),
                KdJenisInap: "2",
                RegIbu: ""
            ),
            new(
                WaitingListId: "WL-002",
                RegId: "REG-011",
                MrNo: "MR-011",
                NamaPasien: "Dewi Lestari",
                Gender: "P",
                TglLahir: "2000-01-01",
                ClinicalUrgency: 1, // Urgent
                ArrivalSourceType: 1, // Rjl
                ReferringUnitName: "",
                RequestedKelasName: "Kelas III",
                PlannedBedId: "",
                SpecialRequirements: "",
                CrtDate: _now.AddMinutes(-120),
                KdJenisInap: "3",
                RegIbu: "REG-010"
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
        item1.ReferralSource.Should().Be("IGD");
        item1.ElapsedWaitMinutes.Should().Be(45);
        item1.Umur.Should().Be("52 Thn");
        item1.KdJenisInap.Should().Be("2");
        item1.RegIbu.Should().BeEmpty();

        var item2 = list[1];
        item2.WaitingListId.Should().Be("WL-002");
        item2.ClinicalUrgency.Should().Be("Urgent");
        item2.ReferralSource.Should().Be("Poliklinik");
        item2.ElapsedWaitMinutes.Should().Be(120);
        item2.KdJenisInap.Should().Be("3");
        item2.RegIbu.Should().Be("REG-010");
    }

    [Fact]
    public void BuildOverview_WithWaitingList_WhenKdJenisInapEmpty_DefaultsToUmum()
    {
        var bangsal = new RawBangsalRow("BGS-01", "Bangsal Dahlia", "KLS-03", "Kelas III");
        var wlRows = new List<RawWaitingListRow>
        {
            new(
                WaitingListId: "WL-003",
                RegId: "REG-012",
                MrNo: "MR-012",
                NamaPasien: "Pasien Umum",
                Gender: "L",
                TglLahir: "1990-01-01",
                ClinicalUrgency: 0,
                ArrivalSourceType: 0,
                ReferringUnitName: "",
                RequestedKelasName: "Kelas III",
                PlannedBedId: "",
                SpecialRequirements: "",
                CrtDate: _now.AddMinutes(-30),
                KdJenisInap: "",
                RegIbu: ""
            )
        };

        var result = WardCockpitDal.BuildOverview(bangsal, [], [], wlRows, _now);
        var item = result.WaitingList.Single();
        item.KdJenisInap.Should().Be("1");
        item.RegIbu.Should().BeEmpty();
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
            TglLahir: "1981-05-12",
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
                Biaya: 125000m,
                TindakanId: "TDK-001",
                HasJasaDokter: true,
                RegId: "REG-20261024-005",
                IsBayi: false
            ),
            new(
                TglJam: new DateTime(2026, 10, 23, 11, 0, 0),
                ItemTindakan: "Pemeriksaan Neonatus & Fototerapi",
                Petugas: "dr. Subroto, Sp.A",
                Biaya: 180000m,
                TindakanId: "TDK-002",
                HasJasaDokter: true,
                RegId: "REG-BABY-001",
                IsBayi: true
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

        result.TabTindakan.Should().HaveCount(2);
        var tdkIbu = result.TabTindakan.First();
        tdkIbu.TglJam.Should().Be(new DateTime(2026, 10, 23, 10, 0, 0));
        tdkIbu.ItemTindakan.Should().Be("Pemasangan Infus & Cairan RL");
        tdkIbu.Petugas.Should().Be("Ns. Ratna");
        tdkIbu.Biaya.Should().Be(125000m);
        tdkIbu.RegId.Should().Be("REG-20261024-005");
        tdkIbu.IsBayi.Should().BeFalse();

        var tdkBayi = result.TabTindakan.Last();
        tdkBayi.TglJam.Should().Be(new DateTime(2026, 10, 23, 11, 0, 0));
        tdkBayi.ItemTindakan.Should().Be("Pemeriksaan Neonatus & Fototerapi");
        tdkBayi.Petugas.Should().Be("dr. Subroto, Sp.A");
        tdkBayi.Biaya.Should().Be(180000m);
        tdkBayi.RegId.Should().Be("REG-BABY-001");
        tdkBayi.IsBayi.Should().BeTrue();

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
            TglLahir: "1995-08-20",
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

    [Fact]
    public void BuildOverview_WithRoomingInBaby_SetsIsRawatGabungTrueAndMapsBabyProperties()
    {
        var bangsal = new RawBangsalRow("BGS-01", "Bangsal ST. ANNA", "KLS-02", "Kelas II");
        var roomBedRows = new List<RawRoomBedRow>
        {
            new(
                RoomId: "ROOM-208",
                RoomName: "Kamar 208",
                KelasKamar: "Kelas II",
                BedId: "04008002",
                BedCode: "208-2",
                IsBedActive: true,
                PakaiBedId: "BE0174276A",
                RegId: "RG01376307",
                MrNo: "00-27-02-76",
                NamaPasien: "ENI MUNASIFAH.NY",
                Gender: "P",
                TglLahir: "1995-08-14",
                TglIn: "2024-06-05",
                JamIn: "14:00:00",
                DpjpName: "dr. Sp.OG",
                PenjaminName: "BPJS Kesehatan",
                BillingKelasName: "Kelas II",
                TransaksiKelasId: "KLS-02",
                RegKelasId: "KLS-02",
                PakaiBedPurpose: 4,
                BabyName: "BAYI NYONYA ENI MUNASIFAH",
                BabyRegId: "RG01376440",
                BabyMrNo: "00-27-04-12",
                BabyDpjpName: "dr. Tri Kusuma, Sp.A",
                BabyDpjpId: "DR00113"
            )
        };

        var result = WardCockpitDal.BuildOverview(bangsal, roomBedRows, [], [], _now);

        var bed = result.Rooms.Single().Beds.Single();
        bed.CurrentOccupancy.Should().NotBeNull();
        bed.CurrentOccupancy!.NamaPasien.Should().Be("ENI MUNASIFAH.NY");
        bed.CurrentOccupancy.IsRawatGabung.Should().BeTrue();
        bed.CurrentOccupancy.BabyName.Should().Be("BAYI NYONYA ENI MUNASIFAH");
        bed.CurrentOccupancy.BabyRegId.Should().Be("RG01376440");
        bed.CurrentOccupancy.BabyMrNo.Should().Be("00-27-04-12");
        bed.CurrentOccupancy.BabyDpjpName.Should().Be("dr. Tri Kusuma, Sp.A");
        bed.CurrentOccupancy.BabyDpjpId.Should().Be("DR00113");
    }

    [Fact]
    public void BuildBedDetail_WithRoomingInBaby_SetsIsRawatGabungTrueAndMapsBabyProperties()
    {
        var bedRow = new RawBedDetailRow(
            BedId: "04008002",
            BedCode: "208-2",
            RoomName: "Kamar 208",
            KelasKamar: "Kelas II",
            PakaiBedId: "BE0174276A",
            RegId: "RG01376307",
            MrNo: "00-27-02-76",
            NamaPasien: "ENI MUNASIFAH.NY",
            Gender: "P",
            TglLahir: "1995-08-14",
            TglIn: "2024-06-05",
            JamIn: "14:00:00",
            TarifPerHari: 300000m,
            TipeJaminanId: "TJ-BPJS",
            PenjaminName: "BPJS Kesehatan",
            DokterId: "D-01",
            DpjpName: "dr. Sp.OG",
            BillingKelasName: "Kelas II",
            BabyName: "BAYI NYONYA ENI MUNASIFAH",
            BabyRegId: "RG01376440",
            BabyMrNo: "00-27-04-12",
            BabyDpjpName: "dr. Tri Kusuma, Sp.A",
            BabyDpjpId: "DR00113"
        );

        var result = WardCockpitDal.BuildBedDetail(bedRow, "00012345678", [], [], _now);

        result.Occupancy.Should().NotBeNull();
        result.Occupancy!.Patient.NamaPasien.Should().Be("ENI MUNASIFAH.NY");
        result.Occupancy.IsRawatGabung.Should().BeTrue();
        result.Occupancy.BabyName.Should().Be("BAYI NYONYA ENI MUNASIFAH");
        result.Occupancy.BabyRegId.Should().Be("RG01376440");
        result.Occupancy.BabyMrNo.Should().Be("00-27-04-12");
        result.Occupancy.BabyDpjpName.Should().Be("dr. Tri Kusuma, Sp.A");
        result.Occupancy.BabyDpjpId.Should().Be("DR00113");
        result.TabRoomCharge.Should().NotBeNull();
        result.TabRoomCharge!.TarifPerHari.Should().Be(300000m);
    }

    [Fact]
    public void BuildOverview_WithKdJenisInap_MapsToOccupancyDto()
    {
        var bangsal = new RawBangsalRow("BGS-01", "Bangsal Melati", "KLS-01", "Kelas I");
        var roomBedRows = new List<RawRoomBedRow>
        {
            new(
                RoomId: "ROOM-101",
                RoomName: "Kamar 101",
                KelasKamar: "Kelas I",
                BedId: "BED-101",
                BedCode: "101",
                IsBedActive: true,
                PakaiBedId: "PKB-001",
                RegId: "REG-001",
                MrNo: "MR-001",
                NamaPasien: "Ibu Melahirkan",
                Gender: "P",
                TglLahir: "1990-01-01",
                TglIn: "2026-10-24",
                JamIn: "08:00:00",
                DpjpName: "dr. SpOG",
                PenjaminName: "Umum",
                BillingKelasName: "Kelas I",
                TransaksiKelasId: "KLS-01",
                RegKelasId: "KLS-01",
                PakaiBedPurpose: 1,
                KdJenisInap: "2"
            )
        };

        var result = WardCockpitDal.BuildOverview(bangsal, roomBedRows, [], [], _now);

        var bed = result.Rooms.First().Beds.First();
        bed.CurrentOccupancy.Should().NotBeNull();
        bed.CurrentOccupancy!.KdJenisInap.Should().Be("2");
    }

    [Fact]
    public void BuildBedDetail_WithKdJenisInap_MapsToOccupancyDto()
    {
        var bedRow = new RawBedDetailRow(
            BedId: "BED-01",
            BedCode: "01",
            RoomName: "Kamar Kebidanan",
            KelasKamar: "Kelas I",
            PakaiBedId: "PKB-001",
            RegId: "REG-001",
            MrNo: "MR-001",
            NamaPasien: "Ibu Melahirkan",
            Gender: "P",
            TglLahir: "1990-01-01",
            TglIn: "2026-10-24",
            JamIn: "08:00:00",
            TarifPerHari: 250000m,
            TipeJaminanId: "TJ-01",
            PenjaminName: "Umum",
            DokterId: "D-01",
            DpjpName: "dr. SpOG",
            BillingKelasName: "Kelas I",
            KdJenisInap: "2"
        );

        var result = WardCockpitDal.BuildBedDetail(bedRow, null, [], [], _now);

        result.Occupancy.Should().NotBeNull();
        result.Occupancy!.KdJenisInap.Should().Be("2");
    }

    [Fact]
    public void BuildOverview_WithDynamicTariffParameters_MapsLayananKelasAndTipeTarifAccurately()
    {
        var bangsal = new RawBangsalRow("BGS-01", "Bangsal Teratai", "KLS-02", "Kelas II", LayananId: "RI001");
        var roomBedRows = new List<RawRoomBedRow>
        {
            new(
                RoomId: "ROOM-101",
                RoomName: "Kamar 101",
                KelasKamar: "Kelas II",
                BedId: "BED-101-A",
                BedCode: "101-A",
                IsBedActive: true,
                PakaiBedId: "PKB-101",
                RegId: "REG-101",
                MrNo: "MR-101",
                NamaPasien: "Pasien Rawat",
                Gender: "L",
                TglLahir: "1980-01-01",
                TglIn: "2026-10-24",
                JamIn: "08:00:00",
                DpjpName: "dr. SpPD",
                PenjaminName: "BPJS",
                BillingKelasName: "Kelas II",
                TransaksiKelasId: "KL2",
                RegKelasId: "KL2",
                PakaiBedPurpose: 1,
                LayananId: "RI001",
                KelasId: "KL2",
                TipeTarifId: "KP"
            )
        };

        var result = WardCockpitDal.BuildOverview(bangsal, roomBedRows, [], [], _now);

        result.Bangsal.LayananId.Should().Be("RI001");
        var bed = result.Rooms.Single().Beds.Single();
        bed.CurrentOccupancy.Should().NotBeNull();
        bed.CurrentOccupancy!.LayananId.Should().Be("RI001");
        bed.CurrentOccupancy.KelasId.Should().Be("KL2");
        bed.CurrentOccupancy.TipeTarifId.Should().Be("KP");
    }

    [Fact]
    public void BuildOverview_WhenRoomBedRowLayananIdEmpty_FallsBackToBangsalLayananIdAndDefaultsTipeTarif()
    {
        var bangsal = new RawBangsalRow("BGS-02", "Bangsal Melati", "KLS-01", "Kelas I", LayananId: "RI002");
        var roomBedRows = new List<RawRoomBedRow>
        {
            new(
                RoomId: "ROOM-201",
                RoomName: "Kamar 201",
                KelasKamar: "Kelas I",
                BedId: "BED-201-A",
                BedCode: "201-A",
                IsBedActive: true,
                PakaiBedId: "PKB-201",
                RegId: "REG-201",
                MrNo: "MR-201",
                NamaPasien: "Pasien VIP",
                Gender: "P",
                TglLahir: "1992-05-10",
                TglIn: "2026-10-24",
                JamIn: "09:00:00",
                DpjpName: "dr. SpJP",
                PenjaminName: "Umum",
                BillingKelasName: "Kelas I",
                TransaksiKelasId: "VIP",
                RegKelasId: "VIP",
                PakaiBedPurpose: 1,
                LayananId: "",
                KelasId: "VIP",
                TipeTarifId: ""
            )
        };

        var result = WardCockpitDal.BuildOverview(bangsal, roomBedRows, [], [], _now);

        result.Bangsal.LayananId.Should().Be("RI002");
        var bed = result.Rooms.Single().Beds.Single();
        bed.CurrentOccupancy.Should().NotBeNull();
        bed.CurrentOccupancy!.LayananId.Should().Be("RI002");
        bed.CurrentOccupancy.KelasId.Should().Be("VIP");
        bed.CurrentOccupancy.TipeTarifId.Should().Be("UM");
    }

    [Fact]
    public void BuildBedDetail_WithDynamicTariffParameters_MapsBedInfoAndOccupancyAccurately()
    {
        var bedRow = new RawBedDetailRow(
            BedId: "01003001",
            BedCode: "103-1",
            RoomName: "Kamar 103",
            KelasKamar: "Kelas II",
            PakaiBedId: "PKB-301",
            RegId: "REG-301",
            MrNo: "MR-301",
            NamaPasien: "Pasien Operasi",
            Gender: "L",
            TglLahir: "1978-03-15",
            TglIn: "2026-10-24",
            JamIn: "07:30:00",
            TarifPerHari: 350000m,
            TipeJaminanId: "TJ-BPJS",
            PenjaminName: "BPJS Kesehatan",
            DokterId: "DOK-03",
            DpjpName: "dr. SpB",
            BillingKelasName: "Kelas II",
            LayananId: "RI001",
            BangsalId: "01",
            KamarKelasId: "KL2",
            OccupancyKelasId: "VIP",
            KelasId: "KL2",
            TipeTarifId: "PR"
        );

        var result = WardCockpitDal.BuildBedDetail(bedRow, "00098765432", [], [], _now);

        result.Should().NotBeNull();
        result.BedInfo.LayananId.Should().Be("RI001");
        result.BedInfo.BangsalId.Should().Be("01");
        result.BedInfo.KelasId.Should().Be("KL2");
        result.Occupancy.Should().NotBeNull();
        result.Occupancy!.LayananId.Should().Be("RI001");
        result.Occupancy.KelasId.Should().Be("VIP");
        result.Occupancy.TipeTarifId.Should().Be("PR");
    }

    [Fact]
    public void BuildBedDetail_WhenOccupancyKelasIdEmpty_FallsBackToKamarKelasIdAndDefaultsTipeTarif()
    {
        var bedRow = new RawBedDetailRow(
            BedId: "02001001",
            BedCode: "201-1",
            RoomName: "Kamar 201",
            KelasKamar: "Kelas I",
            PakaiBedId: "PKB-401",
            RegId: "REG-401",
            MrNo: "MR-401",
            NamaPasien: "Pasien Umum",
            Gender: "P",
            TglLahir: "1988-12-01",
            TglIn: "2026-10-24",
            JamIn: "08:15:00",
            TarifPerHari: 400000m,
            TipeJaminanId: "TJ-UM",
            PenjaminName: "Umum",
            DokterId: "DOK-04",
            DpjpName: "dr. SpA",
            BillingKelasName: "Kelas I",
            LayananId: "RI002",
            BangsalId: "02",
            KamarKelasId: "KL1",
            OccupancyKelasId: "",
            KelasId: "",
            TipeTarifId: ""
        );

        var result = WardCockpitDal.BuildBedDetail(bedRow, null, [], [], _now);

        result.Should().NotBeNull();
        result.BedInfo.LayananId.Should().Be("RI002");
        result.BedInfo.BangsalId.Should().Be("02");
        result.BedInfo.KelasId.Should().Be("KL1");
        result.Occupancy.Should().NotBeNull();
        result.Occupancy!.LayananId.Should().Be("RI002");
        result.Occupancy.KelasId.Should().Be("KL1");
        result.Occupancy.TipeTarifId.Should().Be("UM");
    }

    [Fact]
    public void BuildBedDetail_WithUnoccupiedBed_PreservesBedInfoDynamicTariffFields()
    {
        var bedRow = new RawBedDetailRow(
            BedId: "03001001",
            BedCode: "301-1",
            RoomName: "Kamar VIP",
            KelasKamar: "VIP",
            PakaiBedId: null,
            RegId: null,
            MrNo: null,
            NamaPasien: null,
            Gender: null,
            TglLahir: null,
            TglIn: null,
            JamIn: null,
            TarifPerHari: 800000m,
            TipeJaminanId: null,
            PenjaminName: null,
            DokterId: null,
            DpjpName: null,
            BillingKelasName: null,
            LayananId: "RI003",
            BangsalId: "03",
            KamarKelasId: "VIP",
            OccupancyKelasId: "",
            KelasId: "",
            TipeTarifId: "UM"
        );

        var result = WardCockpitDal.BuildBedDetail(bedRow, null, [], [], _now);

        result.Should().NotBeNull();
        result.BedInfo.LayananId.Should().Be("RI003");
        result.BedInfo.BangsalId.Should().Be("03");
        result.BedInfo.KelasId.Should().Be("VIP");
        result.Occupancy.Should().BeNull();
    }
}
