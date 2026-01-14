using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.Helpers;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using System;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegGetQuery(string RegId) : IRequest<RegGetResponse>, IRegKey;

public record RegGetResponse(
    string RegId,
    string RegDate,
    AuditInfoType RegMasukAudit,
    AuditInfoType RegKeluarAudit,
    AuditInfoType RegCancelOutAudit,
    bool IsAktif,
    int JenisReg,
    string JenisRegDesc,
    PasienReff Pasien,
    string Umur,
    TipeJaminanReff TipeJaminan,
    PolisReff Polis,
    KelasReff Kelas,
    CaraMasukDkType CaraMasukDk,
    RujukanReff Rujukan,
    PpaReff Dokter,
    LayananReff Layanan,
    KarcisReff Karcis,
    TipeTarifReff TipeTarif,
    JmnTipeBrgType TipeBarang);
public class RegJalanGethandler : IRequestHandler<RegGetQuery, RegGetResponse>
{
    private readonly IRegRepo _regRepo;
    private readonly IJaminanRepo _jaminanRepo;
    private readonly ITipeJaminanRepo _tipeJaminanRepo;
    public RegJalanGethandler(IRegRepo regRepo,
        IJaminanRepo jaminanRepo,
        ITipeJaminanRepo tipeJaminanRepo)
    {
        _regRepo = regRepo;
        _jaminanRepo = jaminanRepo;
        _tipeJaminanRepo = tipeJaminanRepo;
    }

    public Task<RegGetResponse> Handle(RegGetQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        var reg = _regRepo.LoadEntity(request).GetValueOrThrow($"Register {request.RegId} not found");
        var tipeJaminan = _tipeJaminanRepo.LoadEntity(TipeJaminanType.Key(reg.TipeJaminan.TipeJaminanId))
            .GetValueOrThrow($"TipeJaminan {reg.TipeJaminan.TipeJaminanId} not found");
        var jaminan = _jaminanRepo.LoadEntity(JaminanType.Key(tipeJaminan.Jaminan.JaminanId))
            .GetValueOrThrow($"Jaminan {tipeJaminan.Jaminan.JaminanId} not found");
        
        var (tipeTarif, tipeBrg) = ResolveTipe(reg.JenisReg, jaminan);
        var umur = UmurHelper.HitungUmur(reg.Pasien.TglLahir);
        var result = new RegGetResponse(
            reg.RegId, reg.RegDate.ToString("yyyy-MM-dd"),
            reg.RegMasukAudit, reg.RegKeluarAudit, reg.RegCancelOutAudit,
            reg.IsAktif, (int)reg.JenisReg, reg.JenisReg.ToString(),
            reg.Pasien, umur, reg.TipeJaminan, reg.Polis,
            reg.Kelas, reg.CaraMasukDk, reg.Rujukan,
            reg.Dokter, reg.Layanan, reg.Karcis,
            tipeTarif, tipeBrg);

        return Task.FromResult(result);

    }

    private static (TipeTarifReff, JmnTipeBrgType) ResolveTipe(JenisRegEnum jenisReg, JaminanType jaminan)
    {
        return jenisReg switch
        {
            JenisRegEnum.RegJalan => (
                new TipeTarifReff(jaminan.TipeTarif.Rajal.TipeTarifId, jaminan.TipeTarif.Rajal.TipeTarifName),
                new JmnTipeBrgType(jaminan.TipeBarang.Rajal.TipeBarangId, jaminan.TipeBarang.Rajal.TipeBarangName)
            ),

            JenisRegEnum.RegInap => (
                new TipeTarifReff(jaminan.TipeTarif.Ranap.TipeTarifId, jaminan.TipeTarif.Ranap.TipeTarifName),
                new JmnTipeBrgType(jaminan.TipeBarang.Ranap.TipeBarangId, jaminan.TipeBarang.Ranap.TipeBarangName)
            ),

            _ => (new TipeTarifReff("-", "-"), new JmnTipeBrgType("-", "-"))
        };
    }
    

}
