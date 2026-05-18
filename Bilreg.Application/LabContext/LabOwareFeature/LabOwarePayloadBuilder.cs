using System.Text.Json;
using Bilreg.Domain.LabContext.LabOrderFeature;

namespace Bilreg.Application.LabContext.LabOwareFeature;

public static class LabOwarePayloadBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static LabOwareOutboundPayload Build(LabOrderModel order)
    {
        var patient = new LabOwarePatientPayload(
            order.Patient.RegId,
            order.Patient.PatientId,
            order.Patient.PatientName,
            order.Patient.BirthDate,
            order.Patient.Gender,
            order.Patient.AgeAtOrder);

        var testItems = order.Items
            .Select(x => new LabOwareTestItemPayload(
                x.ItemNo,
                x.TestCode,
                x.TestName,
                (int)x.TubeType,
                x.SpecimenType,
                x.RequiredTubeCount))
            .ToList();

        LabOwareSpecimenPayload? specimen = null;
        if (!order.CollectionInfo.IsEmpty)
        {
            specimen = new LabOwareSpecimenPayload(
                order.CollectionInfo.CollectedDate,
                order.CollectionInfo.CollectedUserId,
                order.CollectionInfo.CollectionNote);
        }

        return new LabOwareOutboundPayload(order.OrderNo, patient, testItems, specimen);
    }

    public static string Serialize(LabOwareOutboundPayload payload)
        => JsonSerializer.Serialize(payload, JsonOptions);

    public static LabOwareOutboundPayload Deserialize(string payloadJson)
        => JsonSerializer.Deserialize<LabOwareOutboundPayload>(payloadJson, JsonOptions)
           ?? throw new InvalidOperationException("Payload JSON tidak valid.");
}
