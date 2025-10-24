using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.BookingFeature;

public interface IBookingDal :
    IInsert<BookingDto>, 
    IUpdate<BookingDto>, 
    IDelete<IBookingKey>, 
    IGetData<BookingDto, IBookingKey>,
    IListData<BookingDto, Periode>
{
    IEnumerable<BookingDto> ListPerTglBerobat(Periode periode);
}

public class BookingDal : IBookingDal
{
    private readonly DatabaseOptions _opt;

    public BookingDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(BookingDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_Booking (
                BookingId, BookingDate,
                PasienName, TglLahir, Gender, Alamat,
                TglBerobat, JamPraktek, LayananId, DokterId, NoAntrian,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @BookingId, @BookingDate,
                @PasienName, @TglLahir, @Gender, @Alamat,
                @TglBerobat, @JamPraktek, @LayananId, @DokterId, @NoAntrian,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BookingId", dto.BookingId, SqlDbType.VarChar);
        dp.AddParam("@BookingDate", dto.BookingDate, SqlDbType.DateTime);
        
        dp.AddParam("@PasienName", dto.PasienName, SqlDbType.VarChar);
        dp.AddParam("@TglLahir", dto.TglLahir, SqlDbType.DateTime);
        dp.AddParam("@Gender", dto.Gender, SqlDbType.VarChar);
        dp.AddParam("@Alamat", dto.Alamat, SqlDbType.VarChar);
        
        dp.AddParam("@TglBerobat", dto.TglBerobat, SqlDbType.DateTime);
        dp.AddParam("@JamPraktek", dto.JamPraktek, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@DokterId", dto.DokterId, SqlDbType.VarChar);
        dp.AddParam("@NoAntrian", dto.NoAntrian, SqlDbType.Int);
        
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(BookingDto dto)
    {
        const string sql = """
           UPDATE BILRG_Booking 
           SET 
               BookingDate = @BookingDate,
               PasienName = @PasienName,
               TglLahir = @TglLahir,
               Gender = @Gender,
               Alamat = @Alamat,
               TglBerobat = @TglBerobat,
               JamPraktek = @JamPraktek,
               LayananId = @LayananId,
               DokterId = @DokterId,
               NoAntrian = @NoAntrian,
               CrtUser = @CrtUser,
               CrtDate = @CrtDate,
               UpdUser = @UpdUser,
               UpdDate = @UpdDate,
               VodUser = @VodUser,
               VodDate = @VodDate
           WHERE BookingId = @BookingId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@BookingId", dto.BookingId, SqlDbType.VarChar);
        dp.AddParam("@BookingDate", dto.BookingDate, SqlDbType.DateTime);
        
        dp.AddParam("@PasienName", dto.PasienName, SqlDbType.VarChar);
        dp.AddParam("@TglLahir", dto.TglLahir, SqlDbType.DateTime);
        dp.AddParam("@Gender", dto.Gender, SqlDbType.VarChar);
        dp.AddParam("@Alamat", dto.Alamat, SqlDbType.VarChar);
        
        dp.AddParam("@TglBerobat", dto.TglBerobat, SqlDbType.DateTime);
        dp.AddParam("@JamPraktek", dto.JamPraktek, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@DokterId", dto.DokterId, SqlDbType.VarChar);
        dp.AddParam("@NoAntrian", dto.NoAntrian, SqlDbType.Int);
        
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
    
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IBookingKey key)
    {
        const string sql = """
           DELETE BILRG_Booking 
           WHERE BookingId = @BookingId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@BookingId", key.BookingId, SqlDbType.VarChar);
    
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public BookingDto GetData(IBookingKey key)
    {
        const string sql = """
            SELECT
                aa.BookingId, aa.BookingDate,
                aa.PasienName, aa.TglLahir, aa.Gender, aa.Alamat,
                aa.TglBerobat, aa.JamPraktek, aa.LayananId, aa.DokterId, aa.NoAntrian,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate,
                ISNULL(bb.fs_nm_layanan, '') AS LayananName,
                ISNULL(cc.fs_nm_peg, '') AS DokterName
            FROM
                BILRG_Booking aa
                LEFT JOIN ta_layanan bb ON aa.LayananId = bb.fs_kd_layanan
                LEFT JOIN td_peg cc ON aa.DokterId = cc.fs_kd_peg
            WHERE
                aa.BookingId = @BookingId
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@BookingId", key.BookingId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<BookingDto>(sql, dp);
    }

    public IEnumerable<BookingDto> ListData(Periode filter)
    {
        const string sql = """
           SELECT
               aa.BookingId, aa.BookingDate,
               aa.PasienName, aa.TglLahir, aa.Gender, aa.Alamat,
               aa.TglBerobat, aa.JamPraktek, aa.LayananId, aa.DokterId, aa.NoAntrian,
               aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate,
               ISNULL(bb.fs_nm_layanan, '') AS LayananName,
               ISNULL(cc.fs_nm_peg, '') AS DokterName
           FROM
               BILRG_Booking aa
               LEFT JOIN ta_layanan bb ON aa.LayananId = bb.fs_kd_layanan
               LEFT JOIN td_peg cc ON aa.DokterId = cc.fs_kd_peg
           WHERE
               aa.BookingDate BETWEEN @Tgl1 AND @Tgl2
               AND aa.VodDate = @VodDate
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@Tgl1", filter.Tgl1, SqlDbType.DateTime);
        dp.AddParam("@Tgl2", filter.Tgl2, SqlDbType.DateTime);
        dp.AddParam("@VodDate", new DateTime(3000,1,1), SqlDbType.DateTime);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BookingDto>(sql, dp);
    }

    public IEnumerable<BookingDto> ListPerTglBerobat(Periode filter)
    {
        const string sql = """
            SELECT
               aa.BookingId, aa.BookingDate,
               aa.PasienName, aa.TglLahir, aa.Gender, aa.Alamat,
               aa.TglBerobat, aa.JamPraktek, aa.LayananId, aa.DokterId, aa.NoAntrian,
               aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate,
               ISNULL(bb.fs_nm_layanan, '') AS LayananName,
               ISNULL(cc.fs_nm_peg, '') AS DokterName
            FROM
               BILRG_Booking aa
               LEFT JOIN ta_layanan bb ON aa.LayananId = bb.fs_kd_layanan
               LEFT JOIN td_peg cc ON aa.DokterId = cc.fs_kd_peg
            WHERE
               aa.TglBerobat BETWEEN @Tgl1 AND @Tgl2
               AND aa.VodDate = @VodDate
            """;
            
        var dp = new DynamicParameters();
        dp.AddParam("@Tgl1", filter.Tgl1, SqlDbType.DateTime);
        dp.AddParam("@Tgl2", filter.Tgl2, SqlDbType.DateTime);
        dp.AddParam("@VodDate", new DateTime(3000,1,1), SqlDbType.DateTime);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BookingDto>(sql, dp);
    }
}

public class BookingDalTest
{
    private readonly BookingDal _sut = new(ConnStringHelper.GetTestEnv());

    private static BookingDto Faker()
        => new BookingDto(
            BookingId: "A",
            BookingDate: new DateTime(2024, 1, 1),
            PasienName: "B",
            TglLahir: new DateTime(2000, 1, 1),
            Gender: "C",
            Alamat: "D",
            TglBerobat: new DateTime(2024, 1, 2),
            JamPraktek: "E",
            LayananId: "F",
            DokterId: "G",
            NoAntrian: 1,
            CrtUser: "H",
            CrtDate: new DateTime(2024, 1, 1, 10, 0, 0),
            UpdUser: "I",
            UpdDate: new DateTime(2024, 1, 1, 10, 0, 0),
            VodUser: "J",
            VodDate: new DateTime(3000, 1, 1),
            LayananName: "K",
            DokterName: "L"
        );

    private static IBookingKey FakerKey()
        => BookingModel.Key("A");

    private static Periode FakerPeriode()
        => new Periode(new DateTime(2024, 1, 1),new DateTime(2024, 1, 31)
        );

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
            opt => opt
                .Excluding(x => x.DokterName)
                .Excluding(x => x.LayananName));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var booking = Faker();
        _sut.Insert(booking);
        
        var actual = _sut.ListData(FakerPeriode());
        actual.Should().ContainEquivalentOf(booking,
            opt => opt
                .Excluding(x => x.DokterName)
                .Excluding(x => x.LayananName));
    }

    [Fact]
    public void ListPerTglBerobatTest()
    {
        using var trans = TransHelper.NewScope();
        var booking = Faker();
        _sut.Insert(booking);
        
        var actual = _sut.ListPerTglBerobat(FakerPeriode());
        actual.Should().ContainEquivalentOf(booking,
            opt => opt
                .Excluding(x => x.DokterName)
                .Excluding(x => x.LayananName));
    }
}
