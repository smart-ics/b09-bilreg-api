using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using System.Data.SqlClient;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class OrderOpDalTest
{
    private readonly OrderOpDal _sut = new(ConnStringHelper.GetTestEnv());

    static OrderOpDalTest()
    {
        EnsureTableExists();
    }

    private static void EnsureTableExists()
    {
        var optDev = ConnStringHelper.GetTestEnv();
        using var conn = new SqlConnection(ConnStringHelper.Get(optDev.Value));
        const string sql = """
            IF OBJECT_ID('BILRG_OrderOp', 'U') IS NULL
            BEGIN
                CREATE TABLE BILRG_OrderOp
                (
                    OrderOpId VARCHAR(26) NOT NULL CONSTRAINT DF_BILRG_OrderOp_OrderOpId DEFAULT(''),
                    OrderDate DATETIME NOT NULL CONSTRAINT DF_BILRG_OrderOp_OrderDate DEFAULT('3000-01-01'),
                    PasienId VARCHAR(15) NOT NULL CONSTRAINT DF_BILRG_OrderOp_PasienId DEFAULT(''),
                    NamaOperasi VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_OrderOp_NamaOperasi DEFAULT(''),

                    RegId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_OrderOp_RegId DEFAULT(''),
                    Icd10Id VARCHAR(5) NOT NULL CONSTRAINT DF_BILRG_OrderOp_Icd10Id DEFAULT(''),
                    JenisOperasiId VARCHAR(2) NOT NULL CONSTRAINT DF_BILRG_OrderOp_JenisOperasiId DEFAULT(''),
                    UrgencyLevel INT NOT NULL CONSTRAINT DF_BILRG_OrderOp_UrgencyLevel DEFAULT(0),
                    TarifId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_OrderOp_TarifId DEFAULT(''),
                    TarifName VARCHAR(60) NOT NULL CONSTRAINT DF_BILRG_OrderOp_TarifName DEFAULT(''),

                    DokterId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_OrderOp_DokterId DEFAULT(''),
                    EstimasiDurasi INT NOT NULL CONSTRAINT DF_BILRG_OrderOp_EstimasiDurasi DEFAULT(0),
                    PreferedDate DATETIME NOT NULL CONSTRAINT DF_BILRG_OrderOp_PreferedDate DEFAULT('3000-01-01'),
                    SpecialEquipment VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_OrderOp_SpecialEquipment DEFAULT(''),

                    DokterPengorderId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_OrderOp_DokterPengorderId DEFAULT(''),
                    LayananPengorderId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_OrderOp_LayananPengorderId DEFAULT(''),
                    DokterOperatorId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_OrderOp_DokterOperatorId DEFAULT(''),
                    ProsedurTambahan VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_OrderOp_ProsedurTambahan DEFAULT(''),
                    KlasifikasiOperasi INT NOT NULL CONSTRAINT DF_BILRG_OrderOp_KlasifikasiOperasi DEFAULT(1),
                    AnestesiPlan INT NOT NULL CONSTRAINT DF_BILRG_OrderOp_AnestesiPlan DEFAULT(1),
                    PostOpCareUnit INT NOT NULL CONSTRAINT DF_BILRG_OrderOp_PostOpCareUnit DEFAULT(1),
                    AlasanBatal VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_OrderOp_AlasanBatal DEFAULT(''),

                    CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_OrderOp_CreateUserId DEFAULT(''),
                    CrtDate DATETIME NOT NULL CONSTRAINT DF_BILRG_OrderOp_CreateTimestamp DEFAULT('3000-01-01'),
                    UpdUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_OrderOp_UpdateUserId DEFAULT(''),
                    UpdDate DATETIME NOT NULL CONSTRAINT DF_BILRG_OrderOp_UpdateTimestamp DEFAULT('3000-01-01'),
                    VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_OrderOp_VoidUserId DEFAULT(''),
                    VodDate DATETIME NOT NULL CONSTRAINT DF_BILRG_OrderOp_VoidTimestamp DEFAULT('3000-01-01'),

                    CONSTRAINT PK_BILRG_OrderOp PRIMARY KEY CLUSTERED(OrderOpId)
                );
            END
            ELSE
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_OrderOp') AND name = 'DokterPengorderId')
                    ALTER TABLE BILRG_OrderOp ADD DokterPengorderId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_OrderOp_DokterPengorderId DEFAULT('');

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_OrderOp') AND name = 'LayananPengorderId')
                    ALTER TABLE BILRG_OrderOp ADD LayananPengorderId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_OrderOp_LayananPengorderId DEFAULT('');

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_OrderOp') AND name = 'DokterOperatorId')
                    ALTER TABLE BILRG_OrderOp ADD DokterOperatorId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_OrderOp_DokterOperatorId DEFAULT('');

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_OrderOp') AND name = 'ProsedurTambahan')
                    ALTER TABLE BILRG_OrderOp ADD ProsedurTambahan VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_OrderOp_ProsedurTambahan DEFAULT('');

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_OrderOp') AND name = 'KlasifikasiOperasi')
                    ALTER TABLE BILRG_OrderOp ADD KlasifikasiOperasi INT NOT NULL CONSTRAINT DF_BILRG_OrderOp_KlasifikasiOperasi DEFAULT(1);

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_OrderOp') AND name = 'AnestesiPlan')
                    ALTER TABLE BILRG_OrderOp ADD AnestesiPlan INT NOT NULL CONSTRAINT DF_BILRG_OrderOp_AnestesiPlan DEFAULT(1);

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_OrderOp') AND name = 'PostOpCareUnit')
                    ALTER TABLE BILRG_OrderOp ADD PostOpCareUnit INT NOT NULL CONSTRAINT DF_BILRG_OrderOp_PostOpCareUnit DEFAULT(1);

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_OrderOp') AND name = 'AlasanBatal')
                    ALTER TABLE BILRG_OrderOp ADD AlasanBatal VARCHAR(255) NOT NULL CONSTRAINT DF_BILRG_OrderOp_AlasanBatal DEFAULT('');
            END

            IF OBJECT_ID('tc_icd', 'U') IS NULL
                CREATE TABLE tc_icd (fs_kd_icd VARCHAR(10) PRIMARY KEY, fs_ket_icd VARCHAR(255) DEFAULT(''));
            IF OBJECT_ID('tc_mr', 'U') IS NULL
                CREATE TABLE tc_mr (fs_mr VARCHAR(15) PRIMARY KEY, fs_nm_pasien VARCHAR(100) DEFAULT(''), fd_tgl_lahir DATE DEFAULT('3000-01-01'), fs_jns_kelamin VARCHAR(2) DEFAULT(''));
            IF OBJECT_ID('ta_jenis_operasi', 'U') IS NULL
                CREATE TABLE ta_jenis_operasi (fs_kd_jenis_operasi VARCHAR(10) PRIMARY KEY, fs_nm_jenis_operasi VARCHAR(100) DEFAULT(''));
            IF OBJECT_ID('td_peg', 'U') IS NULL
                CREATE TABLE td_peg (fs_kd_peg VARCHAR(10) PRIMARY KEY, fs_nm_peg VARCHAR(100) DEFAULT(''));
            IF OBJECT_ID('ta_layanan', 'U') IS NULL
                CREATE TABLE ta_layanan (fs_kd_layanan VARCHAR(10) PRIMARY KEY, fs_nm_layanan VARCHAR(100) DEFAULT(''));
            """;
        try
        {
            conn.Execute(sql);
        }
        catch
        {
            // Ignore if execution environment handles DDL externally
        }
    }

    private static OrderOpDto Faker()
        => new OrderOpDto(
            OrderOpId: "A",
            OrderDate: new DateTime(2024, 1, 1, 10, 0, 0),
            RegId: "B",
            PasienId: "C",
            Icd10Id: "D",
            JenisOperasiId: "E",
            NamaOperasi: "F",
            DokterId: "G",
            UrgencyLevel: (int)UrgencyLevelEnum.Cito,
            TarifId: "X",
            TarifName: "Y",
            EstimasiDurasi: 120,
            PreferedDate: new DateTime(2024, 1, 2, 8, 0, 0),
            SpecialEquipment: "H",
            DokterPengorderId: "D1",
            LayananPengorderId: "L1",
            DokterOperatorId: "D2",
            ProsedurTambahan: "Tambahan",
            KlasifikasiOperasi: 2,
            AnestesiPlan: 1,
            PostOpCareUnit: 2,
            AlasanBatal: "Batal test",
            CrtUser: "I",
            CrtDate: new DateTime(2024, 1, 1, 10, 0, 0),
            UpdUser: "J",
            UpdDate: new DateTime(2024, 1, 1, 10, 0, 0),
            VodUser: "K",
            VodDate: new DateTime(3000, 1, 1),
            PasienName: "L",
            TglLahir: "2000-01-01",
            Gender: "M",
            fs_ket_icd: "N",
            fs_nm_jenis_operasi: "O",
            fs_nm_peg: "P",
            DokterPengorderName: "Q",
            LayananPengorderName: "R",
            DokterOperatorName: "S"
        );

    private static IOrderOpKey FakerKey()
        => OrderOpModel.Key("A");

    private static Periode FakerPeriode()
        => new Periode(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31));

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(Faker());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(FakerKey());
        actual.Should().BeEquivalentTo(Faker(),
            opt => opt.Excluding(x => x.PasienName)
                      .Excluding(x => x.TglLahir)
                      .Excluding(x => x.Gender)
                      .Excluding(x => x.fs_ket_icd)
                      .Excluding(x => x.fs_nm_jenis_operasi)
                      .Excluding(x => x.fs_nm_peg)
                      .Excluding(x => x.DokterPengorderName)
                      .Excluding(x => x.LayananPengorderName)
                      .Excluding(x => x.DokterOperatorName));
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var orderOp = Faker();
        _sut.Insert(orderOp);

        var actual = _sut.ListData(FakerPeriode());
        actual.Should().ContainEquivalentOf(orderOp,
            opt => opt.Excluding(x => x.PasienName)
                      .Excluding(x => x.TglLahir)
                      .Excluding(x => x.Gender)
                      .Excluding(x => x.fs_ket_icd)
                      .Excluding(x => x.fs_nm_jenis_operasi)
                      .Excluding(x => x.fs_nm_peg)
                      .Excluding(x => x.DokterPengorderName)
                      .Excluding(x => x.LayananPengorderName)
                      .Excluding(x => x.DokterOperatorName));
    }
}
