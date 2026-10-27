using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;

namespace Bilreg.Application.AdmisiRanapContext;

public interface IPasienTrackerInpatientCoordinator
{
    PasienTrackerModel RecordReservationCreated(
        string personName,
        DateOnly tglLahir,
        DateOnly visitDate,
        string reservationId,
        DateTime occurredAt = default);

    PasienTrackerModel RecordReservationCreated(
        ReservationModel reservation,
        DateTime occurredAt = default);

    PasienTrackerModel RecordOpnameRequestCreated(
        string personName,
        DateOnly tglLahir,
        DateOnly visitDate,
        string opnameRequestId,
        DateTime occurredAt = default);

    PasienTrackerModel RecordOpnameRequestCreated(
        OpnameRequestModel opnameRequest,
        DateTime occurredAt = default);

    PasienTrackerModel RecordAdmissionRegistered(
        string personName,
        DateOnly tglLahir,
        DateOnly visitDate,
        string regId,
        string? sourceRefId = null,
        DateTime occurredAt = default);

    PasienTrackerModel RecordAdmissionRegistered(
        ReservationModel reservation,
        string regId,
        DateTime occurredAt = default);

    PasienTrackerModel RecordAdmissionRegistered(
        OpnameRequestModel opnameRequest,
        string regId,
        DateTime occurredAt = default);
}

public static class PasienTrackerInpatientEvents
{
    public const string Reservasi = "Reservasi";
    public const string OpnameRequest = "OpnameRequest";
    public const string Registrasi = "Registrasi";
}
