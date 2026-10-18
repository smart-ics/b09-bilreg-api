using Bilreg.Application.ApotekContext.QueueFeature;
using Microsoft.Extensions.DependencyInjection;

namespace Bilreg.Infrastructure.ApotekContext.QueueFeature;

public static class ApotekQueueInfrastructureServiceRegistration
{
    public static IServiceCollection AddApotekQueueFeature(this IServiceCollection services)
    {
        services.AddScoped<IApotekQueueEntryDal, ApotekQueueEntryDal>();
        services.AddScoped<IApotekQueueEntryRepo, ApotekQueueEntryRepo>();

        services.AddScoped<IPharmacyServicePointDal, PharmacyServicePointDal>();
        services.AddScoped<IPharmacyServicePointRepo, PharmacyServicePointRepo>();

        services.AddScoped<IPharmacyDisplayLoketDal, PharmacyDisplayLoketDal>();
        services.AddScoped<IPharmacyDisplayLoketRepo, PharmacyDisplayLoketRepo>();

        services.AddScoped<IQueueMappingDal, QueueMappingDal>();
        services.AddScoped<IQueueMappingRepo, QueueMappingRepo>();
        services.AddScoped<IPharmacyQueueMappingRepo, QueueMappingRepo>();

        services.AddScoped<IQueueCloseDal, QueueCloseDal>();
        services.AddScoped<IQueueCloseRepo, QueueCloseRepo>();

        services.AddScoped<IPharmacyQueueOperationRepo, PharmacyQueueOperationRepo>();

        return services;
    }
}
