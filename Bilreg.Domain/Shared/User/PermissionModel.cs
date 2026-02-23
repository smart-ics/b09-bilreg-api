namespace Bilreg.Domain.Shared.User;

public class PermissionModel : IPermissionKey
{
    public PermissionModel(string id, string name)
    {
        PermissionId = id;
        PermissionName = name;
    }

    public string PermissionId { get; private set; }
    public string PermissionName { get; private set; }

}

public interface IPermissionKey
{
    string PermissionId { get; }
}