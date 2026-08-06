using Bilreg.Domain.Shared.Helpers;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.Shared.Helpers;
public class SignaParserTest
{
    [Theory]
    [InlineData("2dd1", 2, 1)]
    [InlineData("2 dd 1", 2, 1)]
    [InlineData("2 dd 1/2", 2, 0.5)]
    [InlineData("2 dd 0.5", 2, 0.5)]
    [InlineData("2 dd 0,5", 2, 0.5)]
    [InlineData("2dd", 2, 1)]
    [InlineData("s3dd1 pc batuk", 3, 1)]
    [InlineData("s 4dd4cc bila demam", 4, 4)]
    [InlineData("1-0-0", 1, 1)]
    [InlineData("0-1-0", 1, 1)]
    [InlineData("0-0-1", 1, 1)]
    [InlineData("1-1-1", 3, 1)]
    [InlineData("1-0-1", 2, 1)]
    [InlineData("0-1-1", 2, 1)]
    [InlineData("1-1-0", 2, 1)]
    [InlineData("2-0-0", 1, 2)]
    [InlineData("0-2-0", 1, 2)]
    [InlineData("0-0-2", 1, 2)]
    [InlineData("2-2-2", 3, 2)]
    [InlineData("2-2-0", 2, 2)]
    [InlineData("1-1-0 ac", 2, 1)]
    [InlineData("1/2-1/2-1/2", 3, 0.5)]
    [InlineData("1/2-0-1/2", 2, 0.5)]
    [InlineData("0-0.5-0.5", 2, 0.5)]
    [InlineData("0-0-0.5", 1, 0.5)]
    [InlineData("1/2-0-1/2 pc batuk", 2, 0.5)]
    [InlineData("s bila batuk 0-0-0.5 ac", 1, 0.5)]
    [InlineData("2x1", 2, 1)]
    [InlineData("2 x 1", 2, 1)]
    [InlineData("3x1", 3, 1)]
    [InlineData("2x1 Sebelum Makan", 2, 1)]
    [InlineData("2x sehari 1 tablet", 2, 1)]
    [InlineData("2x sehari 1", 2, 1)]
    [InlineData("2x sehari 1/2", 2, 0.5)]
    [InlineData("2x sehari 0,5", 2, 0.5)]
    [InlineData("2x 1/2", 2, 0.5)]
    [InlineData("Sebelum Makan 2x 1/2", 2, 0.5)]
    [InlineData("Sebelum Makan 2x Sehari 1/2 Tablet", 2, 0.5)]
    public void GivenSigna_WhenParse_ThenShouldMatchCAandDD(string signa, int dd, decimal ca)
    {
        var result = SignaParser.Parse(signa);
        result.DailyDose.Should().Be(dd);
        result.ConsumeAmount.Should().Be(ca);
    }
}