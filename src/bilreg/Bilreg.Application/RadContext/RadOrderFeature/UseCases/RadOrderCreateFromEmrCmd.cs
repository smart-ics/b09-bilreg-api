using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.RadContext.RadOrderFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.RadContext.RadOrderFeature.UseCases;

public record RadOrderCreateFromEmrCmd(
    string UserId,
    string EmrOrderId,
    string RegId,
    string PatientId,
    string PatientName,
    string BirthDateYmd,
    string Gender,
    string Modality,
    List<RadOrderItemInputDto> Items,
    string DokterPengorderId = "",
    string DokterPengorderName = "",
    string LayananPengorderId = "",
    string LayananPengorderName = "",
    string ClinicalNote = "",
    int Urgency = 0,
    bool AllowDuplicateConfirmation = false) : IRequest<RadOrderCreateResponse>;

public record RadOrderCreateResponse(
    string OrderId,
    string OrderNo,
    string EmrOrderId,
    int RadOrderStatus,
    string Modality,
    int ItemCount,
    bool IsCito);

public class RadOrderCreateFromEmrHandler : IRequestHandler<RadOrderCreateFromEmrCmd, RadOrderCreateResponse>
{
    private readonly IRadOrderRepo _radOrderRepo;
    private readonly ISequencer _sequencer;
    private readonly IRadItemResolutionService _resolutionService;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IRegRepo _regRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly IRadOrderDal _radOrderDal;

    public RadOrderCreateFromEmrHandler(
        IRadOrderRepo radOrderRepo,
        ISequencer sequencer,
        IRadItemResolutionService resolutionService,
        ITglJamProvider tglJamProvider,
        IRegRepo regRepo,
        IPpaRepo ppaRepo,
        IRadOrderDal radOrderDal)
    {
        _radOrderRepo = radOrderRepo;
        _sequencer = sequencer;
        _resolutionService = resolutionService;
        _tglJamProvider = tglJamProvider;
        _regRepo = regRepo;
        _ppaRepo = ppaRepo;
        _radOrderDal = radOrderDal;
    }

