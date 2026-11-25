using System.Data;
using Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dals;
using Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dtos;
using Dapper;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.InventoryContext.OpnameFeature;

public class StockOpnameDalTests
{
    [Fact]
    public void StockOpnameSessionDal_BuildParams_WhenNullablePropertiesAreNull_ShouldNotThrowAndBindSafeEmptyStrings()
    {
        // Arrange
        var dto = new StockOpnameSessionDto(
            SessionId: "SES001",
            LayananId: "FAR01",
            ScopeType: "AllActive",
            Status: "Draft",
            CutoffTime: new DateTime(2026, 11, 23, 8, 0, 0),
            StartedAt: null,
            CountingFinishedAt: null,
            VerifiedAt: null,
            ApprovedAt: null,
            ApprovedBy: null,
            RejectedAt: null,
            RejectedBy: null,
            RejectionNotes: null,
            RejectionDisposition: null,
            CancelledAt: null,
            CancelledBy: null,
            CancellationReason: null,
            CreatedBy: "USR01",
            CreatedAt: new DateTime(2026, 11, 23, 8, 0, 0)
        );

        // Act
        var act = () => StockOpnameSessionDal.BuildParams(dto);

        // Assert
        act.Should().NotThrow();
        var dp = act();
        dp.Get<string>("ApprovedBy").Should().BeEmpty();
        dp.Get<string>("RejectedBy").Should().BeEmpty();
        dp.Get<string>("RejectionNotes").Should().BeEmpty();
        dp.Get<string>("RejectionDisposition").Should().BeEmpty();
        dp.Get<string>("CancelledBy").Should().BeEmpty();
        dp.Get<string>("CancellationReason").Should().BeEmpty();
        dp.Get<string>("SessionId").Should().Be("SES001");
        dp.Get<string>("LayananId").Should().Be("FAR01");
        dp.Get<string>("ScopeType").Should().Be("AllActive");
        dp.Get<string>("Status").Should().Be("Draft");
        dp.Get<string>("CreatedBy").Should().Be("USR01");
    }

    [Fact]
    public void StockOpnameSessionDal_BuildParams_WhenNullablePropertiesHaveValues_ShouldBindCorrectValues()
    {
        // Arrange
        var dto = new StockOpnameSessionDto(
            SessionId: "SES002",
            LayananId: "FAR01",
            ScopeType: "AllActive",
            Status: "Approved",
            CutoffTime: new DateTime(2026, 11, 23, 8, 0, 0),
            StartedAt: new DateTime(2026, 11, 23, 8, 30, 0),
            CountingFinishedAt: new DateTime(2026, 11, 23, 9, 0, 0),
            VerifiedAt: new DateTime(2026, 11, 23, 9, 30, 0),
            ApprovedAt: new DateTime(2026, 11, 23, 10, 0, 0),
            ApprovedBy: "APPROVER01",
            RejectedAt: null,
            RejectedBy: null,
            RejectionNotes: null,
            RejectionDisposition: null,
            CancelledAt: null,
            CancelledBy: null,
            CancellationReason: null,
            CreatedBy: "USR01",
            CreatedAt: new DateTime(2026, 11, 23, 8, 0, 0)
        );

        // Act
        var dp = StockOpnameSessionDal.BuildParams(dto);

        // Assert
        dp.Get<string>("ApprovedBy").Should().Be("APPROVER01");
        dp.Get<string>("RejectedBy").Should().BeEmpty();
    }

