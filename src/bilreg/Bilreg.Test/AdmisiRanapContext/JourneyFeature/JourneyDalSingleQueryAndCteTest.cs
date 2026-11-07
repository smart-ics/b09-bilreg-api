using Bilreg.Infrastructure.AdmisiRanapContext.JourneyFeature;
using FluentAssertions;

namespace Bilreg.Test.AdmisiRanapContext.JourneyFeature;

public class JourneyDalSingleQueryAndCteTest
{
    [Fact]
    public void BuildFindAdmissionByReservationSql_SearchesInpatientOrderId_AndProjectsCleanSourceIds()
    {
        // Act
        var sql = JourneyDal.BuildFindAdmissionByReservationSql();
        var fallbackSql = JourneyDal.BuildFindAdmissionByReservationFallbackSql();

        // Assert - Criteria 1: WHERE clause uses InpatientOrderId = @ReservationId
        sql.Should().Contain("a.InpatientOrderId = @ReservationId");
        sql.Should().NotContain("a.ReservationId = @ReservationId");
        sql.Should().NotContain("a.OpnameRequestId");

        // Assert - Criteria 1: Projections have ReservationId from InpatientOrderId and OpnameRequestId as '-'
        sql.Should().Contain("'-' AS OpnameRequestId");
        sql.Should().Contain("a.InpatientOrderId AS ReservationId");
        sql.Should().NotContain("COALESCE");

        fallbackSql.Should().Contain("'-' AS OpnameRequestId");
        fallbackSql.Should().Contain("a.InpatientOrderId AS ReservationId");
        fallbackSql.Should().NotContain("COALESCE");
    }

    [Fact]
    public void BuildFindAdmissionByOpnameSql_SearchesInpatientOrderId_AndProjectsCleanSourceIds()
    {
        // Act
        var sql = JourneyDal.BuildFindAdmissionByOpnameSql();
        var fallbackSql = JourneyDal.BuildFindAdmissionByOpnameFallbackSql();

        // Assert - Criteria 2: WHERE clause uses InpatientOrderId = @OpnameRequestId
        sql.Should().Contain("a.InpatientOrderId = @OpnameRequestId");
        sql.Should().NotContain("a.OpnameRequestId = @OpnameRequestId");
        sql.Should().NotContain("a.ReservationId");

        // Assert - Criteria 2: Projections have OpnameRequestId from InpatientOrderId and ReservationId as '-'
        sql.Should().Contain("a.InpatientOrderId AS OpnameRequestId");
        sql.Should().Contain("'-' AS ReservationId");
        sql.Should().NotContain("COALESCE");

        fallbackSql.Should().Contain("a.InpatientOrderId AS OpnameRequestId");
        fallbackSql.Should().Contain("'-' AS ReservationId");
        fallbackSql.Should().NotContain("COALESCE");
    }

    [Fact]
    public void BuildJourneyCte_AdmissionRoots_CleansLegacyColumnsFromJourneyIdAndIntegrityChecks()
    {
        // Act
        var cteSql = JourneyDal.BuildJourneyCte();

        // Assert - Criteria 3: Legacy columns cleaned from AdmissionRoots
        cteSql.Should().NotContain("a.OpnameRequestId NOT IN");
        cteSql.Should().NotContain("a.ReservationId NOT IN");
        cteSql.Should().NotContain("a.OpnameRequestId <>");
        cteSql.Should().NotContain("a.ReservationId <>");
        cteSql.Should().NotContain("OpnameMulti");
        cteSql.Should().NotContain("ReservationMulti");

        // Assert - Evaluates InpatientOrderId and OrderMulti
        cteSql.Should().Contain("OrderMulti AS (");
        cteSql.Should().Contain("om.InpatientOrderId IS NOT NULL");
        cteSql.Should().Contain("a.InpatientOrderId <> o.OpnameRequestId");
        cteSql.Should().Contain("a.InpatientOrderId <> rs.ReservationId");

        // Assert - Prospective subqueries do not check legacy admission columns
        cteSql.Should().NotContain("a.OpnameRequestId = o.InpatientOrderId");
        cteSql.Should().NotContain("a.ReservationId = r.InpatientOrderId");
    }

    [Fact]
    public void BuildLoadRegJourneySql_ProjectsSourcesBasedOnInpatientOrderWithoutLegacyColumns()
    {
        // Act
        var sql = JourneyDal.BuildLoadRegJourneySql();

        // Assert: Projection uses io.OrderSource and InpatientOrderId, legacy columns eliminated
        sql.Should().NotContain("WHEN a.OpnameRequestId NOT IN");
        sql.Should().NotContain("WHEN a.ReservationId NOT IN");
        sql.Should().Contain("WHEN a.InpatientOrderId NOT IN ('','-') AND io.OrderSource IN (1, 2) THEN a.InpatientOrderId");
        sql.Should().Contain("WHEN a.InpatientOrderId NOT IN ('','-') AND io.OrderSource IN (3, 4) THEN a.InpatientOrderId");
    }
}
