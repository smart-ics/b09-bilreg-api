using Bilreg.Infrastructure.AdmisiRanapContext.JourneyFeature;
using FluentAssertions;

namespace Bilreg.Test.AdmisiRanapContext.JourneyFeature;

public class JourneyDalBatchSelectSqlTest
{
    [Fact]
    public void BuildAdmissionSelectSql_JoinsInpatientOrder_DeterministicallyProjectsSources_AndUsesInpatientOrderIdInWhere()
    {
        // Act
        var sql = JourneyDal.BuildAdmissionSelectSql();

        // Assert - Criteria 1: LEFT JOIN to BILRG_AdmInpatientOrder on InpatientOrderId and VodDate
        sql.Should().Contain("LEFT JOIN BILRG_AdmInpatientOrder io ON a.InpatientOrderId = io.InpatientOrderId AND io.VodDate = @VodDate");

        // Assert - Criteria 2: Mutually exclusive projections using io.OrderSource, eliminating dual COALESCE
        sql.Should().NotContain("COALESCE(NULLIF(a.InpatientOrderId,'-'), a.OpnameRequestId)");
        sql.Should().NotContain("COALESCE(NULLIF(a.InpatientOrderId,'-'), a.ReservationId)");
        sql.Should().Contain("WHEN a.InpatientOrderId NOT IN ('','-') AND io.OrderSource IN (1, 2) THEN a.InpatientOrderId");
        sql.Should().Contain("WHEN a.InpatientOrderId NOT IN ('','-') AND io.OrderSource IN (3, 4) THEN a.InpatientOrderId");

        // Assert - Criteria 3: WHERE clause filters on InpatientOrderId, legacy columns eliminated
        sql.Should().Contain("a.InpatientOrderId IN @OpnIds");
        sql.Should().Contain("a.InpatientOrderId IN @RsvIds");
        sql.Should().NotContain("a.OpnameRequestId IN @OpnIds");
        sql.Should().NotContain("a.ReservationId IN @RsvIds");
    }
}
