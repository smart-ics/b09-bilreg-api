using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

[Collection("OrderOpResourceDb")]
public class OrderOpResourceRepoTest
{
    private readonly OrderOpResourceDal _dal = new(ConnStringHelper.GetTestEnv());
    private readonly OrderOpResourceRepo _sut;

    public OrderOpResourceRepoTest()
    {
        _sut = new OrderOpResourceRepo(_dal);
    }

    [Fact]
    public void Reconcile_ShouldInsertNewItemsWithServerGeneratedStableIds_WhenIdIsTempOrEmpty()
    {
        using var trans = TransHelper.NewScope();
        var orderOpId = "01TEST_RECON_NEW_001";
        var targetCollection = new List<OrderOpResourceDto>
        {
            new()
            {
                Id = "res-temp-12345",
                Category = "BLOOD",
                Name = "Whole Blood Golongan A+",
                Qty = 2,
                Unit = "Kantong",
                Notes = "Standby lab"
            },
            new()
            {
                Id = "",
                Category = "IMPLANT",
                Name = "Titanium Plate 3.5mm",
                Qty = 1,
                Unit = "Pcs",
                Notes = "Steril"
            }
        };

        var result = _sut.Reconcile(orderOpId, "SCHED_01", targetCollection, "user1");

        result.Should().HaveCount(2);
        result.Should().OnlyContain(x => x.OrderOpId == orderOpId);
        result.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.Id) && !x.Id.StartsWith("res-"));
        result.Should().Contain(x => x.Name == "Whole Blood Golongan A+" && x.Category == "BLOOD" && x.Qty == 2);
        result.Should().Contain(x => x.Name == "Titanium Plate 3.5mm" && x.Category == "IMPLANT" && x.Qty == 1);
    }

    [Fact]
    public void Reconcile_ShouldPreserveStableId_AndUpdateAttributes_ForExistingItems()
    {
        using var trans = TransHelper.NewScope();
        var orderOpId = "01TEST_RECON_UPD_002";

        // Initial creation
        var initial = _sut.Reconcile(orderOpId, "SCHED_01", new List<OrderOpResourceDto>
        {
            new()
            {
                Id = "",
                Category = "SPECIAL_EQUIPMENT",
                Name = "C-Arm Fluoroscopy",
                Qty = 1,
                Unit = "Set",
                Notes = "Need technician"
            }
        }, "user1");

        initial.Should().HaveCount(1);
        var stableId = initial.First().Id;
        stableId.Should().NotBeNullOrWhiteSpace();

        // Target with updated quantity and notes for the same stable ID
        var target = new List<OrderOpResourceDto>
        {
            new()
            {
                Id = stableId,
                Category = "SPECIAL_EQUIPMENT",
                Name = "C-Arm Fluoroscopy",
                Qty = 2,
                Unit = "Set",
                Notes = "Updated: 2 sets required"
            }
        };

        var updated = _sut.Reconcile(orderOpId, "SCHED_01", target, "user2");

        updated.Should().HaveCount(1);
        var updatedItem = updated.First();
        updatedItem.Id.Should().Be(stableId); // Stable ID preserved
        updatedItem.Qty.Should().Be(2);
        updatedItem.Notes.Should().Be("Updated: 2 sets required");
    }

    [Fact]
    public void Reconcile_ShouldDeleteMissingItems_AndRetainKeptItems()
    {
        using var trans = TransHelper.NewScope();
        var orderOpId = "01TEST_RECON_DEL_003";

        // 1. Initial 2 items
        var initial = _sut.Reconcile(orderOpId, "SCHED_01", new List<OrderOpResourceDto>
        {
            new() { Name = "Item A", Category = "BLOOD", Qty = 1, Unit = "Kantong" },
            new() { Name = "Item B", Category = "IMPLANT", Qty = 1, Unit = "Pcs" }
        }, "user1");

        initial.Should().HaveCount(2);
        var itemA = initial.First(x => x.Name == "Item A");
        var itemB = initial.First(x => x.Name == "Item B");

        // 2. Target only keeps Item A, adds Item C (Item B should be deleted)
        var target = new List<OrderOpResourceDto>
        {
            new() { Id = itemA.Id, Name = "Item A", Category = "BLOOD", Qty = 3, Unit = "Kantong" },
            new() { Id = "", Name = "Item C", Category = "SPECIAL_EQUIPMENT", Qty = 1, Unit = "Set" }
        };

        var reconciled = _sut.Reconcile(orderOpId, "SCHED_01", target, "user1");

        reconciled.Should().HaveCount(2);
        reconciled.Select(x => x.Name).Should().Contain(new[] { "Item A", "Item C" });
        reconciled.Select(x => x.Name).Should().NotContain("Item B");

        // Verify Item B is deleted from database
        var inDbItemB = _dal.GetData(itemB.Id, orderOpId);
        inDbItemB.Should().BeNull();
    }

    [Fact]
    public void Reconcile_ShouldDeleteAllResources_WhenTargetCollectionIsEmpty()
    {
        using var trans = TransHelper.NewScope();
        var orderOpId = "01TEST_RECON_EMPTY_004";

        _sut.Reconcile(orderOpId, "SCHED_01", new List<OrderOpResourceDto>
        {
            new() { Name = "Item A", Category = "BLOOD", Qty = 1, Unit = "Kantong" }
        }, "user1");

        var emptyTarget = new List<OrderOpResourceDto>();
        var reconciled = _sut.Reconcile(orderOpId, "SCHED_01", emptyTarget, "user1");

        reconciled.Should().BeEmpty();
        _dal.ListData(orderOpId).Should().BeEmpty();
    }

    [Fact]
    public void Reconcile_ShouldRejectTargetItem_WhenTargetOrderOpIdMismatches()
    {
        using var trans = TransHelper.NewScope();
        var orderOpId = "01TEST_ORDER_TARGET";
        var targetCollection = new List<OrderOpResourceDto>
        {
            new()
            {
                OrderOpId = "01TEST_OTHER_ORDER",
                Name = "Item X",
                Category = "BLOOD",
                Qty = 1,
                Unit = "Kantong"
            }
        };

        var act = () => _sut.Reconcile(orderOpId, "SCHED_01", targetCollection, "user1");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*owner mismatch*");
    }

    [Fact]
    public void Reconcile_ShouldRejectTargetItem_WhenIdBelongsToAnotherOrderInDb()
    {
        using var trans = TransHelper.NewScope();
        var orderA = "01ORDER_AAA";
        var orderB = "01ORDER_BBB";

        // Create resource in Order A
        var resA = _sut.Reconcile(orderA, "SCHED_A", new List<OrderOpResourceDto>
        {
            new() { Name = "Resource of Order A", Category = "BLOOD", Qty = 1, Unit = "Kantong" }
        }, "user1");

        var idOfA = resA.First().Id;

        // Try to hijack Order A's resource ID in Order B
        var maliciousTarget = new List<OrderOpResourceDto>
        {
            new()
            {
                Id = idOfA,
                Name = "Hijacked Resource",
                Category = "IMPLANT",
                Qty = 5,
                Unit = "Pcs"
            }
        };

        var act = () => _sut.Reconcile(orderB, "SCHED_B", maliciousTarget, "user1");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{idOfA}*belongs to another OrderOpId*");
    }

    [Fact]
    public void Reconcile_ShouldRejectInvalidAttributes()
    {
        using var trans = TransHelper.NewScope();
        var orderOpId = "01TEST_VAL_ERRORS";

        // Empty Name
        var act1 = () => _sut.Reconcile(orderOpId, "SCHED_01", new List<OrderOpResourceDto>
        {
            new() { Name = "", Category = "BLOOD", Qty = 1 }
        }, "user1");
        act1.Should().Throw<ArgumentException>();

        // Zero Quantity
        var act2 = () => _sut.Reconcile(orderOpId, "SCHED_01", new List<OrderOpResourceDto>
        {
            new() { Name = "Item", Category = "BLOOD", Qty = 0 }
        }, "user1");
        act2.Should().Throw<ArgumentException>();

        // Invalid Category
        var act3 = () => _sut.Reconcile(orderOpId, "SCHED_01", new List<OrderOpResourceDto>
        {
            new() { Name = "Item", Category = "INVALID_CATEGORY", Qty = 1 }
        }, "user1");
        act3.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Reconcile_Idempotency_MultipleCallsWithSameTarget_PreservesStateAndStableIds()
    {
        using var trans = TransHelper.NewScope();
        var orderOpId = "01TEST_RECON_IDEMP_001";
        var initialTarget = new List<OrderOpResourceDto>
        {
            new() { Name = "Whole Blood A+", Category = "BLOOD", Qty = 2, Unit = "Kantong", Notes = "Urgent" },
            new() { Name = "Screw 4.0mm", Category = "IMPLANT", Qty = 6, Unit = "Pcs", Notes = "Sterile" }
        };

        // First call
        var firstRun = _sut.Reconcile(orderOpId, "SCHED_01", initialTarget, "user1");
        firstRun.Should().HaveCount(2);
        var id1 = firstRun.First(x => x.Name == "Whole Blood A+").Id;
        var id2 = firstRun.First(x => x.Name == "Screw 4.0mm").Id;

        // Second call with same persisted IDs and attributes
        var secondTarget = new List<OrderOpResourceDto>
        {
            new() { Id = id1, Name = "Whole Blood A+", Category = "BLOOD", Qty = 2, Unit = "Kantong", Notes = "Urgent" },
            new() { Id = id2, Name = "Screw 4.0mm", Category = "IMPLANT", Qty = 6, Unit = "Pcs", Notes = "Sterile" }
        };

        var secondRun = _sut.Reconcile(orderOpId, "SCHED_01", secondTarget, "user1");

        secondRun.Should().HaveCount(2);
        secondRun.Select(x => x.Id).Should().BeEquivalentTo([id1, id2]);
        var allInDb = _dal.ListData(orderOpId).ToList();
        allInDb.Should().HaveCount(2, "Idempotent reconcile must not duplicate resources in database");
    }

    [Fact]
    public void Reconcile_AtomicRollback_WhenExceptionOccursDuringReconciliation_NoPartialPersistence()
    {
        using var trans = TransHelper.NewScope();
        var orderOpId = "01TEST_RECON_ROLLBACK_002";

        var targetWithInvalidSecondItem = new List<OrderOpResourceDto>
        {
            new() { Name = "Valid Blood Unit", Category = "BLOOD", Qty = 1, Unit = "Kantong" },
            new() { Name = "Invalid Item", Category = "ILLEGAL_CAT", Qty = 1 }
        };

        // Act
        var act = () => _sut.Reconcile(orderOpId, "SCHED_01", targetWithInvalidSecondItem, "user1");

        // Assert
        act.Should().Throw<ArgumentException>();
        var itemsInDb = _dal.ListData(orderOpId).ToList();
        itemsInDb.Should().BeEmpty("Validation failure in batch must prevent any partial resource insertion");
    }

    [Fact]
    public void Reconcile_AtomicRollback_WhenTransactionScopeDisposedWithoutComplete_AllMutationsReverted()
    {
        var orderOpId = "01TEST_RECON_ROLLBACK_003";
        string initialId;

        // Step 1: Commit 1 baseline item in an independent scope
        using (var initialScope = TransHelper.NewScope())
        {
            var initialResult = _sut.Reconcile(orderOpId, "SCHED_01", new List<OrderOpResourceDto>
            {
                new() { Name = "Baseline PRC", Category = "BLOOD", Qty = 1, Unit = "Kantong", Notes = "Initial" }
            }, "user1");

            initialId = initialResult.First().Id;
            initialScope.Complete();
        }

        try
        {
            // Step 2: Open uncommitted scope, mutate baseline and add a new item, then discard
            using (var abortedScope = TransHelper.NewScope())
            {
                _sut.Reconcile(orderOpId, "SCHED_01", new List<OrderOpResourceDto>
                {
                    new() { Id = initialId, Name = "Baseline PRC", Category = "BLOOD", Qty = 10, Unit = "Kantong", Notes = "Mutated" },
                    new() { Name = "Temporary Plate", Category = "IMPLANT", Qty = 1, Unit = "Pcs" }
                }, "user2");

                // Intentionally do NOT call abortedScope.Complete()
            }

            // Step 3: Verify rollback in a fresh scope
            using (var verifyScope = TransHelper.NewScope())
            {
                var currentItems = _dal.ListData(orderOpId).ToList();
                currentItems.Should().HaveCount(1, "Second item must not persist after aborted scope");
                var item = currentItems.First();
                item.OrderOpResourceId.Should().Be(initialId);
                item.Jumlah.Should().Be(1, "Initial item quantity must revert to 1");
                item.Keterangan.Should().Be("Initial", "Initial item notes must revert to Initial");
            }
        }
        finally
        {
            // Cleanup baseline item
            using var cleanupScope = TransHelper.NewScope();
            _sut.DeleteAll(orderOpId);
            cleanupScope.Complete();
        }
    }

    [Fact]
    public void Reconcile_FieldValidation_RejectsOverlyLongFields()
    {
        using var trans = TransHelper.NewScope();
        var orderOpId = "01TEST_VAL_LIMITS";

        // Name > 100
        var actName = () => _sut.Reconcile(orderOpId, "SCHED_01", new List<OrderOpResourceDto>
        {
            new() { Name = new string('A', 101), Category = "BLOOD", Qty = 1 }
        }, "user1");
        actName.Should().Throw<ArgumentException>().WithMessage("*100 karakter*");

        // Unit > 20
        var actUnit = () => _sut.Reconcile(orderOpId, "SCHED_01", new List<OrderOpResourceDto>
        {
            new() { Name = "Valid Name", Category = "BLOOD", Qty = 1, Unit = new string('U', 21) }
        }, "user1");
        actUnit.Should().Throw<ArgumentException>().WithMessage("*20 karakter*");

        // Notes > 255
        var actNotes = () => _sut.Reconcile(orderOpId, "SCHED_01", new List<OrderOpResourceDto>
        {
            new() { Name = "Valid Name", Category = "BLOOD", Qty = 1, Notes = new string('N', 256) }
        }, "user1");
        actNotes.Should().Throw<ArgumentException>().WithMessage("*255 karakter*");
    }
}
