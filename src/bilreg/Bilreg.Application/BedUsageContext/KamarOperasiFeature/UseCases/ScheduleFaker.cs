namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public static class ScheduleFaker
{
    private static readonly List<FakeSchedule> Data = CreateFakeData();

    public static IEnumerable<OkListScheduleResponse> Generate(string tglYmd)
    {
        return Data
            .Where(x => x.TanggalOp == tglYmd)
            .Select(x => new OkListScheduleResponse(
                x.PasienId,
                x.PasienName,
                x.TglLahir,
                x.NamaOperasi,
                x.Urgency,
                x.Durasi,
                x.StartTime,   
                x.DokterId,
                x.DokterName,
                x.KamarId,
                x.KamarName
            ));
    }

    // INTERNAL MODEL
    private record FakeSchedule(
        string TanggalOp, 
        string PasienId, string PasienName, string TglLahir,
        string NamaOperasi, string Urgency, 
        string DokterId, string DokterName,
        int Durasi, string StartTime, 
        string KamarId, string KamarName
    );

    private static List<FakeSchedule> CreateFakeData()
    {
        return
        [
            //  2025-12-02
            new FakeSchedule("2025-12-01", "OPSCH001", "Agus Budiman", "2000-03-22", "Appendectomy", "Urgent", "D001",
                "dr. Surya, Sp.B", 60, "07:30", "OK001", "KAMAR OPERASI I"),
            new FakeSchedule("2025-12-01", "OPSCH002", "Budi Santoso", "1988-11-12", "Cholecystectomy", "Elective",
                "D002", "dr. Melati, Sp.B", 120, "08:00", "OK002", "KAMAR OPERASI II"),
            new FakeSchedule("2025-12-01", "OPSCH003", "Citra Lestari", "1995-04-10", "Caesar Section", "Emergency",
                "D010", "dr. Patricia, Sp.OG", 90, "09:15", "OK003", "KAMAR OPERASI III"),
            new FakeSchedule("2025-12-01", "OPSCH004", "Dedi Pratama", "1977-07-19", "Laparotomy", "Urgent", "D003",
                "dr. Joko, Sp.B", 180, "10:00", "OK004", "KAMAR OPERASI IV"),
            new FakeSchedule("2025-12-01", "OPSCH005", "Erika Meliana", "1992-02-15", "Tonsillectomy", "Elective",
                "D020", "dr. Arman, Sp.THT", 45, "11:30", "OK001", "KAMAR OPERASI I"),
            new FakeSchedule("2025-12-01", "OPSCH006", "Fajar Hidayat", "1983-09-25", "Fracture Fixation", "Urgent",
                "D004", "dr. Rudi, Sp.OT", 150, "12:15", "OK002", "KAMAR OPERASI II"),
            new FakeSchedule("2025-12-01", "OPSCH007", "Gita Sari", "2001-01-17", "Tumor Excision", "Elective", "D005",
                "dr. Bianca, Sp.B(K)", 140, "13:30", "OK003", "KAMAR OPERASI III"),

            // 2025-12-02
            new FakeSchedule("2025-12-02", "OPSCH011", "Kevin Hartono", "1985-06-14", "Fracture Fixation", "Urgent",
                "D004", "dr. Rudi, Sp.OT", 160, "07:45", "OK001", "KAMAR OPERASI I"),
            new FakeSchedule("2025-12-02", "OPSCH012", "Linda Marpaung", "1973-09-02", "Laparotomy", "Elective", "D002",
                "dr. Melati, Sp.B", 170, "08:30", "OK002", "KAMAR OPERASI II"),
            new FakeSchedule("2025-12-02", "OPSCH013", "Michael Adrian", "2002-10-10", "Tonsillectomy", "Elective",
                "D020", "dr. Arman, Sp.THT", 50, "09:00", "OK003", "KAMAR OPERASI III"),
            new FakeSchedule("2025-12-02", "OPSCH014", "Nadia Rosalin", "1999-01-01", "Caesar Section", "Emergency",
                "D010", "dr. Patricia, Sp.OG", 95, "10:30", "OK004", "KAMAR OPERASI IV"),

            // 2025-12-03
            new FakeSchedule("2025-12-03", "OPSCH020", "Yulia Karina", "1997-07-07", "Cholecystectomy", "Elective",
                "D001", "dr. Surya, Sp.B", 130, "07:00", "OK001", "KAMAR OPERASI I"),
            new FakeSchedule("2025-12-03", "OPSCH021", "Agus Kurniawan", "1972-09-14", "Laparotomy", "Emergency",
                "D003", "dr. Joko, Sp.B", 190, "08:30", "OK002", "KAMAR OPERASI II"),
            new FakeSchedule("2025-12-03", "OPSCH022", "Bella Angeline", "2003-11-20", "Tonsillectomy", "Elective",
                "D020", "dr. Arman, Sp.THT", 55, "09:15", "OK003", "KAMAR OPERASI III"),

            // 2025-12-04
            new FakeSchedule("2025-12-04", "OPSCH028", "Hendra Saputra", "1983-09-13", "Appendectomy", "Urgent", "D003",
                "dr. Joko, Sp.B", 65, "07:45", "OK001", "KAMAR OPERASI I"),
            new FakeSchedule("2025-12-04", "OPSCH029", "Indra Prabowo", "1975-05-17", "Cholecystectomy", "Elective",
                "D002", "dr. Melati, Sp.B", 125, "08:30", "OK002", "KAMAR OPERASI II"),

            // 2025-12-05
            new FakeSchedule("2025-12-05", "OPSCH036", "Putri Wulandari", "1990-07-07", "Appendectomy", "Urgent",
                "D002", "dr. Melati, Sp.B", 70, "07:15", "OK001", "KAMAR OPERASI I"),
            new FakeSchedule("2025-12-05", "OPSCH037", "Rangga Haryanto", "1978-02-14", "Fracture Fixation", "Urgent",
                "D004", "dr. Rudi, Sp.OT", 155, "08:00", "OK002", "KAMAR OPERASI II"),
            new FakeSchedule("2025-12-05", "OPSCH038", "Sari Dewi", "1985-09-30", "Cholecystectomy", "Elective", "D001",
                "dr. Surya, Sp.B", 135, "09:20", "OK003", "KAMAR OPERASI III")
        ];
    }
}