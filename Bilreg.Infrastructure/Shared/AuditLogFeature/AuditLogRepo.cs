using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.AuditLogFeature;

namespace Bilreg.Infrastructure.Shared.AuditLogFeature;

public class AuditLogRepo : IAuditRepo
{
    private readonly IAuditLogDal _auditLogDal;

    public AuditLogRepo(IAuditLogDal auditLogDal)
    {
        _auditLogDal = auditLogDal;
    }

    public void SaveChanges(AuditLog audit)
    {
        _auditLogDal.Insert(AuditLogDto.FromModel(audit));
    }
}
