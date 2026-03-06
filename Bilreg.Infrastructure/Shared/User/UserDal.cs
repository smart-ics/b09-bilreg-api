using Bilreg.Domain.Shared.User;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.Shared.User;

public interface IUserDal :
    IInsert<UserDto>,
    IUpdate<UserDto>,
    IGetData<UserDto, IUserKey>
{ }
public class UserDal : IUserDal
{
    private readonly DatabaseOptions _opt;

    public UserDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(UserDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_User(
                UserId, Email, UserName, IsActive)
            VALUES(
                @UserId, @Email, @UserName, @IsActive)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@UserId", dto.UserId, SqlDbType.VarChar);
        dp.AddParam("@Email", dto.Email, SqlDbType.DateTime);
        dp.AddParam("@UserName", dto.UserName, SqlDbType.VarChar);
        dp.AddParam("@IsActive", dto.IsActive, SqlDbType.Bit);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(UserDto dto)
    {
        const string sql = """
           UPDATE 
                BILRG_User
           SET
              UserId = @UserId, 
              UserName = @UserName, 
              IsActive = @IsActive
           WHERE
              Email = @Email
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@UserId", dto.UserId, SqlDbType.VarChar);
        dp.AddParam("@Email", dto.Email, SqlDbType.DateTime);
        dp.AddParam("@UserName", dto.UserName, SqlDbType.VarChar);
        dp.AddParam("@IsActive", dto.IsActive, SqlDbType.Bit);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public UserDto GetData(IUserKey key)
    {
        const string sql = """
           SELECT
               aa.UserId, aa.Email, aa.UserName, IsActive
           FROM
               BILRG_User aa
           WHERE
               aa.Email = @Email
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@Email", key.Email, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<UserDto>(sql, dp);
    }
}






public record UserDto(string UserId, string Email, string UserName, bool IsActive)
{
    public static UserDto FromModel(UserModel model)
    {
        var result = new UserDto(model.UserId, model.Email, model.UserName, model.IsActive);
        return result;
    }

    public UserModel ToModel(IEnumerable<UserRoleModel> roles)
    {
        return new UserModel(UserId, Email, UserName, IsActive, roles);
    }
}