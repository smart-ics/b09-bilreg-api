using Bilreg.Application.ApotekContext.PenjualanFeature;
using Microsoft.Extensions.DependencyInjection;

namespace Bilreg.Infrastructure.ApotekContext.PenjualanFeature;

/// <summary>
/// Registrasi Dependency Injection komponen infrastruktur dan persistensi Penjualan Farmasi (tb_trs_dobill_umum & 2).
/// </summary>
public static class ApotekInfrastructureServiceRegistration
{
    public static IServiceCollection AddApotekPenjualanFeature(this IServiceCollection services)
    {
        // DAL Dapper & SqlBulkCopy
        services.AddScoped<IPenjualanDal, PenjualanDal>();
        services.AddScoped<IPenjualanItemDal, PenjualanItemDal>();

        // Repository
        services.AddScoped<IPenjualanRepo, PenjualanRepo>();

        // Service Adapters & Ports
        services.AddScoped<ITrkTarifPricingPort, TrkTarifPricingAdapter>();
        services.AddScoped<ITrkBillingChargePort, TrkBillingChargeAdapter>();
        services.AddScoped<ITataRekeningInvoicePermissionPort, DenyTataRekeningInvoicePermissionPort>();
        services.AddScoped<IFornasCheckPort, DefaultFornasCheckPort>();
        services.AddScoped<IPaymentClearancePort, FailClosedPaymentClearancePort>();

        return services;
    }
}
