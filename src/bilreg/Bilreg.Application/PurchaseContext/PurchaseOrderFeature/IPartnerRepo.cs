using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PurchaseContext.PurchaseOrderFeature;

public interface IPartnerRepo: ILoadEntity<PartnerType, IPartnerKey>;