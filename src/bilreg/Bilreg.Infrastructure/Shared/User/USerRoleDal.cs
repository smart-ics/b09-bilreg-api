using Bilreg.Domain.Shared.User;
using Bilreg.Infrastructure.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.Shared.User;

public interface IUserRoleDal :
    IInsert<UserRoleDto>,
    IDelete<IUserKey>,
    IListData<UserRoleDto, IUserKey>
{ }
public class USerRoleDal : IUserRoleDal
{
    private readonly DatabaseOptions _opt;

    public USerRoleDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public void Insert(UserRoleDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_UserRole(
                UserId, Email, UserName, RoleId, RoleName)
            VALUES(
                @UserId, @Email, @UserName, @RoleId, @RoleName)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@UserId", dto.UserId, SqlDbType.VarChar);
        dp.AddParam("@Email", dto.Email, SqlDbType.DateTime);
        dp.AddParam("@UserName", dto.UserName, SqlDbType.VarChar);
        dp.AddParam("@RoleId", dto.RoleId, SqlDbType.VarChar);
        dp.AddParam("@RoleName", dto.RoleName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IUserKey key)
    {
        const string sql = """
           DELETE FROM
               BILRG_UserRole
           WHERE
             Email = @Email
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@Email", key.Email, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<UserRoleDto> ListData(IUserKey key)
    {
        const string sql = """
           SELECT
               aa.UserId, aa.Email, aa.UserName, aa.RoleId, aa.RoleName 
           FROM
               BILRG_UserRole aa
           WHERE
               aa.Email = @Email
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@Email", key.Email, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<UserRoleDto>(sql, dp);
    }
}

public record UserRoleDto(string UserId, string Email, string UserName, string RoleId, string RoleName)
{
    public static UserRoleDto FromMode(UserRoleModel model)
    {
        var result = new UserRoleDto(model.UserId, model.Email, model.UserName, model.Role.RoleId, model.Role.RoleName);
        return result;
    }

    public UserRoleModel ToMode()
    {
        var role = new RoleModel(RoleId, RoleName);
        return new UserRoleModel(UserId, Email, UserName, role);
    }
}