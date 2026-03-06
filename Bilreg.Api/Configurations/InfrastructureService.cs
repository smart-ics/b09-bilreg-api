using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Infrastructure;
using Bilreg.Infrastructure.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Bilreg.Infrastructure.Shared.Param;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.CleanArchHelper;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;
using Scrutor;

namespace Bilreg.Api.Configurations;

public static class InfrastructureService
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, 
        IConfiguration configuration)
    {
        services
            .AddScoped<INunaCounterDal, ParamNoDal>()
            .AddScoped<INunaCounterDecDal, ParamNoDal>()
            .AddScoped<ITglJamProvider, TglJamProvider>()
            .AddScoped<ISequencer, Sequencer>()
            .AddMemoryCache();

        services
            .Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SECTION_NAME))
            .Configure<PasienContextOptions>(configuration.GetSection(PasienContextOptions.SECTION_NAME))
            .Configure<RemoteCetakOptions>(configuration.GetSection(RemoteCetakOptions.SECTION_NAME))
            .Configure<Emr25Options>(configuration.GetSection(Emr25Options.SECTION_NAME));

        services
            .Scan(selector => selector
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IInsert<>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IUpdate<>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IDelete<>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IGetData<,>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IListData<>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IListData<,>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IListData<,,>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IListDataMayBe<>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IListDataMayBe<,>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(INunaService<,>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IRequestResponseService<,>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(ISaveChange<>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(ISaveChange<,>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(IDeleteEntity<>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
                .FromAssemblyOf<InfrastructureAssemblyAnchor>()
                    .AddClasses(c => c.AssignableTo(typeof(ILoadEntity<,>)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelfWithInterfaces()
                    .WithScopedLifetime()
            );
        return services;
    }

}