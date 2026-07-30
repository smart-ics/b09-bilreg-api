using Bilreg.Domain.Shared.Helpers;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class SequencerAdmissionAllocationTest
{
    private const int MaxValue = 9999;
    private readonly Sequencer _sut = new(ConnStringHelper.GetTestEnv());
    private readonly string _connString = ConnStringHelper.Get(
        ConnStringHelper.GetTestEnv().Value);

    [Fact]
    public void BoundedSequence_FirstAllocationIsOne_AndConfigurationIsNonCycling()
    {
        var tag = NewTag("FIRST");
        try
        {
            _sut.GetNextNoUrut(tag, MaxValue).Should().Be(1);

            using var conn = new SqlConnection(_connString);
            var state = conn.QuerySingle<SequenceState>("""
                SELECT
                    CONVERT(BIGINT, aa.minimum_value) AS MinimumValue,
                    CONVERT(BIGINT, aa.maximum_value) AS MaximumValue,
                    CONVERT(BIGINT, aa.increment) AS IncrementValue,
                    aa.is_cycling AS IsCycling
                FROM sys.sequences aa
                INNER JOIN sys.schemas bb ON bb.schema_id = aa.schema_id
                WHERE bb.name = 'dbo' AND aa.name = @SequenceName
                """, new { SequenceName = SequenceName(tag) });

            state.MinimumValue.Should().Be(1);
            state.MaximumValue.Should().Be(MaxValue);
            state.IncrementValue.Should().Be(1);
            state.IsCycling.Should().BeFalse();
        }
        finally
        {
            DropSequence(tag);
        }
    }

    [Fact]
    public void BoundedSequence_Allocates9999_ThenFailsExplicitlyWithoutCycling()
    {
        var tag = NewTag("MAX");
        try
        {
            CreateSequence(tag, 9999);

            _sut.GetNextNoUrut(tag, MaxValue).Should().Be(9999);
            var exhausted = () => _sut.GetNextNoUrut(tag, MaxValue);

            exhausted.Should().Throw<SequenceExhaustedException>()
                .WithMessage($"*{tag}*9999*");
        }
        finally
        {
            DropSequence(tag);
        }
    }

    [Fact]
    public void BoundedSequence_ConcurrentLazyAllocationProducesUniqueNumbers()
    {
        var tag = NewTag("RACE");
        try
        {
            var results = new ConcurrentBag<int>();

            Parallel.For(0, 24, _ =>
                results.Add(_sut.GetNextNoUrut(tag, MaxValue)));

            results.Should().HaveCount(24);
            results.Should().OnlyHaveUniqueItems();
            results.Order().Should().Equal(Enumerable.Range(1, 24));
        }
        finally
        {
            DropSequence(tag);
        }
    }

    [Fact]
    public void BoundedSequence_IsSeparatedByServicePointAndBusinessDateTag()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var tags = new[]
        {
            $"AN2607230000_BP{suffix}",
            $"AN2607230000_UM{suffix}",
            $"AN2607240000_BP{suffix}"
        };

        try
        {
            tags.Select(tag => _sut.GetNextNoUrut(tag, MaxValue))
                .Should().OnlyContain(x => x == 1);
            _sut.GetNextNoUrut(tags[0], MaxValue).Should().Be(2);
        }
        finally
        {
            foreach (var tag in tags)
                DropSequence(tag);
        }
    }

    private static string NewTag(string purpose)
    {
        var tag = $"AQ{purpose}{Guid.NewGuid():N}";
        return tag[..Math.Min(40, tag.Length)];
    }

    private static string SequenceName(string tag) => $"sq_{tag.ToLowerInvariant()}";

    private void CreateSequence(string tag, int startWith)
    {
        var sequenceName = SequenceName(tag);
        using var conn = new SqlConnection(_connString);
        conn.Execute($"""
            CREATE SEQUENCE [dbo].[{sequenceName}]
                AS INT
                START WITH {startWith}
                INCREMENT BY 1
                MINVALUE 1
                MAXVALUE 9999
                NO CYCLE;
            """);
    }

    private void DropSequence(string tag)
    {
        var sequenceName = SequenceName(tag);
        using var conn = new SqlConnection(_connString);
        conn.Execute($"DROP SEQUENCE IF EXISTS [dbo].[{sequenceName}];");
    }

    private sealed record SequenceState(
        long MinimumValue,
        long MaximumValue,
        long IncrementValue,
        bool IsCycling);
}