    [Fact]
    public void StockOpnameRecountResultDal_BuildParams_WhenNullablePropertiesAreNull_ShouldNotThrowAndBindSafeEmptyStrings()
    {
        // Arrange
        var dto = new StockOpnameRecountResultDto(
            RecountResultId: "REC001",
            SessionId: "SES001",
            BrgId: "BRG01",
            BatchNo: "BATCH01",
            ExpiredDate: "2026-12-31",
            RecountQty: 10m,
            RecountTimestamp: new DateTime(2026, 11, 23, 10, 0, 0),
            SystemQtyAtRecount: 10m,
            ResidualVarianceQty: 0m,
            DiscrepancyReason: null,
            SupervisorNotes: null,
            SupervisorUserId: null!
        );

        // Act
        var act = () => StockOpnameRecountResultDal.BuildParams(dto);

        // Assert
        act.Should().NotThrow();
        var dp = act();
        dp.Get<string>("RecountResultId").Should().Be("REC001");
        dp.Get<string>("SessionId").Should().Be("SES001");
        dp.Get<string>("BrgId").Should().Be("BRG01");
        dp.Get<string>("BatchNo").Should().Be("BATCH01");
        dp.Get<string>("ExpiredDate").Should().Be("2026-12-31");
        dp.Get<string>("DiscrepancyReason").Should().BeEmpty();
        dp.Get<string>("SupervisorNotes").Should().BeEmpty();
        dp.Get<string>("SupervisorUserId").Should().BeEmpty();
    }

    [Fact]
    public void StockOpnameVarianceItemDal_BuildParams_WhenFinalQuantityIsNull_ShouldNotThrow()
    {
        // Arrange
        var dto = new StockOpnameVarianceItemDto(
            SessionId: "SES001",
            BrgId: "BRG01",
            BatchNo: "BATCH01",
            ExpiredDate: "2026-12-31",
            BaseSatuanId: "PCS",
            PhysicalCountQty: 10m,
            SystemQtyAtCount: 12m,
            InitialVarianceQty: -2m,
            VerificationStatus: "PendingVerification",
            FinalQuantity: null
        );

        // Act
        var act = () => StockOpnameVarianceItemDal.BuildParams(dto);

        // Assert
        act.Should().NotThrow();
        var dp = act();
        dp.Get<string>("SessionId").Should().Be("SES001");
        dp.Get<string>("BrgId").Should().Be("BRG01");
        dp.Get<string>("BatchNo").Should().Be("BATCH01");
        dp.Get<string>("ExpiredDate").Should().Be("2026-12-31");
        dp.Get<string>("BaseSatuanId").Should().Be("PCS");
        dp.Get<string>("VerificationStatus").Should().Be("PendingVerification");
        dp.Get<decimal?>("FinalQuantity").Should().BeNull();
    }

    [Fact]
    public void StockOpnameVarianceItemDal_BuildParams_WhenNullablePropertiesAreNull_ShouldNotThrowAndBindSafeEmptyStrings()
    {
        // Arrange
        var dto = new StockOpnameVarianceItemDto(
            SessionId: "SES001",
            BrgId: "BRG01",
            BatchNo: "BATCH01",
            ExpiredDate: "2026-12-31",
            BaseSatuanId: null!,
            PhysicalCountQty: 10m,
            SystemQtyAtCount: 10m,
            InitialVarianceQty: 0m,
            VerificationStatus: null!,
            FinalQuantity: null
        );

        // Act
        var act = () => StockOpnameVarianceItemDal.BuildParams(dto);

        // Assert
        act.Should().NotThrow();
        var dp = act();
        dp.Get<string>("BaseSatuanId").Should().BeEmpty();
        dp.Get<string>("VerificationStatus").Should().BeEmpty();
    }

    [Fact]
    public void StockOpnameAdjustmentDal_BuildParams_WhenAuditFieldsAreNull_ShouldNotThrowAndBindSafeEmptyStrings()
    {
        // Arrange
        var dto = new StockOpnameAdjustmentDto(
            AdjustmentId: "ADJ001",
            SessionId: "SES001",
            BrgId: "BRG01",
            BatchNo: "BATCH01",
            ExpiredDate: "2026-12-31",
            LayananId: "FAR01",
            AdjustmentQty: 5m,
            BaseSatuanId: null!,
            Hpp: 1000m,
            NilaiFinansial: 5000m,
            StokMutasiId: null!,
            LegacyBukuId: null!,
            MovementKind: 1,
            ExecutedAt: new DateTime(2026, 11, 23, 10, 0, 0),
            ExecutedBy: null!
        );

        // Act
        var act = () => StockOpnameAdjustmentDal.BuildParams(dto);

        // Assert
        act.Should().NotThrow();
        var dp = act();
        dp.Get<string>("AdjustmentId").Should().Be("ADJ001");
        dp.Get<string>("SessionId").Should().Be("SES001");
        dp.Get<string>("BrgId").Should().Be("BRG01");
        dp.Get<string>("BatchNo").Should().Be("BATCH01");
        dp.Get<string>("ExpiredDate").Should().Be("2026-12-31");
        dp.Get<string>("LayananId").Should().Be("FAR01");
        dp.Get<string>("BaseSatuanId").Should().BeEmpty();
        dp.Get<string>("StokMutasiId").Should().BeEmpty();
        dp.Get<string>("LegacyBukuId").Should().BeEmpty();
        dp.Get<string>("ExecutedBy").Should().BeEmpty();
    }

