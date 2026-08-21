using FluentAssertions;

namespace Bilreg.Test.ApotekContext.Architecture;

public class ApotekContextBoundaryTest
{
    private static readonly string[] ForbiddenTokens =
    [
        "PenjualanModel",
        "IPenjualanDal",
        "TelaahModel",
        "IStokMutasiDal",
        "ITrsBillingDal"
    ];

    [Fact]
    public void Apotek_application_and_infrastructure_must_not_reference_legacy_sales_or_neighbor_dals()
    {
        var roots = new[]
        {
            Path.Combine(FindSrcRoot(), "Bilreg.Application", "ApotekContext"),
            Path.Combine(FindSrcRoot(), "Bilreg.Infrastructure", "ApotekContext")
        };

        Directory.Exists(roots[0]).Should().BeTrue("Apotek Application folder must exist");
        Directory.Exists(roots[1]).Should().BeTrue("Apotek Infrastructure folder must exist");

        var hits = new List<string>();
        foreach (var root in roots)
        {
            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                foreach (var token in ForbiddenTokens)
                {
                    if (text.Contains(token, StringComparison.Ordinal))
                        hits.Add($"{file}: {token}");
                }
            }
        }

        hits.Should().BeEmpty(
            "ApotekContext must not reference {0}",
            string.Join(", ", ForbiddenTokens));
    }

    [Fact]
    public void Apotek_must_not_dual_write_legacy_du_or_forbidden_tables()
    {
        var roots = new[]
        {
            Path.Combine(FindSrcRoot(), "Bilreg.Application", "ApotekContext"),
            Path.Combine(FindSrcRoot(), "Bilreg.Infrastructure", "ApotekContext")
        };
        var forbidden = new[]
        {
            "INSERT INTO tb_trs_dobill_umum",
            "BILRG_AptCreditNote",
            "BILRG_AptDispenseAuthorized",
            "BILRG_AptBackorder",
            "BILRG_AptInvoiceCharge",
            "BILRG_AptResepRevisionTask"
        };
        var hits = new List<string>();
        foreach (var root in roots)
        {
            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                foreach (var token in forbidden)
                {
                    if (text.Contains(token, StringComparison.OrdinalIgnoreCase))
                        hits.Add($"{file}: {token}");
                }
            }
        }
        hits.Should().BeEmpty();
    }

    [Fact]
    public void ApotekContext_marker_types_exist()
    {
        typeof(Bilreg.Domain.ApotekContext.Shared.ApotekContextMarker).Namespace
            .Should().Be("Bilreg.Domain.ApotekContext.Shared");
        typeof(Bilreg.Application.ApotekContext.Shared.ApotekApplicationMarker).Namespace
            .Should().Be("Bilreg.Application.ApotekContext.Shared");
        typeof(Bilreg.Infrastructure.ApotekContext.Shared.ApotekInfrastructureMarker).Namespace
            .Should().Be("Bilreg.Infrastructure.ApotekContext.Shared");
    }

    private static string FindSrcRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "Bilreg.Domain"))
                && Directory.Exists(Path.Combine(dir.FullName, "Bilreg.Application")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Unable to locate src/bilreg from test base directory.");
    }
}
