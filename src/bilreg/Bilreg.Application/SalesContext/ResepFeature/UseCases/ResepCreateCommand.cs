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
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.DataTypeExtension;

namespace Bilreg.Application.SalesContext.ResepFeature.UseCases;

public interface IResepCreateDal
{
    IEnumerable<IResepKey> FindPotentialDuplicate(string pasienId, DateTime date, IEnumerable<string> brgIds);
}

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
    IEnumerable<ResepCreateObatCommand> ListObat,
    string LayananStokId = "",
    DateTime? TglRencanaPeriksa = null,
    string ResepAsalReffId = "",
    bool IsConfirmedDuplicate = false) : IRequest<ResepCreateResponse>, IRegKey, IPpaKey, ILayananKey, ITipeBrgKey;

public record ResepCreateObatCommand(
    string BrgId,
    string SatuanId,
    decimal Qty,
    int Iter,
    string Signa,
    string Instruction,
    string Note,
    IEnumerable<ResepCreateItemRacikCommand> ListItemRacik,
    bool IsRacik = false,
    int? Frequency = null,
    decimal? UnitDose = null,
    string? Route = null,
    string? MealRelation = null,
    string? UddTime = null);

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
    private readonly IResepCreateDal _resepDal;

    public ResepCreateHandler(
        IRegRepo regRepo,
        IPpaRepo ppaRepo,
        ILayananRepo layananRepo,
        ITipeBrgRepo tipeBrgRepo,
        IBrgRepo brgRepo,
        ISatuanRepo satuanRepo,
        IResepRepo resepRepo,
        IResepCreateDal resepDal)
    {
        _regRepo = Guard.Against.Null(regRepo, nameof(regRepo));
        _ppaRepo = Guard.Against.Null(ppaRepo, nameof(ppaRepo));
        _layananRepo = Guard.Against.Null(layananRepo, nameof(layananRepo));
        _tipeBrgRepo = Guard.Against.Null(tipeBrgRepo, nameof(tipeBrgRepo));
        _brgRepo = Guard.Against.Null(brgRepo, nameof(brgRepo));
        _satuanRepo = Guard.Against.Null(satuanRepo, nameof(satuanRepo));
        _resepRepo = Guard.Against.Null(resepRepo, nameof(resepRepo));
        _resepDal = Guard.Against.Null(resepDal, nameof(resepDal));
    }

    public Task<ResepCreateResponse> Handle(ResepCreateCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        if (request.ListObat.IsNullOrEmpty())
            throw new InvalidOperationException("Resep harus memiliki minimal 1 item obat.");
        if (request.Iter < 0)
            throw new ArgumentException("Iter tidak boleh bernilai negatif.", nameof(request.Iter));

        // BUILD
        var reg = LoadReg(request);
        var layanan = LoadLayanan(request);
        var dokter = LoadDokter(request);
        var tipeBrg = LoadTipeBrg(request);
        var bodyMetric = new BodyMetricType(request.BeratBadan, request.TinggiBadan, request.Lpb);
        var urgenitas = string.IsNullOrWhiteSpace(request.UrgenitasId)
            ? UrgenitasType.Default
            : UrgenitasType.Load(request.UrgenitasId, string.Empty);
        var layananStok = LoadLayananStok(request.LayananStokId);
        var tglRencana = request.TglRencanaPeriksa ?? DateTime.Now;

        // CHECK POTENTIAL DUPLICATES (EX-04)
        if (!request.IsConfirmedDuplicate)
        {
            var brgIds = request.ListObat
                .Select(x => x.BrgId)
                .Concat(request.ListObat.Where(x => x.ListItemRacik != null).SelectMany(x => x.ListItemRacik.Select(r => r.BrgId)))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var duplicates = _resepDal.FindPotentialDuplicate(reg.Pasien.PasienId, tglRencana.Date, brgIds);
            if (duplicates != null && duplicates.Any())
            {
                throw new ArgumentException("Terdeteksi potensi duplikasi resep pada hari yang sama. Harap konfirmasi sebelum melanjutkan.");
            }
        }

        var resep = ResepModel.Create(
            reg,
            bodyMetric,
            dokter,
            layanan,
            urgenitas,
            tipeBrg,
            request.Iter,
            request.Description,
            request.UserId,
            layananStok,
            tglRencana,
            request.ResepAsalReffId);

        resep = BuildItemResep(resep, request.ListObat);
        resep.Validate();

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

    private LayananType? LoadLayananStok(string? layananStokId)
    {
        if (string.IsNullOrWhiteSpace(layananStokId))
            return null;

        var key = LayananType.Key(layananStokId);
        return _layananRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Layanan Stok '{layananStokId}' not found")
            );
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
            var items = obat.ListItemRacik?.ToList() ?? new List<ResepCreateItemRacikCommand>();
            var isRacik = obat.IsRacik || items.Any();

            if (isRacik)
            {
                if (!items.Any())
                    throw new InvalidOperationException($"Wadah racikan '{obat.BrgId}' harus memiliki minimal 1 bahan komponen.");

                var brg = BrgObatType.Default with { BrgId = obat.BrgId, BrgName = obat.BrgId };
                var satuanKey = SatuanType.Key(obat.SatuanId);
                var satuan = LoadSatuan(satuanKey);

                resep.AddRacik(
                    brg,
                    satuan,
                    obat.Qty,
                    obat.Iter,
                    obat.Signa,
                    obat.Instruction,
                    obat.Frequency,
                    obat.UnitDose,
                    obat.Route ?? AppConst.DASH,
                    obat.MealRelation ?? AppConst.DASH,
                    obat.UddTime ?? AppConst.DASH,
                    obat.Note ?? AppConst.DASH);

                foreach (var item in items)
                {
                    var itemRacikKey = BrgObatType.Key(item.BrgId);
                    var itemRacik = LoadBrg(itemRacikKey);

                    var satuanItemRacikKey = SatuanType.Key(item.SatuanId);
                    var satuanItemRacik = LoadSatuan(satuanItemRacikKey);

                    resep.AddItemRacik(brg, itemRacik, satuanItemRacik, item.Qty, item.Dosis, item.DosisTxt);
                }
            }
            else
            {
                var brgKey = BrgObatType.Key(obat.BrgId);
                var brg = LoadBrg(brgKey);

                var satuanKey = SatuanType.Key(obat.SatuanId);
                var satuan = LoadSatuan(satuanKey);

                resep.AddObat(
                    brg,
                    satuan,
                    obat.Qty,
                    obat.Iter,
                    obat.Signa,
                    obat.Instruction,
                    obat.Frequency,
                    obat.UnitDose,
                    obat.Route ?? AppConst.DASH,
                    obat.MealRelation ?? AppConst.DASH,
                    obat.UddTime ?? AppConst.DASH,
                    obat.Note ?? AppConst.DASH);
            }
        }

        return resep;
    }
}