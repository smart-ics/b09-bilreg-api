using Microsoft.Extensions.DependencyInjection;

namespace Bilreg.Application.BrmContext;

public static class BrmApplicationServiceRegistration
{
    public static IServiceCollection AddBrmApplication(this IServiceCollection services)
    {
        return services;
    }
}
