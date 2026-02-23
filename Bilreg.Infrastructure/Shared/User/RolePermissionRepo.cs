using Bilreg.Application.Shared.User;
using Bilreg.Domain.Shared.User;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.Shared.User;

public class RolePermissionRepo : IRolePermissionRepo
{
    public MayBe<RolePermissionModel> LoadEntity(IRoleKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var dtos = RolePermissionDto
            .ListData()
            .Where(x => x.RoleId == key.RoleId)
            .ToArray();

        if (dtos.Length == 0)
            return MayBe<RolePermissionModel>.None;

        var role = new RolePermissionModel(
            dtos[0].RoleId,
            dtos[0].RoleName,
            dtos.Select(p => new PermissionModel(
                p.PermissionId,
                p.PermissionName
            ))
        );

        return MayBe.From(role);
    }
    
    public IEnumerable<RolePermissionModel> ListData()
    {
        return RolePermissionDto
            .ListData()
            .GroupBy(x => new { x.RoleId, x.RoleName })
            .Select(g => new RolePermissionModel(
                g.Key.RoleId,
                g.Key.RoleName,
                g.Select(p => new PermissionModel(
                    p.PermissionId,
                    p.PermissionName
                ))
            ));
    }

}
