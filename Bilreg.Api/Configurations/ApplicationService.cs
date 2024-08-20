using System.Reflection;
using Bilreg.Application;
using FluentValidation;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.CleanArchHelper;
using Scrutor;

namespace Bilreg.Api.Configurations;

public static class ApplicationService
{
    private const string APPLICATION_ASSEMBLY = "Bilreg.Application";

    public static IServiceCollection AddApplication(this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(ApplicationAssemblyAnchor).Assembly))
            .AddValidatorsFromAssembly(Assembly.Load(APPLICATION_ASSEMBLY));

        services
            .AddScoped<INunaCounterBL, NunaCounterBL>();
        
        services
            .Scan(selector => selector
                .FromAssemblyOf<ApplicationAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(INunaWriter<>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<ApplicationAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(INunaWriterWithReturn<>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime() 
                .FromAssemblyOf<ApplicationAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(INunaBuilder<>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<ApplicationAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(INunaBuilderMeta<>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<ApplicationAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(INunaService<,>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
            );
        return services;
    }
}