    public Task<RadOrderCreateResponse> Handle(RadOrderCreateFromEmrCmd request, CancellationToken cancellationToken)
    {
        // 0. Guard clauses parameter wajib
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.EmrOrderId, nameof(request.EmrOrderId));
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));
        Guard.Against.NullOrWhiteSpace(request.PatientId, nameof(request.PatientId));
        Guard.Against.NullOrWhiteSpace(request.PatientName, nameof(request.PatientName));
        Guard.Against.NullOrWhiteSpace(request.Modality, nameof(request.Modality));
        Guard.Against.NullOrEmpty(request.Items, nameof(request.Items));

        // 1. Idempotensi: jika EmrOrderId sudah ada, kembalikan respons order eksisting
        var existingOrder = _radOrderRepo.LoadByEmrOrderId(request.EmrOrderId)
            .Match(
                onSome: x => x,
                onNone: () => null as RadOrderModel
            );

        if (existingOrder is not null)
        {
            return Task.FromResult(new RadOrderCreateResponse(
                existingOrder.OrderId,
                existingOrder.OrderNo,
                existingOrder.EmrOrderId,
                (int)existingOrder.RadOrderStatus,
                existingOrder.Modality.ToString(),
                existingOrder.Items.Count,
                existingOrder.IsCito));
        }

        // 2. Validasi status kunjungan aktif via _regRepo (EX-01)
        var reg = _regRepo.LoadEntity(RegModel.Key(request.RegId))
            .Match(
                onSome: x => x,
                onNone: () => null as RegModel
            );

        if (reg is null)
            throw new ValidationException($"EX-01: Registrasi kunjungan '{request.RegId}' tidak ditemukan.");

        if (!reg.IsAktif)
            throw new ValidationException($"EX-01: Registrasi kunjungan '{request.RegId}' sudah tidak aktif atau sudah keluar/discharge.");

        // 3. Validasi dokter pengorder via _ppaRepo jika DokterPengorderId diisi
        var dokterPengorderName = request.DokterPengorderName;
        if (!string.IsNullOrWhiteSpace(request.DokterPengorderId))
        {
            var ppa = _ppaRepo.LoadEntity(PpaType.Key(request.DokterPengorderId))
                .Match(
                    onSome: x => x,
                    onNone: () => null as PpaType
                );

            if (ppa is null || ppa.PpaId == "-" || string.IsNullOrWhiteSpace(ppa.PpaId))
                throw new ValidationException($"EX-01: Dokter pengorder '{request.DokterPengorderId}' tidak ditemukan atau tidak aktif.");

            if (string.IsNullOrWhiteSpace(dokterPengorderName))
                dokterPengorderName = ppa.PpaName;
        }

        // 4. Validasi Single-Modality dan resolusi item via _resolutionService (EX-02)
        if (!Enum.TryParse<RadModalityEnum>(request.Modality, ignoreCase: true, out var modalityEnum))
            throw new ValidationException($"EX-02: Modalitas radiologi '{request.Modality}' tidak valid.");

        var resolved = _resolutionService.ResolveItems(request.Items, request.Modality);
        if (!resolved.IsValid)
            throw new ValidationException($"EX-02: {resolved.ErrorMessage}");

        // 5. Deteksi duplikasi 24 jam via _radOrderDal (EX-06)
        var patientId = !string.IsNullOrWhiteSpace(reg?.Pasien?.PasienId) && reg.Pasien.PasienId != "-"
            ? reg.Pasien.PasienId
            : request.PatientId;

        var tarifIds = resolved.Items.Select(x => x.TarifId).Distinct().ToList();
        var hasRecent = _radOrderDal.HasRecentOrder(patientId, request.Modality, tarifIds, 24);
        if (hasRecent && !request.AllowDuplicateConfirmation)
        {
            var tarifNames = resolved.Items.Select(x => x.TarifName).Distinct();
            throw new DuplicateRadOrderWarningException(
                patientId,
                request.Modality,
                tarifIds,
                $"EX-06: RAD_DUPLICATE_ORDER_WARNING - Peringatan duplikasi pemeriksaan radiologi dalam 24 jam terakhir untuk pasien '{patientId}' pada modalitas '{request.Modality}'. Tindakan: {string.Join(", ", tarifNames)}. Konfirmasikan sebelum melanjutkan.");
        }

        // 6. Generate sequence unik:
        var now = _tglJamProvider.Now;
        var orderId = _sequencer.GetNextNo(RadOrderModel.IdPrefix, now);
        var orderNo = _sequencer.GetNextNo(RadOrderModel.OrderNoPrefix, now);

        // 7. Bentuk snapshot pasien dan agregat RadOrderModel.CreateFromEmr(...)
        var patientName = !string.IsNullOrWhiteSpace(reg?.Pasien?.PasienName) && reg.Pasien.PasienName != "-"
            ? reg.Pasien.PasienName
            : request.PatientName;

        DateTime birthDate;
        if (reg?.Pasien?.TglLahir != null && reg.Pasien.TglLahir.Year != 3000)
            birthDate = reg.Pasien.TglLahir.ToDateTime(TimeOnly.MinValue);
        else if (DateTime.TryParseExact(request.BirthDateYmd, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1))
            birthDate = d1;
        else if (DateTime.TryParse(request.BirthDateYmd, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d2))
            birthDate = d2;
        else
            birthDate = new DateTime(3000, 1, 1);

        var gender = !string.IsNullOrWhiteSpace(reg?.Pasien?.Gender) && reg.Pasien.Gender != "-"
            ? reg.Pasien.Gender
            : (string.IsNullOrWhiteSpace(request.Gender) ? "-" : request.Gender);

        var ageAtOrder = birthDate.Year == 3000 ? 0 : ComputeAge(birthDate, now);

        var snapshot = new PatientSnapshotType(
            RegId: request.RegId,
            PatientId: patientId,
            PatientName: patientName,
            BirthDate: birthDate,
            Gender: gender,
            AgeAtOrder: ageAtOrder);

        var audit = new AuditInfoType(request.UserId, now);
        var orderUrgency = Enum.IsDefined(typeof(RadUrgencyEnum), request.Urgency)
            ? (RadUrgencyEnum)request.Urgency
            : RadUrgencyEnum.Rutin;

        var order = RadOrderModel.CreateFromEmr(
            request.EmrOrderId,
            snapshot,
            modalityEnum,
            resolved.Items,
            orderNo,
            audit,
            request.DokterPengorderId,
            dokterPengorderName,
            request.LayananPengorderId,
            request.LayananPengorderName,
            request.ClinicalNote,
            orderUrgency,
            orderId);

        // 8. Simpan agregat
        _radOrderRepo.SaveChanges(order);

        // 9. Kembalikan response
        return Task.FromResult(new RadOrderCreateResponse(
            order.OrderId,
            order.OrderNo,
            order.EmrOrderId,
            (int)order.RadOrderStatus,
            order.Modality.ToString(),
            order.Items.Count,
            order.IsCito));
    }

    private static int ComputeAge(DateTime birthDate, DateTime now)
    {
        var age = now.Year - birthDate.Year;
        if (now.Date < birthDate.Date.AddYears(age))
            age--;
        return Math.Max(0, age);
    }
}
