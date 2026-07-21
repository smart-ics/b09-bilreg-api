using Bilreg.Application;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Application.IgdContext.IgdVisitFeature.TriageEngine;
using Bilreg.Application.PaymentContext.PasienBalanceFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
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
            .AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(ApplicationAssemblyAnchor).Assembly));
            //.AddValidatorsFromAssembly(Assembly.Load(APPLICATION_ASSEMBLY));

        services
            .AddScoped<INunaCounterBL, NunaCounterBL>()
            .AddScoped<ITriageMethodEngine, AtsTriageEngine>()
            .AddScoped<ITriageMethodEngineResolver, TriageMethodEngineResolver>()
            .AddScoped<IAddBillAppService, AddBillAppService>()
            .AddScoped<IAdmissionRegistrationOrchestrator, AdmissionRegistrationOrchestrator>()
            .AddScoped<PasienBalanceBootstrapService>()
            .AddScoped<IPasienBalanceLoader, PasienBalanceLoader>()
            .AddScoped<IJourneyCandidateFinder, JourneyCandidateFinder>();
        
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
                    .AddClasses(c => c.AssignableTo(typeof(INunaService<,>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<ApplicationAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IFactoryLoadOrNull<,>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()                 
                .FromAssemblyOf<ApplicationAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IFactoryLoad<,>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()                 
            );
        return services;
    }
}
