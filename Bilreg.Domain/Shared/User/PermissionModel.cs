namespace Bilreg.Domain.Shared.User;

public class PermissionModel : IPermissionKey
{
    public PermissionModel(string id, string code)
    {
        PermissionId = id;
        PermissionCode = code;
    }

    public string PermissionId { get; private set; }
    public string PermissionCode { get; private set; }

    
}

public interface IPermissionKey
{
    string PermissionId { get; }
}