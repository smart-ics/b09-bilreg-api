using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.TelaahResepFeature;

public class TelaahResepModelTest
{
    [Fact]
    public void Completes_approved_partial_and_rejected_from_explicit_dispositions()
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [
            TelaahResepItemModel.Pending(1, 1, "A", "A", 10),
            TelaahResepItemModel.Pending(2, 2, "B", "B", 5)
        ]);
        telaah.Start("pharm", DateTime.Now);
        telaah.UpdateItem(telaah.Items[0].WithDisposition(TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, "", "pharm"));
        telaah.UpdateItem(telaah.Items[1].WithDisposition(TelaahDispositionEnum.Rejected, "B", "B", 0, "allergy", "pharm"));
        telaah.Complete(DateTime.Now);
        telaah.TelaahStatus.Should().Be(TelaahStatusEnum.PartiallyApproved);
        telaah.AcceptedItems().Should().HaveCount(1);
        telaah.CanEstablishSalesOrder.Should().BeTrue();
    }

    [Fact]
    public void Rejected_review_cannot_establish_sales_order()
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [TelaahResepItemModel.Pending(1, 1, "A", "A", 10)]);
        telaah.Start("pharm", DateTime.Now);
        telaah.UpdateItem(telaah.Items[0].WithDisposition(TelaahDispositionEnum.Rejected, "A", "A", 0, "unsafe", "pharm"));
        telaah.Complete(DateTime.Now);
        telaah.TelaahStatus.Should().Be(TelaahStatusEnum.Rejected);
        telaah.CanEstablishSalesOrder.Should().BeFalse();
    }

    [Fact]
    public void Substitute_requires_reason()
    {
        var pending = TelaahResepItemModel.Pending(1, 1, "A", "A", 10);
        var act = () => pending.WithDisposition(TelaahDispositionEnum.AcceptedSubstitute, "B", "B", 10, "", "pharm");
        act.Should().Throw<ApotekDomainException>();
    }

    [Fact]
    public void Reject_requires_reason()
    {
        var pending = TelaahResepItemModel.Pending(1, 1, "A", "A", 10);
        var act = () => pending.WithDisposition(TelaahDispositionEnum.Rejected, "A", "A", 0, "  ", "pharm");
        act.Should().Throw<ApotekDomainException>()
            .WithMessage("Substitute and reject dispositions require a reason.");
    }

    [Fact]
    public void Complete_rejected_when_any_line_still_pending()
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [
            TelaahResepItemModel.Pending(1, 1, "A", "A", 10),
            TelaahResepItemModel.Pending(2, 2, "B", "B", 5)
        ]);
        telaah.Start("pharm", DateTime.Now);
        telaah.UpdateItem(telaah.Items[0].WithDisposition(
            TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, "", "pharm"));

        var act = () => telaah.Complete(DateTime.Now);

        act.Should().Throw<ApotekDomainException>()
            .WithMessage("Every line must have an explicit disposition before completion.");
        telaah.TelaahStatus.Should().Be(TelaahStatusEnum.UnderReview);
    }

    [Fact]
    public void UpdateItem_rejected_after_terminal_complete()
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [TelaahResepItemModel.Pending(1, 1, "A", "A", 10)]);
        telaah.Start("pharm", DateTime.Now);
        telaah.UpdateItem(telaah.Items[0].WithDisposition(
            TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, "", "pharm"));
        telaah.Complete(DateTime.Now);

        var act = () => telaah.UpdateItem(telaah.Items[0].WithDisposition(
            TelaahDispositionEnum.Rejected, "A", "A", 0, "late", "pharm"));

        act.Should().Throw<ApotekDomainException>()
            .WithMessage("Telaah items can change only while Under Review.");
        telaah.TelaahStatus.Should().Be(TelaahStatusEnum.Approved);
    }

    [Fact]
    public void Completes_fully_approved_when_all_lines_accepted()
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [
            TelaahResepItemModel.Pending(1, 1, "A", "A", 10),
            TelaahResepItemModel.Pending(2, 2, "B", "B", 5)
        ]);
        telaah.Start("pharm", DateTime.Now);
        telaah.UpdateItem(telaah.Items[0].WithDisposition(
            TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, "", "pharm"));
        telaah.UpdateItem(telaah.Items[1].WithDisposition(
            TelaahDispositionEnum.AcceptedSubstitute, "C", "C", 5, "stock alt", "pharm"));
        telaah.Complete(DateTime.Now);

        telaah.TelaahStatus.Should().Be(TelaahStatusEnum.Approved);
        telaah.AcceptedItems().Should().HaveCount(2);
        telaah.CanEstablishSalesOrder.Should().BeTrue();
    }

    [Fact]
    public void Open_initializes_default_13_kajian_items_with_safe_polarity()
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [TelaahResepItemModel.Pending(1, 1, "A", "A", 10)]);
        telaah.KajianItems.Should().HaveCount(13);
        telaah.KajianItems.All(x => !x.RequiresNote).Should().BeTrue();
        telaah.KajianItems.Single(x => x.KajianResepId == "1.1").IsYes.Should().BeTrue();
        telaah.KajianItems.Single(x => x.KajianResepId == "3.5").IsYes.Should().BeFalse();
    }

    [Fact]
    public void Open_accepts_custom_kajian_items()
    {
        var customCatalog = TelaahResepKajianModel.DefaultCatalog()
            .Select(x => x.KajianResepId == "1.1" ? x.WithAnswer(false, "Konfirmasi dokter") : x)
            .ToList();

        var telaah = TelaahResepModel.Open("ARX1", "R1", [TelaahResepItemModel.Pending(1, 1, "A", "A", 10)], customCatalog);

        telaah.KajianItems.Single(x => x.KajianResepId == "1.1").IsYes.Should().BeFalse();
        telaah.KajianItems.Single(x => x.KajianResepId == "1.1").CatatanKajian.Should().Be("Konfirmasi dokter");
    }

    [Fact]
    public void Rehydrate_accepts_custom_kajian_items()
    {
        var customCatalog = TelaahResepKajianModel.DefaultCatalog()
            .Select(x => x.KajianResepId == "3.2" ? x.WithAnswer(true, "Duplikasi terkontrol") : x)
            .ToList();

        var telaah = TelaahResepModel.Rehydrate(
            "ATR001", "ARX1", "R1", TelaahStatusEnum.UnderReview, "pharm",
            DateTime.Now, DateTime.MinValue, 3,
            [TelaahResepItemModel.Pending(1, 1, "A", "A", 10)],
            customCatalog);

        telaah.KajianItems.Single(x => x.KajianResepId == "3.2").IsYes.Should().BeTrue();
        telaah.KajianItems.Single(x => x.KajianResepId == "3.2").CatatanKajian.Should().Be("Duplikasi terkontrol");
    }

    [Fact]
    public void UpdateKajian_updates_answer_note_and_increments_version()
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [TelaahResepItemModel.Pending(1, 1, "A", "A", 10)]);
        telaah.Start("pharm", DateTime.Now);
        var initialVersion = telaah.Version;

        telaah.UpdateKajian("1.1", false, "Nama pasien dikonfirmasi manual");

        var updated = telaah.KajianItems.Single(x => x.KajianResepId == "1.1");
        updated.IsYes.Should().BeFalse();
        updated.CatatanKajian.Should().Be("Nama pasien dikonfirmasi manual");
        telaah.Version.Should().Be(initialVersion + 1);
    }

    [Fact]
    public void UpdateKajian_rejected_when_not_under_review()
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [TelaahResepItemModel.Pending(1, 1, "A", "A", 10)]);

        var act = () => telaah.UpdateKajian("1.1", false, "catatan");

        act.Should().Throw<ApotekDomainException>()
            .WithMessage("Telaah items can change only while Under Review.");
    }

    [Fact]
    public void UpdateKajian_rejected_after_terminal_complete()
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [TelaahResepItemModel.Pending(1, 1, "A", "A", 10)]);
        telaah.Start("pharm", DateTime.Now);
        telaah.UpdateItem(telaah.Items[0].WithDisposition(TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, "", "pharm"));
        telaah.Complete(DateTime.Now);

        var act = () => telaah.UpdateKajian("1.1", false, "late note");

        act.Should().Throw<ApotekDomainException>()
            .WithMessage("Telaah items can change only while Under Review.");
    }

    [Fact]
    public void UpdateKajian_rejected_when_code_not_found()
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [TelaahResepItemModel.Pending(1, 1, "A", "A", 10)]);
        telaah.Start("pharm", DateTime.Now);

        var act = () => telaah.UpdateKajian("9.9", false, "catatan");

        act.Should().Throw<ApotekDomainException>()
            .WithMessage("*9.9*");
    }

    [Fact]
    public void UpdateKajianBatch_updates_multiple_answers_and_increments_version_once()
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [TelaahResepItemModel.Pending(1, 1, "A", "A", 10)]);
        telaah.Start("pharm", DateTime.Now);
        var initialVersion = telaah.Version;

        telaah.UpdateKajianBatch([
            ("1.2", false, "SIP dokter dicek"),
            ("3.5", true, "Interaksi dipantau apoteker")
        ]);

        telaah.KajianItems.Single(x => x.KajianResepId == "1.2").IsYes.Should().BeFalse();
        telaah.KajianItems.Single(x => x.KajianResepId == "1.2").CatatanKajian.Should().Be("SIP dokter dicek");
        telaah.KajianItems.Single(x => x.KajianResepId == "3.5").IsYes.Should().BeTrue();
        telaah.KajianItems.Single(x => x.KajianResepId == "3.5").CatatanKajian.Should().Be("Interaksi dipantau apoteker");
        telaah.Version.Should().Be(initialVersion + 1);
    }

    [Fact]
    public void UpdateKajianBatch_rejected_when_not_under_review()
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [TelaahResepItemModel.Pending(1, 1, "A", "A", 10)]);

        var act = () => telaah.UpdateKajianBatch([("1.1", false, "note")]);

        act.Should().Throw<ApotekDomainException>()
            .WithMessage("Telaah items can change only while Under Review.");
    }

    [Fact]
    public void Complete_throws_when_canonical_kajian_code_missing()
    {
        var incompleteCatalog = TelaahResepKajianModel.DefaultCatalog()
            .Where(x => x.KajianResepId != "3.5")
            .ToList();

        var telaah = TelaahResepModel.Open("ARX1", "R1",
            [TelaahResepItemModel.Pending(1, 1, "A", "A", 10)],
            incompleteCatalog);
        telaah.Start("pharm", DateTime.Now);
        telaah.UpdateItem(telaah.Items[0].WithDisposition(TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, "", "pharm"));

        var act = () => telaah.Complete(DateTime.Now);

        act.Should().Throw<ApotekDomainException>()
            .WithMessage("*Missing codes: 3.5*");
    }

    [Fact]
    public void Complete_throws_when_positive_polarity_item_answered_no_without_note()
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [TelaahResepItemModel.Pending(1, 1, "A", "A", 10)]);
        telaah.Start("pharm", DateTime.Now);
        telaah.UpdateItem(telaah.Items[0].WithDisposition(TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, "", "pharm"));
        telaah.UpdateKajian("1.1", false, "   ");

        var act = () => telaah.Complete(DateTime.Now);

        act.Should().Throw<ApotekDomainException>()
            .WithMessage("*1.1*");
    }

    [Fact]
    public void Complete_succeeds_when_positive_polarity_item_answered_no_with_note()
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [TelaahResepItemModel.Pending(1, 1, "A", "A", 10)]);
        telaah.Start("pharm", DateTime.Now);
        telaah.UpdateItem(telaah.Items[0].WithDisposition(TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, "", "pharm"));
        telaah.UpdateKajian("1.1", false, "Dosis diverifikasi via telepon dr. Setiawan");

        telaah.Complete(DateTime.Now);

        telaah.TelaahStatus.Should().Be(TelaahStatusEnum.Approved);
    }

    [Fact]
    public void Complete_throws_when_negative_polarity_item_answered_yes_without_note()
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [TelaahResepItemModel.Pending(1, 1, "A", "A", 10)]);
        telaah.Start("pharm", DateTime.Now);
        telaah.UpdateItem(telaah.Items[0].WithDisposition(TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, "", "pharm"));
        telaah.UpdateKajian("3.5", true, "");

        var act = () => telaah.Complete(DateTime.Now);

        act.Should().Throw<ApotekDomainException>()
            .WithMessage("*3.5*");
    }

    [Fact]
    public void Complete_fails_if_kajian_count_less_than_13()
    {
        var incompleteCatalog = TelaahResepKajianModel.DefaultCatalog()
            .Take(12)
            .ToList();

        var telaah = TelaahResepModel.Open("ARX1", "R1",
            [TelaahResepItemModel.Pending(1, 1, "A", "A", 10)],
            incompleteCatalog);
        telaah.Start("pharm", DateTime.Now);
        telaah.UpdateItem(telaah.Items[0].WithDisposition(TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, "", "pharm"));

        var act = () => telaah.Complete(DateTime.Now);

        act.Should().Throw<ApotekDomainException>()
            .WithMessage("*Missing codes*");
    }

    [Theory]
    [InlineData("1.1")]
    [InlineData("1.2")]
    [InlineData("1.3")]
    [InlineData("1.4")]
    [InlineData("2.1")]
    [InlineData("2.2")]
    [InlineData("2.3")]
    [InlineData("2.4")]
    [InlineData("3.1")]
    public void Complete_fails_if_positive_polarity_answered_No_without_note(string kajianId)
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [TelaahResepItemModel.Pending(1, 1, "A", "A", 10)]);
        telaah.Start("pharm", DateTime.Now);
        telaah.UpdateItem(telaah.Items[0].WithDisposition(TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, "", "pharm"));
        telaah.UpdateKajian(kajianId, false, "  ");

        var act = () => telaah.Complete(DateTime.Now);

        act.Should().Throw<ApotekDomainException>()
            .WithMessage($"*{kajianId}*");
    }

    [Theory]
    [InlineData("3.2")]
    [InlineData("3.3")]
    [InlineData("3.4")]
    [InlineData("3.5")]
    public void Complete_fails_if_negative_polarity_answered_Yes_without_note(string kajianId)
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [TelaahResepItemModel.Pending(1, 1, "A", "A", 10)]);
        telaah.Start("pharm", DateTime.Now);
        telaah.UpdateItem(telaah.Items[0].WithDisposition(TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, "", "pharm"));
        telaah.UpdateKajian(kajianId, true, "");

        var act = () => telaah.Complete(DateTime.Now);

        act.Should().Throw<ApotekDomainException>()
            .WithMessage($"*{kajianId}*");
    }

    [Fact]
    public void Complete_succeeds_when_all_problem_notes_present()
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [TelaahResepItemModel.Pending(1, 1, "A", "A", 10)]);
        telaah.Start("pharm", DateTime.Now);
        telaah.UpdateItem(telaah.Items[0].WithDisposition(TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, "", "pharm"));

        var updates = new List<(string, bool, string)>
        {
            ("1.1", false, "Catatan 1.1"),
            ("1.2", false, "Catatan 1.2"),
            ("1.3", false, "Catatan 1.3"),
            ("1.4", false, "Catatan 1.4"),
            ("2.1", false, "Catatan 2.1"),
            ("2.2", false, "Catatan 2.2"),
            ("2.3", false, "Catatan 2.3"),
            ("2.4", false, "Catatan 2.4"),
            ("3.1", false, "Catatan 3.1"),
            ("3.2", true, "Catatan 3.2"),
            ("3.3", true, "Catatan 3.3"),
            ("3.4", true, "Catatan 3.4"),
            ("3.5", true, "Catatan 3.5"),
        };
        telaah.UpdateKajianBatch(updates);

        telaah.Complete(DateTime.Now);

        telaah.TelaahStatus.Should().Be(TelaahStatusEnum.Approved);
        telaah.KajianItems.All(x => x.RequiresNote).Should().BeTrue();
        telaah.KajianItems.All(x => x.HasRequiredNote).Should().BeTrue();
        telaah.KajianItems.All(x => x.IsCompliant).Should().BeTrue();
    }

    [Fact]
    public void UpdateKajianBatch_updates_multiple_points_in_one_version_bump()
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [TelaahResepItemModel.Pending(1, 1, "A", "A", 10)]);
        telaah.Start("pharm", DateTime.Now);
        var initialVersion = telaah.Version;

        telaah.UpdateKajianBatch([
            ("1.1", false, "Catatan 1.1"),
            ("2.1", false, "Catatan 2.1"),
            ("3.2", true, "Catatan 3.2")
        ]);

        telaah.Version.Should().Be(initialVersion + 1);
        telaah.KajianItems.Single(x => x.KajianResepId == "1.1").CatatanKajian.Should().Be("Catatan 1.1");
        telaah.KajianItems.Single(x => x.KajianResepId == "2.1").CatatanKajian.Should().Be("Catatan 2.1");
        telaah.KajianItems.Single(x => x.KajianResepId == "3.2").CatatanKajian.Should().Be("Catatan 3.2");
    }

    [Fact]
    public void PrescribedAttributes_unchanged_after_WithDisposition()
    {
        var item = TelaahResepItemModel.Pending(1, 10, "ORIG-ID", "Original Drug", 15m);
        item.PrescribedBrgId.Should().Be("ORIG-ID");
        item.PrescribedBrgName.Should().Be("Original Drug");
        item.PrescribedQty.Should().Be(15m);

        var accepted = item.WithDisposition(TelaahDispositionEnum.AcceptedAsPrescribed, "ORIG-ID", "Original Drug", 15m, "", "pharm");
        accepted.PrescribedBrgId.Should().Be("ORIG-ID");
        accepted.PrescribedBrgName.Should().Be("Original Drug");
        accepted.PrescribedQty.Should().Be(15m);

        var substituted = item.WithDisposition(TelaahDispositionEnum.AcceptedSubstitute, "SUB-ID", "Sub Drug", 10m, "Stock alternative", "pharm");
        substituted.PrescribedBrgId.Should().Be("ORIG-ID");
        substituted.PrescribedBrgName.Should().Be("Original Drug");
        substituted.PrescribedQty.Should().Be(15m);

        var rejected = item.WithDisposition(TelaahDispositionEnum.Rejected, "ORIG-ID", "Original Drug", 0m, "Allergy", "pharm");
        rejected.PrescribedBrgId.Should().Be("ORIG-ID");
        rejected.PrescribedBrgName.Should().Be("Original Drug");
        rejected.PrescribedQty.Should().Be(15m);
    }
}
