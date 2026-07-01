using Bilreg.Domain.Shared.User;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.Shared.User;

public interface IUserRepo :
    ILoadEntity<UserModel, IUserKey>,
    ISaveChange<UserModel>
{
}
