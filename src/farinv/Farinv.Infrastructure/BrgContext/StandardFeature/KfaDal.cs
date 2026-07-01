using Dapper;
using Farinv.Domain.BrgContext.StandardFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public interface IKfaDal :
    IInsert<KfaDto>,
    IUpdate<KfaDto>,
    IDelete<IKfaKey>,
    IGetData<KfaDto, IKfaKey>,
    IListData<KfaDto, string>
{
}

public class KfaDal : IKfaDal
{
    private readonly DatabaseOptions _opt;

    public KfaDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(KfaDto dto)
    {
        const string sql = """
            INSERT INTO FARPU_Kfa (
                KfaId, KfaName, Active, State,
                KfaTemplateId, KfaTemplateName,
                FarmalkesTypeId, FarmalkesTypeName, FarmalkesTypeGroup, FarmalkesHsCode,
                Produksi, NamaDagang, Manufacturer, Registrar, NomorIzinEdar, LkppCode,
                ControlledDrugId, ControlledDrugName, RutePemberianId, RutePemberianName,
                BentukSediaanId, DosePerUnit, UcumId, UcumName, UomName,
                Generik, FixPrice, HetPrice,
                RxTerm, NetWeight, NetWeightName, Volume, VolumeName,
                Description, Indication, Warning, SideEffect, Tags
            ) VALUES (
                @KfaId, @KfaName, @Active, @State,
                @KfaTemplateId, @KfaTemplateName,
                @FarmalkesTypeId, @FarmalkesTypeName, @FarmalkesTypeGroup, @FarmalkesHsCode,
                @Produksi, @NamaDagang, @Manufacturer, @Registrar, @NomorIzinEdar, @LkppCode,
                @ControlledDrugId, @ControlledDrugName, @RutePemberianId, @RutePemberianName,
                @BentukSediaanId, @DosePerUnit, @UcumId, @UcumName, @UomName,
                @Generik, @FixPrice, @HetPrice,
                @RxTerm, @NetWeight, @NetWeightName, @Volume, @VolumeName,
                @Description, @Indication, @Warning, @SideEffect, @Tags
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@KfaId", dto.KfaId, SqlDbType.VarChar);
        dp.AddParam("@KfaName", dto.KfaName, SqlDbType.VarChar);
        dp.AddParam("@Active", dto.Active, SqlDbType.Bit);
        dp.AddParam("@State", dto.State, SqlDbType.VarChar);
        dp.AddParam("@KfaTemplateId", dto.KfaTemplateId, SqlDbType.VarChar);
        dp.AddParam("@KfaTemplateName", dto.KfaTemplateName, SqlDbType.VarChar);
        dp.AddParam("@FarmalkesTypeId", dto.FarmalkesTypeId, SqlDbType.VarChar);
        dp.AddParam("@FarmalkesTypeName", dto.FarmalkesTypeName, SqlDbType.VarChar);
        dp.AddParam("@FarmalkesTypeGroup", dto.FarmalkesTypeGroup, SqlDbType.VarChar);
        dp.AddParam("@FarmalkesHsCode", dto.FarmalkesHsCode, SqlDbType.VarChar);
        dp.AddParam("@Produksi", dto.Produksi, SqlDbType.VarChar);
        dp.AddParam("@NamaDagang", dto.NamaDagang, SqlDbType.VarChar);
        dp.AddParam("@Manufacturer", dto.Manufacturer, SqlDbType.VarChar);
        dp.AddParam("@Registrar", dto.Registrar, SqlDbType.VarChar);
        dp.AddParam("@NomorIzinEdar", dto.NomorIzinEdar, SqlDbType.VarChar);
        dp.AddParam("@LkppCode", dto.LkppCode, SqlDbType.VarChar);
        dp.AddParam("@ControlledDrugId", dto.ControlledDrugId, SqlDbType.VarChar);
        dp.AddParam("@ControlledDrugName", dto.ControlledDrugName, SqlDbType.VarChar);
        dp.AddParam("@RutePemberianId", dto.RutePemberianId, SqlDbType.VarChar);
        dp.AddParam("@RutePemberianName", dto.RutePemberianName, SqlDbType.VarChar);
        dp.AddParam("@BentukSediaanId", dto.BentukSediaanId, SqlDbType.VarChar);
        dp.AddParam("@DosePerUnit", dto.DosePerUnit, SqlDbType.Decimal);
        dp.AddParam("@UcumId", dto.UcumId, SqlDbType.VarChar);
        dp.AddParam("@UcumName", dto.UcumName, SqlDbType.VarChar);
        dp.AddParam("@UomName", dto.UomName, SqlDbType.VarChar);
        dp.AddParam("@Generik", dto.Generik, SqlDbType.Bit);
        dp.AddParam("@FixPrice", dto.FixPrice, SqlDbType.Decimal);
        dp.AddParam("@HetPrice", dto.HetPrice, SqlDbType.Decimal);
        dp.AddParam("@RxTerm", dto.RxTerm, SqlDbType.VarChar);
        dp.AddParam("@NetWeight", dto.NetWeight, SqlDbType.Decimal);
        dp.AddParam("@NetWeightName", dto.NetWeightName, SqlDbType.VarChar);
        dp.AddParam("@Volume", dto.Volume, SqlDbType.Decimal);
        dp.AddParam("@VolumeName", dto.VolumeName, SqlDbType.VarChar);
        dp.AddParam("@Description", dto.Description, SqlDbType.VarChar);
        dp.AddParam("@Indication", dto.Indication, SqlDbType.VarChar);
        dp.AddParam("@Warning", dto.Warning, SqlDbType.VarChar);
        dp.AddParam("@SideEffect", dto.SideEffect, SqlDbType.VarChar);
        dp.AddParam("@Tags", dto.Tags, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(KfaDto dto)
    {
        const string sql = """
            UPDATE FARPU_Kfa
            SET
                KfaName = @KfaName,
                Active = @Active,
                State = @State,
                KfaTemplateId = @KfaTemplateId,
                KfaTemplateName = @KfaTemplateName,
                FarmalkesTypeId = @FarmalkesTypeId,
                FarmalkesTypeName = @FarmalkesTypeName,
                FarmalkesTypeGroup = @FarmalkesTypeGroup,
                FarmalkesHsCode = @FarmalkesHsCode,
                Produksi = @Produksi,
                NamaDagang = @NamaDagang,
                Manufacturer = @Manufacturer,
                Registrar = @Registrar,
                NomorIzinEdar = @NomorIzinEdar,
                LkppCode = @LkppCode,
                ControlledDrugId = @ControlledDrugId,
                ControlledDrugName = @ControlledDrugName,
                RutePemberianId = @RutePemberianId,
                RutePemberianName = @RutePemberianName,
                BentukSediaanId = @BentukSediaanId,
                DosePerUnit = @DosePerUnit,
                UcumId = @UcumId,
                UcumName = @UcumName,
                UomName = @UomName,
                Generik = @Generik,
                FixPrice = @FixPrice,
                HetPrice = @HetPrice,
                RxTerm = @RxTerm,
                NetWeight = @NetWeight,
                NetWeightName = @NetWeightName,
                Volume = @Volume,
                VolumeName = @VolumeName,
                Description = @Description,
                Indication = @Indication,
                Warning = @Warning,
                SideEffect = @SideEffect,
                Tags = @Tags
            WHERE
                KfaId = @KfaId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@KfaId", dto.KfaId, SqlDbType.VarChar);
        dp.AddParam("@KfaName", dto.KfaName, SqlDbType.VarChar);
        dp.AddParam("@Active", dto.Active, SqlDbType.Bit);
        dp.AddParam("@State", dto.State, SqlDbType.VarChar);
        dp.AddParam("@KfaTemplateId", dto.KfaTemplateId, SqlDbType.VarChar);
        dp.AddParam("@KfaTemplateName", dto.KfaTemplateName, SqlDbType.VarChar);
        dp.AddParam("@FarmalkesTypeId", dto.FarmalkesTypeId, SqlDbType.VarChar);
        dp.AddParam("@FarmalkesTypeName", dto.FarmalkesTypeName, SqlDbType.VarChar);
        dp.AddParam("@FarmalkesTypeGroup", dto.FarmalkesTypeGroup, SqlDbType.VarChar);
        dp.AddParam("@FarmalkesHsCode", dto.FarmalkesHsCode, SqlDbType.VarChar);
        dp.AddParam("@Produksi", dto.Produksi, SqlDbType.VarChar);
        dp.AddParam("@NamaDagang", dto.NamaDagang, SqlDbType.VarChar);
        dp.AddParam("@Manufacturer", dto.Manufacturer, SqlDbType.VarChar);
        dp.AddParam("@Registrar", dto.Registrar, SqlDbType.VarChar);
        dp.AddParam("@NomorIzinEdar", dto.NomorIzinEdar, SqlDbType.VarChar);
        dp.AddParam("@LkppCode", dto.LkppCode, SqlDbType.VarChar);
        dp.AddParam("@ControlledDrugId", dto.ControlledDrugId, SqlDbType.VarChar);
        dp.AddParam("@ControlledDrugName", dto.ControlledDrugName, SqlDbType.VarChar);
        dp.AddParam("@RutePemberianId", dto.RutePemberianId, SqlDbType.VarChar);
        dp.AddParam("@RutePemberianName", dto.RutePemberianName, SqlDbType.VarChar);
        dp.AddParam("@BentukSediaanId", dto.BentukSediaanId, SqlDbType.VarChar);
        dp.AddParam("@DosePerUnit", dto.DosePerUnit, SqlDbType.Decimal);
        dp.AddParam("@UcumId", dto.UcumId, SqlDbType.VarChar);
        dp.AddParam("@UcumName", dto.UcumName, SqlDbType.VarChar);
        dp.AddParam("@UomName", dto.UomName, SqlDbType.VarChar);
        dp.AddParam("@Generik", dto.Generik, SqlDbType.Bit);
        dp.AddParam("@FixPrice", dto.FixPrice, SqlDbType.Decimal);
        dp.AddParam("@HetPrice", dto.HetPrice, SqlDbType.Decimal);
        dp.AddParam("@RxTerm", dto.RxTerm, SqlDbType.VarChar);
        dp.AddParam("@NetWeight", dto.NetWeight, SqlDbType.Decimal);
        dp.AddParam("@NetWeightName", dto.NetWeightName, SqlDbType.VarChar);
        dp.AddParam("@Volume", dto.Volume, SqlDbType.Decimal);
        dp.AddParam("@VolumeName", dto.VolumeName, SqlDbType.VarChar);
        dp.AddParam("@Description", dto.Description, SqlDbType.VarChar);
        dp.AddParam("@Indication", dto.Indication, SqlDbType.VarChar);
        dp.AddParam("@Warning", dto.Warning, SqlDbType.VarChar);
        dp.AddParam("@SideEffect", dto.SideEffect, SqlDbType.VarChar);
        dp.AddParam("@Tags", dto.Tags, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IKfaKey key)
    {
        const string sql = """
            DELETE FROM FARPU_Kfa
            WHERE KfaId = @KfaId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@KfaId", key.KfaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public KfaDto GetData(IKfaKey key)
    {
        const string sql = """
            SELECT
                KfaId, KfaName, Active, State,
                KfaTemplateId, KfaTemplateName, FarmalkesTypeId, FarmalkesTypeName,
                FarmalkesTypeGroup, FarmalkesHsCode, Produksi, NamaDagang,
                Manufacturer, Registrar, NomorIzinEdar, LkppCode,
                ControlledDrugId, ControlledDrugName, RutePemberianId, RutePemberianName,
                BentukSediaanId, DosePerUnit, UcumId, UcumName,
                UomName, Generik, FixPrice, HetPrice,
                RxTerm, NetWeight, NetWeightName, Volume,
                VolumeName, Description, Indication, Warning,
                SideEffect, Tags
            FROM 
                FARPU_Kfa
            WHERE 
                KfaId = @KfaId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@KfaId", key.KfaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<KfaDto>(sql, dp);
    }

    public IEnumerable<KfaDto> ListData(string filter)
    {
        const string sql = """
            SELECT
                KfaId, KfaName, Active, State,
                KfaTemplateId, KfaTemplateName, FarmalkesTypeId, FarmalkesTypeName,
                FarmalkesTypeGroup, FarmalkesHsCode, Produksi, NamaDagang,
                Manufacturer, Registrar, NomorIzinEdar, LkppCode,
                ControlledDrugId, ControlledDrugName, RutePemberianId, RutePemberianName,
                BentukSediaanId, DosePerUnit, UcumId, UcumName,
                UomName, Generik, FixPrice, HetPrice,
                RxTerm, NetWeight, NetWeightName, Volume,
                VolumeName, Description, Indication, Warning,
                SideEffect, Tags
            FROM 
                FARPU_Kfa
            WHERE 
                KfaName LIKE @filter
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@filter", $"%{filter}%", SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<KfaDto>(sql, dp);
    }
}
