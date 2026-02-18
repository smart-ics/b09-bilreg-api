using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers;
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
    TipeBrgType TipeBarang,
    int NoAntrian,
    string SjpNo);
public class RegJalanGethandler : IRequestHandler<RegGetQuery, RegGetResponse>
{
    private readonly IRegRepo _regRepo;
    private readonly IJaminanRepo _jaminanRepo;
    private readonly ITipeJaminanRepo _tipeJaminanRepo;
    private readonly IAntrianRepo _queRepo;
    public RegJalanGethandler(IRegRepo regRepo,
        IJaminanRepo jaminanRepo,
        ITipeJaminanRepo tipeJaminanRepo,
        IAntrianRepo queRepo)
    {
        _regRepo = regRepo;
        _jaminanRepo = jaminanRepo;
        _tipeJaminanRepo = tipeJaminanRepo;
        _queRepo = queRepo;
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

        var dateTime = reg.RegDate.ToDateTime(TimeOnly.MinValue);
        var listQue = _queRepo.ListData(dateTime)?.ToList() ?? [];
        var que = listQue.FirstOrDefault(x => x.ReffId == reg.RegId) 
            ?? new AntrianView("-", 0, -1, "-", "-", "-", new DateTime(3000, 1, 1), "-", "-", "-", TimeOnly.MinValue, TimeOnly.MinValue);



        var result = new RegGetResponse(
            reg.RegId, reg.RegDate.ToString("yyyy-MM-dd"),
            reg.RegMasukAudit, reg.RegKeluarAudit, reg.RegCancelOutAudit,
            reg.IsAktif, (int)reg.JenisReg, reg.JenisReg.ToString(),
            reg.Pasien, umur, reg.TipeJaminan, reg.Polis,
            reg.Kelas, reg.CaraMasukDk, reg.Rujukan,
            reg.Dokter, reg.Layanan, reg.Karcis,
            tipeTarif, tipeBrg, que.NoUrut, reg.SjpNo);

        return Task.FromResult(result);

    }

    private static (TipeTarifReff, TipeBrgType) ResolveTipe(JenisRegEnum jenisReg, JaminanType jaminan)
    {
        return jenisReg switch
        {
            JenisRegEnum.RegJalan => (
                new TipeTarifReff(jaminan.TipeTarif.Rajal.TipeTarifId, jaminan.TipeTarif.Rajal.TipeTarifName),
                new TipeBrgType(jaminan.TipeBarang.Rajal.TipeBarangId, jaminan.TipeBarang.Rajal.TipeBarangName)
            ),

            JenisRegEnum.RegInap => (
                new TipeTarifReff(jaminan.TipeTarif.Ranap.TipeTarifId, jaminan.TipeTarif.Ranap.TipeTarifName),
                new TipeBrgType(jaminan.TipeBarang.Ranap.TipeBarangId, jaminan.TipeBarang.Ranap.TipeBarangName)
            ),

            _ => (new TipeTarifReff("-", "-"), new TipeBrgType("-", "-"))
        };
    }
    

}
