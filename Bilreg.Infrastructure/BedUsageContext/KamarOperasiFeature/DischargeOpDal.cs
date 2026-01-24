using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public interface IDischargeOpDal :
    IInsert<DischargeOpDto>,
    IUpdate<DischargeOpDto>,
    IDelete<IDischargeOpKey>,
    IGetData<DischargeOpDto, IDischargeOpKey>,
    IListData<DischargeOpDto, DateTime>
{
}

public class DischargeOpDal : IDischargeOpDal
{
    private readonly DatabaseOptions _opt;

    public DischargeOpDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(DischargeOpDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_DischargeOp(
                DischargeOpId, DischargeOpDate, OrderOpId,
                PasienId, RegId, KamarId, PpaId, PatientCondition, PostOpNote,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate )
            VALUES (
                @DischargeOpId, @DischargeOpDate, @OrderOpId,
                @PasienId, @RegId, @KamarId, @PpaId, @PatientCondition, @PostOpNote,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@DischargeOpId", dto.DischargeOpId, SqlDbType.VarChar);
        dp.AddParam("@DischargeOpDate", dto.DischargeOpDate, SqlDbType.DateTime);
        dp.AddParam("@OrderOpId", dto.OrderOpId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@KamarId", dto.KamarId, SqlDbType.VarChar);
        dp.AddParam("@PpaId", dto.PpaId, SqlDbType.VarChar);
        dp.AddParam("@PatientCondition", dto.PatientCondition, SqlDbType.Int);
        dp.AddParam("@PostOpNote", dto.PostOpNote, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(DischargeOpDto model)
    {
        throw new NotImplementedException();
    }

    public void Delete(IDischargeOpKey key)
    {
        throw new NotImplementedException();
    }

    public DischargeOpDto GetData(IDischargeOpKey key)
    {
        throw new NotImplementedException();
    }


    public IEnumerable<DischargeOpDto> ListData(DateTime filter)
    {
        throw new NotImplementedException();
    }
}