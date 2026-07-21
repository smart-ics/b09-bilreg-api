// ReSharper disable InconsistentNaming

using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class ta_reg_inap_dal_Test
{
    private readonly ta_reg_inap_dal _sut = new(ConnStringHelper.GetTestEnv());

    private static ta_reg_inap_dto Faker(
        string regId = "RI00000001",
        string prosedur = "IGD",
        string booking = " ",
        string sekunder = " ")
        => new(regId, prosedur, booking, sekunder);

    private static IRegKey Key(string regId = "RI00000001")
        => RegModel.Key(regId);

    private static void EnsureTableExists()
    {
        const string sql = """
            IF OBJECT_ID('ta_reg_inap', 'U') IS NULL
            BEGIN
                CREATE TABLE ta_reg_inap
                (
                    fs_kd_reg             VARCHAR(10) NOT NULL CONSTRAINT DF_ta_reg_inap_fs_kd_reg DEFAULT(' '),
                    fs_kd_caramasuk_inap  VARCHAR(3)  NOT NULL CONSTRAINT DF_ta_reg_inap_fs_kd_caramasuk_inap DEFAULT(' '),
                    fs_kd_trs_booking_bed VARCHAR(10) NOT NULL CONSTRAINT DF_ta_reg_inap_fs_kd_trs_booking_bed DEFAULT(' '),
                    fs_kd_medis_sekunder  VARCHAR(10) NOT NULL CONSTRAINT DF_ta_reg_inap_fs_kd_medis_sekunder DEFAULT(' ')
                )
            END
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Execute(sql);
    }

    [Fact]
    public void Insert_UsesBoundParameters_AndSucceeds()
    {
        using var trans = TransHelper.NewScope();
        EnsureTableExists();
        _sut.Insert(Faker());
    }

    [Fact]
    public void GetData_ReturnsInsertedValues()
    {
        using var trans = TransHelper.NewScope();
        EnsureTableExists();
        var expected = Faker(prosedur: "RJL", booking: " ", sekunder: " ");
        _sut.Insert(expected);

        var actual = _sut.GetData(Key());

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Insert_BlankBookingAndSecondary_RoundTripSafely()
    {
        using var trans = TransHelper.NewScope();
        EnsureTableExists();
        var expected = Faker(booking: " ", sekunder: " ");
        _sut.Insert(expected);

        var actual = _sut.GetData(Key());

        ta_reg_inap_dto.NormalizeOptional(actual.fs_kd_trs_booking_bed).Should().BeEmpty();
        ta_reg_inap_dto.NormalizeOptional(actual.fs_kd_medis_sekunder).Should().BeEmpty();
        actual.fs_kd_caramasuk_inap.Trim().Should().Be("IGD");
    }

    [Fact]
    public void Update_ChangesProcedure_WithoutCreatingAnotherRow()
    {
        using var trans = TransHelper.NewScope();
        EnsureTableExists();
        _sut.Insert(Faker(prosedur: "IGD"));
        _sut.Update(Faker(prosedur: "RJL"));

        var actual = _sut.GetData(Key());
        actual.fs_kd_caramasuk_inap.Trim().Should().Be("RJL");
        actual.fs_kd_reg.Trim().Should().Be("RI00000001");
    }

    [Fact]
    public void Delete_RemovesOnlyIntendedRegistrationRow()
    {
        using var trans = TransHelper.NewScope();
        EnsureTableExists();
        _sut.Insert(Faker("RI00000001"));
        _sut.Insert(Faker("RI00000002"));

        _sut.Delete(Key("RI00000001"));

        _sut.GetData(Key("RI00000001")).Should().BeNull();
        _sut.GetData(Key("RI00000002")).Should().NotBeNull();
    }
}
