using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using MediatR;
using Nuna.Lib.DataTypeExtension;

namespace Bilreg.Application.SalesContext.ResepFeature.UseCases;

public record ResepCreateCommand(
    string RegId,
    string PpaId,
    string LayananId,
    string? UrgenitasId,
    string TipeBrgId,
    int Iter,
    string Description,
    decimal BeratBadan,
    decimal TinggiBadan,
    decimal Lpb,
    string UserId,
    IEnumerable<ResepCreateObatCommand> ListObat) : IRequest<ResepCreateResponse>, IRegKey, IPpaKey, ILayananKey, ITipeBrgKey;

public record ResepCreateObatCommand(
    string BrgId,
    string SatuanId,
    decimal Qty,
    int Iter,
    string Signa,
    string Instruction,
    string Note,
    IEnumerable<ResepCreateItemRacikCommand> ListItemRacik);

public record ResepCreateItemRacikCommand(
    string BrgId,
    string SatuanId,
    decimal Qty,
    decimal Dosis,
    string DosisTxt);

public record ResepCreateResponse(string ResepId);

public class ResepCreateHandler : IRequestHandler<ResepCreateCommand, ResepCreateResponse>
{
    private readonly IRegRepo _regRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly ITipeBrgRepo _tipeBrgRepo;
    private readonly IBrgRepo _brgRepo;
    private readonly ISatuanRepo _satuanRepo;
    private readonly IResepRepo _resepRepo;

    public ResepCreateHandler(IRegRepo regRepo, IPpaRepo ppaRepo, ILayananRepo layananRepo, ITipeBrgRepo tipeBrgRepo,
        IBrgRepo brgRepo, ISatuanRepo satuanRepo, IResepRepo resepRepo)
    {
        _regRepo = regRepo;
        _ppaRepo = ppaRepo;
        _layananRepo = layananRepo;
        _tipeBrgRepo = tipeBrgRepo;
        _brgRepo = brgRepo;
        _satuanRepo = satuanRepo;
        _resepRepo = resepRepo;
    }

    public Task<ResepCreateResponse> Handle(ResepCreateCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrWhiteSpace(request.UserId);
        if (request.ListObat.IsNullOrEmpty())
            throw new ArgumentException("List Obat tidak boleh kosong");

        // BUILD
        var reg = LoadReg(request);
        var layanan = LoadLayanan(request);
        var dokter = LoadDokter(request);
        var tipeBrg = LoadTipeBrg(request);
        var bodyMetric = new BodyMetricType(request.BeratBadan, request.TinggiBadan, request.Lpb);
        var urgenitas = string.IsNullOrWhiteSpace(request.UrgenitasId)
            ? UrgenitasType.Default
            : UrgenitasType.Load(request.UrgenitasId, string.Empty);

        var resep = ResepModel.Create(reg, bodyMetric, dokter, layanan, urgenitas, tipeBrg, request.Iter,
            request.Description, request.UserId);
        resep = BuildItemResep(resep, request.ListObat);

        // WRITE
        _resepRepo.SaveChanges(resep);
        return Task.FromResult(new ResepCreateResponse(resep.ResepId));
    }

    private RegModel LoadReg(IRegKey key)
    {
        var result = _regRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Register {key.RegId} not found")
            );

        return !result.IsAktif
            ? throw new ArgumentException($"Register '{key.RegId}' tidak aktif")
            : result;
    }

    private LayananType LoadLayanan(ILayananKey lynKey)
    {
        var layanan = _layananRepo.LoadEntity(lynKey)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Layanan {lynKey.LayananId} not found")
            );

        return layanan;
    }

    private DokterType LoadDokter(IPpaKey key)
    {
        var ppa = _ppaRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"PPA '{key.PpaId}' invalid")
            );

        var dokter = DokterType.Default with { DokterId = ppa.PpaId, DokterName = ppa.PpaName };
        return dokter;
    }

    private TipeBrgType LoadTipeBrg(ITipeBrgKey tipeBrgKey)
    {
        var tipeBrg = _tipeBrgRepo.LoadEntity(tipeBrgKey)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Tipe Barang {tipeBrgKey.TipeBrgId} not found")
            );

        return tipeBrg;
    }

    private IBrg LoadBrg(IBrgKey brgKey)
    {
        var brg = _brgRepo.LoadEntity(brgKey)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Barang {brgKey.BrgId} not found")
            );

        return brg;
    }
    
    private SatuanType LoadSatuan(ISatuanKey satuanKey)
    {
        var satuan = _satuanRepo.LoadEntity(satuanKey)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Satuan Barang {satuanKey.SatuanId} not found")
            );

        return satuan;
    }

    private ResepModel BuildItemResep(ResepModel resep, IEnumerable<ResepCreateObatCommand> listObat)
    {
        foreach (var obat in listObat)
        {
            if (!obat.ListItemRacik.Any())
            {
                var brgKey = BrgObatType.Key(obat.BrgId);
                var brg = LoadBrg(brgKey);

                var satuanKey = SatuanType.Key(obat.SatuanId);
                var satuan = LoadSatuan(satuanKey);
            
                resep.AddObat(brg, satuan, obat.Qty, obat.Iter, obat.Signa, obat.Instruction, obat.Note);
            }
            else
            {
                var brg = BrgObatType.Default with { BrgId = obat.BrgId, BrgName = obat.BrgId };
                var satuanKey = SatuanType.Key(obat.SatuanId);
                var satuan = LoadSatuan(satuanKey);
            
                resep.AddObat(brg, satuan, obat.Qty, obat.Iter, obat.Signa, obat.Instruction, obat.Note);
                
                foreach (var item in obat.ListItemRacik)
                {
                    var itemRacikKey = BrgObatType.Key(item.BrgId);
                    var itemRacik = LoadBrg(itemRacikKey);

                    var satuanItemRacikKey = SatuanType.Key(item.SatuanId);
                    var satuanItemRacik = LoadSatuan(satuanItemRacikKey);
                
                    resep.AddItemRacik(brg, itemRacik, satuanItemRacik, item.Qty, item.Dosis, item.DosisTxt);
                }
            }
        }

        return resep;
    }
}