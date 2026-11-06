namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

/// <summary>
/// Serializes live SQL tests that share <c>TB_TRS_DO</c> and can deadlock under parallel xUnit.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class DeliveryDatabaseCollection
{
    public const string Name = "DeliveryDatabaseTests";
}
