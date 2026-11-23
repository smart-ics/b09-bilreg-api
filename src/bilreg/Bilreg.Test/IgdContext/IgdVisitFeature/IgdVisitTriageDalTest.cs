using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Infrastructure.IgdContext.IgdVisitFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.IgdVisitFeature;

public class IgdVisitTriageDalTest
{
    private readonly IgdVisitTriageDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IgdVisitTriageDto Faker(
        int noTriage = 1,
        string level = "P3",
        bool isVoided = false,
        string voidUserId = "",
        DateTime? voidDateTime = null,
        string voidReason = "")
        => new IgdVisitTriageDto(
            IgdVisitId: "TIGV0000000001",
            NoTriage: noTriage,
            TriageMethod: "ATS",
            TriageLevel: level,
            TriageColor: "YELLOW",
            AirwaysScore: 3,
            BreathingScore: 3,
            BloodCirculationScore: 3,
            GcsEyeScore: 3,
            GcsMotorScore: 3,
            GcsVoiceScore: 3,
            IsManualOverrideBlack: false,
            OverrideByUserId: "-",
            OverrideReason: "-",
            OverrideDateTime: new DateTime(3000, 1, 1),
            AssessmentDateTime: new DateTime(2026, 1, 1, 8, 30, 0),
            AssessorUserId: "U1",
            Notes: "TD turun",
            IsVoided: isVoided,
            VoidUserId: voidUserId,
            VoidDateTime: voidDateTime ?? new DateTime(3000, 1, 1),
            VoidReason: voidReason);

    private static IIgdVisitKey FakerKey() => IgdVisitModel.Key("TIGV0000000001");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker(1));
        _sut.Insert(Faker(2, "P2"));
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var active = Faker(1, "P3");
        var voided = Faker(
            noTriage: 2,
            level: "P2",
            isVoided: true,
            voidUserId: "U2",
            voidDateTime: new DateTime(2026, 1, 1, 9, 0, 0),
            voidReason: "Salah input skor");
        _sut.Insert(active);
        _sut.Insert(voided);

        var actual = _sut.ListData(FakerKey()).ToList();
        actual.Should().HaveCount(2);
        actual.Should().ContainEquivalentOf(active);
        actual.Should().ContainEquivalentOf(voided);
    }

    [Fact]
    public void DtoRoundTrip_PreservesVoidMetadata()
    {
        var voidTime = new DateTime(2026, 1, 1, 9, 15, 0);
        var model = IgdVisitTriageType.Default with
        {
            NoTriage = 2,
            Method = TriageMethodEnum.Ats,
            Level = TriageLevelEnum.Ats2,
            Color = TriageColorEnum.Red,
            IsVoided = true,
            VoidUserId = "U9",
            VoidDateTime = voidTime,
            VoidReason = "Input ganda",
        };

        var dto = IgdVisitTriageDto.FromModel("IGV000000001", model);
        var restored = dto.ToModel();

        dto.IsVoided.Should().BeTrue();
        dto.VoidUserId.Should().Be("U9");
        dto.VoidDateTime.Should().Be(voidTime);
        dto.VoidReason.Should().Be("Input ganda");
        restored.Should().BeEquivalentTo(model);
    }
}
