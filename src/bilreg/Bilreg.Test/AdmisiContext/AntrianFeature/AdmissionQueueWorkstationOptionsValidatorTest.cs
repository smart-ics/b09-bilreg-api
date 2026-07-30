using Bilreg.Api.Configurations;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AdmissionQueueWorkstationOptionsValidatorTest
{
    [Fact]
    public void UniqueWorkstationToLoketMappings_AreValid()
    {
        var result=new AdmissionQueueApiOptionsValidator().Validate(null,new AdmissionQueueApiOptions
        {
            Workstations=[new(){WorkstationKey="ADM-01",LoketKey="L1"},new(){WorkstationKey="ADM-02",LoketKey="L2"}]
        });
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void DuplicateLoketMapping_IsRejectedAtStartup()
    {
        var result=new AdmissionQueueApiOptionsValidator().Validate(null,new AdmissionQueueApiOptions
        {
            Workstations=[new(){WorkstationKey="ADM-01",LoketKey="L1"},new(){WorkstationKey="ADM-02",LoketKey="l1"}]
        });
        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void DuplicateWorkstationMapping_IsRejectedAtStartup()
    {
        var result=new AdmissionQueueApiOptionsValidator().Validate(null,new AdmissionQueueApiOptions
        {
            Workstations=[new(){WorkstationKey="ADM-01",LoketKey="L1"},new(){WorkstationKey="adm-01",LoketKey="L2"}]
        });
        result.Failed.Should().BeTrue();
    }
}
