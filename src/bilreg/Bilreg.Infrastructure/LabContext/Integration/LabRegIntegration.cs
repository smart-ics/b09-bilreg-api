using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentValidation;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.LabContext.Integration;

public class LabRegIntegration : ILabRegIntegration
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly IRegAktifRepo _regAktifRepo;
    private readonly IRegRepo _regRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly ISequencerManual _sequencer;
    private readonly IOptions<LabOptions> _labOptions;
    private readonly ITglJamProvider _tglJamProvider;

    public LabRegIntegration(
        ILabOrderRepo labOrderRepo,
        IRegAktifRepo regAktifRepo,
        IRegRepo regRepo,
        ILayananRepo layananRepo,
        ISequencerManual sequencer,
        IOptions<LabOptions> labOptions,
        ITglJamProvider tglJamProvider)
    {
        _labOrderRepo = labOrderRepo;
        _regAktifRepo = regAktifRepo;
        _regRepo = regRepo;
        _layananRepo = layananRepo;
        _sequencer = sequencer;
        _labOptions = labOptions;
        _tglJamProvider = tglJamProvider;
    }

    public string CreateExecutionRegistration(LabRegExecutionRequest request)
    {
        Guard.Against.Null(request);
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var order = _labOrderRepo.LoadEntity(LabOrderModel.Key(request.OrderId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"LabOrder '{request.OrderId}' not found."));

        if (order.OrderSource == LabOrderSourceEnum.ExternalPatient)
        {
            return order.Patient.RegId;
        }

        if (order.OrderSource == LabOrderSourceEnum.Emr)
        {
            var today = DateOnly.FromDateTime(_tglJamProvider.Now);

            if (!string.IsNullOrWhiteSpace(request.ExecutionRegId))
            {
                var explicitRegAktif = _regAktifRepo.LoadEntity(RegModel.Key(request.ExecutionRegId))
                    .Match(
                        onSome: r => r,
                        onNone: () => null);

                if (explicitRegAktif is null || explicitRegAktif.Pasien.PasienId != order.Patient.PatientId || explicitRegAktif.RegDate != today)
                {
                    throw new ValidationException($"Registrasi eksekusi '{request.ExecutionRegId}' tidak aktif hari ini untuk pasien.");
                }

                return request.ExecutionRegId!;
            }

            var activeRegs = _regAktifRepo.ListData(PasienModel.Key(order.Patient.PatientId))
                ?.Where(x => x.RegDate == today)
                .ToList() ?? [];

            if (activeRegs.Count == 1)
            {
                return activeRegs[0].RegId;
            }

            if (activeRegs.Count > 1)
            {
                throw new ValidationException("Terdapat lebih dari satu registrasi aktif hari ini untuk pasien. Silakan pilih nomor registrasi kunjungan.");
            }

            // activeRegs.Count == 0
            var originalReg = _regRepo.LoadEntity(RegModel.Key(order.Patient.RegId))
                .Match(
                    onSome: x => x,
                    onNone: () => throw new KeyNotFoundException($"Registrasi asal '{order.Patient.RegId}' tidak ditemukan."));

            var isUmum = originalReg.TipeJaminan.TipeJaminanId is "00000" or "000";
            if (!isUmum)
            {
                throw new ValidationException("Pasien BPJS/Asuransi wajib melakukan pendaftaran di Loket Admisi terlebih dahulu untuk penerbitan eligibilitas (SEP).");
            }

            var newNo = _sequencer.GetNextNoUrut("NOREG", "No Urut Reg Masuk");
            var newRegId = $"RG{newNo:D8}";

            var labLayananId = _labOptions.Value?.LayananLabId ?? "LAB";
            var layananReff = _layananRepo.LoadEntity(LayananType.Key(labLayananId))
                .Match(
                    onSome: l => l.ToReff(),
                    onNone: () => new LayananReff(labLayananId, "Laboratorium"));

            var dokterReff = !string.IsNullOrWhiteSpace(order.DokterPerujukId)
                ? new PpaReff(order.DokterPerujukId, order.DokterPerujukName)
                : originalReg.Dokter;

            var reg = new RegModel(
                regId: newRegId,
                regDate: today,
                regMasukAudit: new AuditInfoType(request.UserId, _tglJamProvider.Now),
                regKeluarAudit: AuditInfoType.Default,
                regCancelOutAudit: AuditInfoType.Default,
                regVoidAudit: AuditInfoType.Default,
                jenisReg: JenisRegEnum.RegJalan,
                pasien: originalReg.Pasien,
                tipeJaminan: originalReg.TipeJaminan,
                polis: originalReg.Polis,
                kelas: originalReg.Kelas,
                caraMasukDk: originalReg.CaraMasukDk,
                rujukan: originalReg.Rujukan,
                dokter: dokterReff,
                layanan: layananReff,
                karcis: KarcisType.Default.ToReff(),
                eligibility: RegEligibilityType.Default,
                listKomponen: [],
                kelasDk: originalReg.KelasDk,
                bangsal: new BangsalReff("-", "-"),
                jenisInap: JenisInapType.Default,
                regIbu: string.Empty);

            var newRegAktif = RegAktifModel.CreateFromReg(reg);

            using (var trans = TransHelper.NewScope())
            {
                _regRepo.SaveChanges(reg);
                _regAktifRepo.SaveChanges(newRegAktif);
                trans.Complete();
            }

            return newRegId;
        }

        throw new InvalidOperationException($"OrderSource '{order.OrderSource}' tidak didukung untuk aktivasi registrasi.");
    }
}
