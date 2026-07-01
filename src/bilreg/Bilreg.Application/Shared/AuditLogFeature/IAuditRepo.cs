using Bilreg.Domain.Shared.AuditLogFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.Shared.AuditLogFeature;

public interface IAuditRepo : ISaveChange<AuditLog>
{
}