    [Fact]
    public void StockOpnameScopeItemDto_TwoParameterConstructor_ShouldSetSessionIdAndBrgIdAndDefaultEmptyBrgName()
    {
        // Act
        var dto = new StockOpnameScopeItemDto("SES001", "BRG001");

        // Assert
        dto.SessionId.Should().Be("SES001");
        dto.BrgId.Should().Be("BRG001");
        dto.BrgName.Should().BeEmpty();
    }

    [Fact]
    public void StockOpnameScopeItemDto_ParameterlessConstructor_ShouldInitializeAllPropertiesToEmptyStrings()
    {
        // Act
        var dto = new StockOpnameScopeItemDto();

        // Assert
        dto.SessionId.Should().BeEmpty();
        dto.BrgId.Should().BeEmpty();
        dto.BrgName.Should().BeEmpty();
    }

    [Fact]
    public void StockOpnameScopeItemDto_ConstructorSignatures_ShouldSatisfyDapperRequirementsForTwoColumnMaterialization()
    {
        // Assert: Parameterless constructor exists
        var paramlessCtor = typeof(StockOpnameScopeItemDto).GetConstructor(Type.EmptyTypes);
        paramlessCtor.Should().NotBeNull("Dapper requires a parameterless constructor fallback for property mapping");

        // Assert: 2-parameter constructor (SessionId, BrgId) exists with matching signatures
        var twoParamCtor = typeof(StockOpnameScopeItemDto).GetConstructor(new[] { typeof(string), typeof(string) });
        twoParamCtor.Should().NotBeNull("Dapper requires constructor matching signature (System.String SessionId, System.String BrgId)");

        var parameters = twoParamCtor!.GetParameters();
        parameters.Should().HaveCount(2);
        parameters[0].Name.Should().Be("SessionId");
        parameters[0].ParameterType.Should().Be(typeof(string));
        parameters[1].Name.Should().Be("BrgId");
        parameters[1].ParameterType.Should().Be(typeof(string));
    }

    [Fact]
    public void StockOpnameScopeItemDto_DapperMaterializationFromTwoColumns_ShouldSucceedWithoutException()
    {
        // Arrange: Simulate 2-column resultset from FARPU_StockOpnameScopeItem (SessionId, BrgId)
        using var table = new DataTable();
        table.Columns.Add("SessionId", typeof(string));
        table.Columns.Add("BrgId", typeof(string));
        table.Rows.Add("SES-2026-001", "BRG-MED-001");

        using var reader = table.CreateDataReader();

        // Act: Use Dapper's row parser to deserialize 2 columns into StockOpnameScopeItemDto
        var parser = reader.GetRowParser<StockOpnameScopeItemDto>();
        reader.Read().Should().BeTrue();
        var act = () => parser(reader);

        // Assert: Materialization succeeds without throwing InvalidOperationException
        act.Should().NotThrow();
        var dto = act();
        dto.Should().NotBeNull();
        dto.SessionId.Should().Be("SES-2026-001");
        dto.BrgId.Should().Be("BRG-MED-001");
        dto.BrgName.Should().BeEmpty();

        var model = dto.ToModel();
        model.SessionId.Should().Be("SES-2026-001");
        model.BrgId.Should().Be("BRG-MED-001");
        model.BrgName.Should().BeEmpty();
    }
}
