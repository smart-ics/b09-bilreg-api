using Bilreg.Application.LabContext.LabOwareFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Test.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.LabContext.LabOwareFeature;

public class LabOwarePayloadSerializationTest
{
    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesFields()
    {
        var snapshot = new PatientSnapshotType(
            "REG1", "MR1", "Pasien", new DateTime(1990, 1, 1), "L", 35);
        var item = LabOrderTestSupport.SampleItem();
        var order = LabOrderTestSupport.CreateEmrOrder("LAB00000001", lines: [LabOrderTestSupport.ResolvedLine(item)], snapshot: snapshot, audit: new AuditInfoType("U1", DateTime.Now));
        order.Charge("U1");
        order.MarkCharged("TDK1", "U1");
        order.CollectSpecimen("U1", new CollectionInfoType(DateTime.Now, "U1", "ok"));

        var payload = LabOwarePayloadBuilder.Build(order);
        var json = LabOwarePayloadBuilder.Serialize(payload);
        var restored = LabOwarePayloadBuilder.Deserialize(json);

        restored.OrderNo.Should().Be("LAB00000001");
        restored.Patient.PatientId.Should().Be("MR1");
        restored.TestItems.Should().HaveCount(1);
        restored.TestItems[0].TestCode.Should().Be("HB");
        restored.Specimen.Should().NotBeNull();
        restored.Specimen!.CollectionNote.Should().Be("ok");
    }
}
