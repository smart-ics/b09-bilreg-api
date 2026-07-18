using Taksaka.Core.Entities;
using Taksaka.Core.Enums;

namespace Taksaka.Core.Tests;

public sealed class JobTests
{
    [Fact]
    public void Job_DefaultStatus_IsCreated()
    {
        var job = new Job();

        Assert.Equal(JobStatus.Created, job.Status);
    }

    [Fact]
    public void JobPriority_ContainsExpectedValues()
    {
        Assert.Contains(JobPriority.Critical, Enum.GetValues<JobPriority>());
        Assert.Contains(JobPriority.Background, Enum.GetValues<JobPriority>());
    }
}
