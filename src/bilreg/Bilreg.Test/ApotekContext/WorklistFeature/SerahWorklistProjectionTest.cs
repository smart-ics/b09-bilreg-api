using Bilreg.Application.ApotekContext.WorklistFeature;
using Bilreg.Domain.ApotekContext.DispensingFeature;
using Bilreg.Domain.ApotekContext.Shared;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.WorklistFeature;

public class SerahWorklistProjectionTest
{
    private static readonly DateTime PreparedAt = new(2026, 8, 20, 10, 0, 0);
    private const int WindowDays = 7;

    [Fact]
    public void Completed_status_maps_to_Completed_category()
    {
        SerahWorklistProjection.ComputeCategory(
                DispensingStatusEnum.Completed,
                PreparedAt,
                DateTime.Now,
                DateTime.Now,
                DateTime.Now,
                ApotekDate.Empty,
                DateTime.Now,
                WindowDays)
            .Should().Be(SerahWorklistProjection.Completed);
    }

    [Fact]
    public void Handover_timestamp_maps_to_Completed_even_when_status_is_Prepared()
    {
        SerahWorklistProjection.ComputeCategory(
                DispensingStatusEnum.Prepared,
                PreparedAt,
                PreparedAt.AddHours(1),
                PreparedAt.AddHours(2),
                PreparedAt.AddHours(3),
                ApotekDate.Empty,
                DateTime.Now,
                WindowDays)
            .Should().Be(SerahWorklistProjection.Completed);
    }

    [Fact]
    public void Expired_terminal_status_maps_to_AccountablyResolved()
    {
        SerahWorklistProjection.ComputeCategory(
                DispensingStatusEnum.Expired,
                PreparedAt,
                ApotekDate.Empty,
                ApotekDate.Empty,
                ApotekDate.Empty,
                ApotekDate.Empty,
                PreparedAt.AddDays(10),
                WindowDays)
            .Should().Be(SerahWorklistProjection.AccountablyResolved);
    }

    [Fact]
    public void Pickup_called_without_education_maps_to_ReadyForReview()
    {
        var pickupCalled = PreparedAt.AddHours(1);
        SerahWorklistProjection.ComputeCategory(
                DispensingStatusEnum.Prepared,
                PreparedAt,
                pickupCalled,
                ApotekDate.Empty,
                ApotekDate.Empty,
                ApotekDate.Empty,
                PreparedAt.AddDays(1),
                WindowDays)
            .Should().Be(SerahWorklistProjection.ReadyForReview);
    }

    [Fact]
    public void Education_after_pickup_maps_to_ReadyForHandover()
    {
        var pickupCalled = PreparedAt.AddHours(1);
        var educationAt = PreparedAt.AddHours(2);
        SerahWorklistProjection.ComputeCategory(
                DispensingStatusEnum.Prepared,
                PreparedAt,
                pickupCalled,
                educationAt,
                ApotekDate.Empty,
                ApotekDate.Empty,
                PreparedAt.AddDays(1),
                WindowDays)
            .Should().Be(SerahWorklistProjection.ReadyForHandover);
    }

    [Fact]
    public void Within_collection_window_maps_to_ReadyForPickup()
    {
        SerahWorklistProjection.ComputeCategory(
                DispensingStatusEnum.Prepared,
                PreparedAt,
                ApotekDate.Empty,
                ApotekDate.Empty,
                ApotekDate.Empty,
                ApotekDate.Empty,
                PreparedAt.AddDays(3),
                WindowDays)
            .Should().Be(SerahWorklistProjection.ReadyForPickup);
    }

    [Fact]
    public void Past_collection_window_without_override_maps_to_PickupExpired()
    {
        SerahWorklistProjection.ComputeCategory(
                DispensingStatusEnum.Prepared,
                PreparedAt,
                ApotekDate.Empty,
                ApotekDate.Empty,
                ApotekDate.Empty,
                ApotekDate.Empty,
                PreparedAt.AddDays(WindowDays + 1),
                WindowDays)
            .Should().Be(SerahWorklistProjection.PickupExpired);
    }

    [Fact]
    public void Past_collection_window_with_override_stays_ReadyForPickup()
    {
        SerahWorklistProjection.ComputeCategory(
                DispensingStatusEnum.Prepared,
                PreparedAt,
                ApotekDate.Empty,
                ApotekDate.Empty,
                ApotekDate.Empty,
                PreparedAt.AddDays(WindowDays + 1),
                PreparedAt.AddDays(WindowDays + 2),
                WindowDays)
            .Should().Be(SerahWorklistProjection.ReadyForPickup);
    }

    [Fact]
    public void PickupExpired_uses_explicit_asOf_clock_not_wall_clock()
    {
        var asOfInsideWindow = PreparedAt.AddDays(WindowDays);
        var asOfOutsideWindow = PreparedAt.AddDays(WindowDays).AddMinutes(1);

        SerahWorklistProjection.ComputeCategory(
                DispensingStatusEnum.Prepared,
                PreparedAt,
                ApotekDate.Empty,
                ApotekDate.Empty,
                ApotekDate.Empty,
                ApotekDate.Empty,
                asOfInsideWindow,
                WindowDays)
            .Should().Be(SerahWorklistProjection.ReadyForPickup);

        SerahWorklistProjection.ComputeCategory(
                DispensingStatusEnum.Prepared,
                PreparedAt,
                ApotekDate.Empty,
                ApotekDate.Empty,
                ApotekDate.Empty,
                ApotekDate.Empty,
                asOfOutsideWindow,
                WindowDays)
            .Should().Be(SerahWorklistProjection.PickupExpired);
    }

    [Fact]
    public void Category_strings_are_projection_labels_not_dispensing_status_names()
    {
        SerahWorklistProjection.ReadyForPickup.Should().NotBe(nameof(DispensingStatusEnum.Prepared));
        SerahWorklistProjection.PickupExpired.Should().NotBe(nameof(DispensingStatusEnum.Expired));
    }
}
