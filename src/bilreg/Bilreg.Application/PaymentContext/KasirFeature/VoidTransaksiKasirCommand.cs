using System.Transactions;
using Ardalis.GuardClauses;
using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Domain.Shared;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public record VoidTransaksiKasirCommand(
    string TrsKasirId,
    string SupervisorUserId,
    string SupervisorPassword,
    string AlasanVoid,
    string PetugasVoid = "") : IRequest<VoidTransaksiKasirResponse>, ITrsKasirKey;

public record VoidTransaksiKasirResponse(
    string TrsKasirId,
    string StatusTrs,
    string TglVoid,
    string JamVoid,
    string PetugasVoid,
    string AlasanVoid);

public class VoidTransaksiKasirHandler : IRequestHandler<VoidTransaksiKasirCommand, VoidTransaksiKasirResponse>
{
    private readonly ITrsKasirRepo _trsKasirRepo;
    private readonly ITrsKasirLegacyDal _trsKasirLegacyDal;
    private readonly IKasirSupervisorAuthDal _supervisorAuthDal;
    private readonly IKasirStatusSyncDal _statusSyncDal;
    private readonly ITglJamProvider _tglJamProvider;

    public VoidTransaksiKasirHandler(
        ITrsKasirRepo trsKasirRepo,
        ITrsKasirLegacyDal trsKasirLegacyDal,
        IKasirSupervisorAuthDal supervisorAuthDal,
        IKasirStatusSyncDal statusSyncDal,
        ITglJamProvider tglJamProvider)
    {
        _trsKasirRepo = trsKasirRepo;
        _trsKasirLegacyDal = trsKasirLegacyDal;
        _supervisorAuthDal = supervisorAuthDal;
        _statusSyncDal = statusSyncDal;
        _tglJamProvider = tglJamProvider;
    }

    public Task<VoidTransaksiKasirResponse> Handle(VoidTransaksiKasirCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.TrsKasirId, nameof(request.TrsKasirId));
        Guard.Against.NullOrWhiteSpace(request.SupervisorUserId, nameof(request.SupervisorUserId));
        Guard.Against.NullOrWhiteSpace(request.SupervisorPassword, nameof(request.SupervisorPassword));
        Guard.Against.NullOrWhiteSpace(request.AlasanVoid, nameof(request.AlasanVoid));

        if (request.AlasanVoid.Trim().Length < 10)
        {
            throw new BusinessValidationException("Alasan void minimal 10 karakter.");
        }

        // Rule 1: Validasi kredensial supervisor via IKasirSupervisorAuthDal
        var isValidSupervisor = _supervisorAuthDal.ValidateSupervisor(request.SupervisorUserId, request.SupervisorPassword);
        if (!isValidSupervisor)
        {
            throw new BusinessValidationException("Kredensial supervisor tidak valid atau user tidak memiliki hak otorisasi void kasir.");
        }

        // Rule 2: Ambil transaksi dan validasi status BERHASIL
        var model = _trsKasirRepo.GetKey(request.TrsKasirId)
            ?? throw new KeyNotFoundException($"Transaksi kasir {request.TrsKasirId} tidak ditemukan.");

        if (model.IsVoid || model.StatusTrs != KasirConst.StatusTrsBerhasil)
        {
            throw new BusinessValidationException($"Transaksi kasir {request.TrsKasirId} sudah berstatus VOID dan tidak dapat dibatalkan lagi.");
        }

        var petugasVoid = !string.IsNullOrWhiteSpace(request.PetugasVoid)
            ? request.PetugasVoid.Trim()
            : request.SupervisorUserId.Trim();

        // Rule 3: Eksekusi void dalam satu transaksi atomik
        using var scope = TransHelper.NewScope();

        model.Void(petugasVoid, request.AlasanVoid, _tglJamProvider.Now);
        _trsKasirRepo.Save(model);
        _trsKasirLegacyDal.Void(model.TrsKasirId, model.TglVoid, model.JamVoid, petugasVoid);
        _statusSyncDal.Reversal(model.OrderRef, model.TrsKasirId, model.RegId);

        scope.Complete();

        var response = new VoidTransaksiKasirResponse(
            TrsKasirId: model.TrsKasirId,
            StatusTrs: model.StatusTrs,
            TglVoid: model.TglVoid,
            JamVoid: model.JamVoid,
            PetugasVoid: model.PetugasVoid,
            AlasanVoid: model.AlasanVoid);

        return Task.FromResult(response);
    }
}
