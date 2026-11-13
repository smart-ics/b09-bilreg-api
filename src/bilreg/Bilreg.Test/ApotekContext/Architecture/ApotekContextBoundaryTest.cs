using FluentAssertions;

namespace Bilreg.Test.ApotekContext.Architecture;

public class ApotekContextBoundaryTest
{
    private static readonly string[] ForbiddenTokens =
    [
        "SalesContext.PenjualanFeature",
        "SalesContext.PenjualanModel",
        "TelaahModel",
        "IStokMutasiDal"
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
            "BILRG_AptCreditNote",
            "BILRG_AptDispenseAuthorized",
            "BILRG_AptBackorder",
            "BILRG_AptInvoiceCharge",
            "BILRG_AptResepRevisionTask",
            "BILRG_AdmLoketCurrentCall",
            "BILRG_AdmDisplayLoket",
            "BILRG_AdmServicePoint"
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
    public void Apotek_pharmacy_queue_tables_must_be_registered_and_satisfy_adr_apt_001_boundaries()
    {
        var sqlRoot = Path.Combine(FindSrcRoot(), "Bilreg.SqlDb", "ApotekContext");
        Directory.Exists(sqlRoot).Should().BeTrue("ApotekContext SQL schema directory must exist");

        var registeredTables = new[]
        {
            "BILRG_AptServicePoint",
            "BILRG_AptQueueEntry",
            "BILRG_AptLoketCurrentCall",
            "BILRG_AptDisplayLoket",
            "BILRG_AptQueueMapping"
        };

        foreach (var table in registeredTables)
        {
            var ddlPath = Path.Combine(sqlRoot, $"{table}.sql");
            File.Exists(ddlPath).Should().BeTrue($"DDL script for {table} must exist in ApotekContext schema");
            var content = File.ReadAllText(ddlPath);
            content.Should().Contain($"CREATE TABLE {table}", $"DDL script must define {table}");
        }

        // Alter script for PasienHadir
        var alterScript = Path.Combine(sqlRoot, "BILRG_AptQueueMapping_Alter_PasienHadir.sql");
        File.Exists(alterScript).Should().BeTrue("Alter script for PasienHadir must exist");
        var alterContent = File.ReadAllText(alterScript);
        alterContent.Should().Contain("PasienHadir");
        alterContent.Should().Contain("PasienHadirAt");

        // ADR-APT-001 Boundary Guards:
        // 1. AntrianStatusEnum must remain purely generic (Waiting, InService, Done, Withdrawn).
        var enumNames = Enum.GetNames<Bilreg.Domain.AdmisiContext.AntrianFeature.AntrianStatusEnum>();
        enumNames.Should().BeEquivalentTo(["Waiting", "InService", "Done", "Withdrawn"],
            "ADR-APT-001 mandates that AntrianStatusEnum must not be polluted with pharmacy operational states");

        // 2. Pharmacy operational progress is isolated in BILRG_AptQueueEntry via ApotekPhase
        var queueEntryDdl = File.ReadAllText(Path.Combine(sqlRoot, "BILRG_AptQueueEntry.sql"));
        queueEntryDdl.Should().Contain("ApotekPhase", "BILRG_AptQueueEntry must own pharmacy operational phase per ADR-APT-001");
        queueEntryDdl.Should().Contain("CallCountPenerimaan");
        queueEntryDdl.Should().Contain("CallCountSerah");

        // 3. Concurrency protection on BILRG_AptLoketCurrentCall
        var loketDdl = File.ReadAllText(Path.Combine(sqlRoot, "BILRG_AptLoketCurrentCall.sql"));
        loketDdl.Should().Contain("RowVersion ROWVERSION NOT NULL", "BILRG_AptLoketCurrentCall must protect loket claims with ROWVERSION");
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
