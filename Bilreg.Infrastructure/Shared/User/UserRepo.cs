using Bilreg.Application.Shared.User;
using Bilreg.Domain.Shared.User;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.Shared.User;

public class UserRepo : IUserRepo
{
    private readonly IUserDal _userDal;
    private readonly IUserRoleDal _userRoleDal;
    public UserRepo(IUserDal userDal, 
        IUserRoleDal userRoleDal)
    {
        _userDal = userDal;
        _userRoleDal = userRoleDal;
    }

    public MayBe<UserModel> LoadEntity(IUserKey key)
    {
        var listRole = _userRoleDal.ListData(key)?.ToList() ?? [];
        var listRoleMode = listRole.Select(x => x.ToMode());

        var userDto = _userDal.GetData(key);
        if (userDto is null)
            return MayBe<UserModel>.None;
        var model = userDto.ToModel(listRoleMode);
        return MayBe.From(model);
    }

    public void SaveChanges(UserModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _userDal.Update(UserDto.FromModel(model)),
                onNone: () => _userDal.Insert(UserDto.FromModel(model))
            );

        _userRoleDal.Delete(model);
        foreach (var item in model.ListRole)
        {
            _userRoleDal.Insert(UserRoleDto.FromMode(item));    
        }

    }
}
