using Ardalis.GuardClauses;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.MutasiFeature.UseCases;

public record TerimaMutasiConfirmCommand(
    string MutasiBrgId,
    string LayananPenerimaId,
    string UserId,
    string Note = "") : IRequest<TerimaMutasiConfirmResponse>, IMutasiBrgKey;

public record TerimaMutasiConfirmResponse(
    string TerimaMutasiId,
    string State) : ITerimaMutasiKey;

public class TerimaMutasiConfirmHandler : IRequestHandler<TerimaMutasiConfirmCommand, TerimaMutasiConfirmResponse>
{
    private readonly IMutasiBrgRepo _mutasiBrgRepo;
    private readonly ITerimaMutasiRepo _terimaMutasiRepo;
    private readonly ITerimaMutasiMovementService _movementService;
    private readonly ISqlDatabase _sqlDatabase;
    private readonly IAuditRepo _auditRepo;

    public TerimaMutasiConfirmHandler(
        IMutasiBrgRepo mutasiBrgRepo,
        ITerimaMutasiRepo terimaMutasiRepo,
        ITerimaMutasiMovementService movementService,
        ISqlDatabase sqlDatabase,
        IAuditRepo auditRepo)
    {
        _mutasiBrgRepo = mutasiBrgRepo;
        _terimaMutasiRepo = terimaMutasiRepo;
        _movementService = movementService;
        _sqlDatabase = sqlDatabase;
        _auditRepo = auditRepo;
    }

    public Task<TerimaMutasiConfirmResponse> Handle(TerimaMutasiConfirmCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.MutasiBrgId, nameof(request.MutasiBrgId));
        Guard.Against.NullOrWhiteSpace(request.LayananPenerimaId, nameof(request.LayananPenerimaId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        TerimaMutasiConfirmResponse? response = null;

        _sqlDatabase.ExecuteTransaction((conn, trans) =>
        {
            var mutasiMaybe = _mutasiBrgRepo.LoadEntityForUpdate(request, conn, trans);
            if (!mutasiMaybe.HasValue)
                throw new KeyNotFoundException($"MutasiBrg with Id '{request.MutasiBrgId}' not found.");

            var mutasi = mutasiMaybe.Value;

            if (mutasi.State != MutasiBrgStateEnum.Dispatched)
                throw new InvalidOperationException($"Hanya dokumen berstatus Dispatched yang dapat diterima. Status saat ini: {mutasi.State}.");

            if (!string.Equals(mutasi.LayananTujuan.LayananId?.Trim(), request.LayananPenerimaId.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Unit penerima '{request.LayananPenerimaId}' tidak sesuai dengan unit tujuan mutasi '{mutasi.LayananTujuan.LayananId}'.");

            var snapshotJson = AuditLogSnapshotJson.Serialize(mutasi);
            var receiveDate = DateTime.Now;

            var terimaItems = mutasi.ListItem
                .Select(item => TerimaMutasiItemModel.CreateConfirmed(
                    item.NoUrut,
                    item.Brg,
                    item.BatchNo,
                    item.ExpiredDate,
                    item.QtyKirim,
                    item.Satuan))
                .ToList();

            var terimaMutasi = TerimaMutasiModel.CreateConfirmed(
                mutasi.MutasiBrgId,
                mutasi.OrderMutasiId,
                mutasi.LayananAsal,
                mutasi.LayananTujuan,
                request.UserId,
                terimaItems,
                request.Note,
                receiveDate);

            _movementService.SettleReceivedStock(terimaMutasi, conn, trans);
            mutasi.MarkReceived(request.UserId, receiveDate);

            _terimaMutasiRepo.SaveChanges(terimaMutasi, conn, trans);
            _mutasiBrgRepo.SaveChanges(mutasi, conn, trans);

            var auditLog = AuditLog.Create(
                request.UserId.Trim(),
                receiveDate,
                actionType: "CONFIRM_TERIMA_MUTASI",
                entityName: nameof(TerimaMutasiModel),
                entityId: terimaMutasi.TerimaMutasiId,
                reason: string.IsNullOrWhiteSpace(request.Note) ? "Penerimaan mutasi penuh" : request.Note.Trim(),
                originalDataJson: snapshotJson,
                correlationId: mutasi.MutasiBrgId);
            _auditRepo.SaveChanges(auditLog);

            response = new TerimaMutasiConfirmResponse(terimaMutasi.TerimaMutasiId, terimaMutasi.State.ToString());
        });

        return Task.FromResult(response!);
    }
}
