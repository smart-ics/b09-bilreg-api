using System.Globalization;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public static class LabOrderCreateHelper
{
    public const string OrderNoSequenceTag = "LAB";

    public static string NextOrderNo(ISequencer sequencer)
    {
        var nextNo = sequencer.GetNextNoUrut(OrderNoSequenceTag);
        return $"{OrderNoSequenceTag}{nextNo:D8}";
    }

    public static PatientSnapshotType BuildSnapshot(
        string regId,
        string patientId,
        string patientName,
        string birthDateYmd,
        string gender,
        DateTime orderTimestamp)
    {
        var birthDate = string.IsNullOrWhiteSpace(birthDateYmd)
            ? new DateTime(3000, 1, 1)
            : DateTime.ParseExact(birthDateYmd, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        var age = birthDate.Year == 3000
            ? 0
            : ComputeAgeAtOrder(birthDate, orderTimestamp);

        return new PatientSnapshotType(
            RegId: regId ?? "",
            PatientId: patientId ?? "",
            PatientName: patientName ?? "",
            BirthDate: birthDate,
            Gender: string.IsNullOrWhiteSpace(gender) ? "-" : gender,
            AgeAtOrder: age);
    }

    public static int ComputeAgeAtOrder(DateTime birthDate, DateTime orderTimestamp)
    {
        var years = orderTimestamp.Year - birthDate.Year;
        if (orderTimestamp.Date < birthDate.Date.AddYears(years))
            years--;
        return Math.Max(0, years);
    }

    public static List<LabOrderModel.ResolvedOrderLine> MapResolvedLines(
        IEnumerable<ResolvedLabOrderLine> lines)
        => lines
            .Select(x => new LabOrderModel.ResolvedOrderLine(x.Item, x.Components))
            .ToList();
}
