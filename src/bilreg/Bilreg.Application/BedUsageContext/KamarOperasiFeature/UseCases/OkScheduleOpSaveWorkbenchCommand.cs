using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using System.Globalization;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record SaveWorkbenchResourceDto(
    string Id = "",
    string Category = "",
    string Name = "",
    int Qty = 1,
    string Unit = "",
    string? Notes = null
);

public record SaveWorkbenchResourceInput(
    string Id = "",
    string Category = "",
    string Name = "",
    int Qty = 1,
    string Unit = "",
    string? Notes = null
) : SaveWorkbenchResourceDto(Id, Category, Name, Qty, Unit, Notes);

public record OkScheduleOpSaveWorkbenchCommand(
    string OrderOpId,
    string KamarId,
    string Tgl,
    string Jam,
    int Durasi,
    string UserId,
    string DokterOperatorId = "",
    List<string>? ListPpaIds = null,
    int TurnoverBuffer = 30,
    string AlasanReschedule = "",
    string AlasanAssignLeader = "",
    bool IsOverride = false,
    string OverrideReason = "",
    bool Publish = false,
    List<SaveWorkbenchResourceDto>? Resources = null
) : IRequest<OkScheduleOpSaveWorkbenchResponse>, IOrderOpKey;

public record OkScheduleOpSaveWorkbenchResponse(
    string ScheduleOpId,
    bool IsDraft,
    bool Rescheduled,
    bool LeaderChanged,
    bool PpaChanged,
    bool Published,
    bool ResourceChanged = false,
    List<OrderOpResourceDto>? Resources = null
);

public class OkScheduleOpSaveWorkbenchHandler : IRequestHandler<OkScheduleOpSaveWorkbenchCommand, OkScheduleOpSaveWorkbenchResponse>
{
    private readonly IScheduleOpRepo _scheduleOpRepo;
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IKamarRepo _kamarRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly IOpCaseRepo _opCaseRepo;
    private readonly IOrderOpResourceRepo _resourceRepo;
    private readonly IMediator _mediator;
    private readonly ITglJamProvider _tglJamProvider;

    public OkScheduleOpSaveWorkbenchHandler(
        IScheduleOpRepo scheduleOpRepo,
        IOrderOpRepo orderOpRepo,
        IKamarRepo kamarRepo,
        IPpaRepo ppaRepo,
        IOpCaseRepo opCaseRepo,
        IOrderOpResourceRepo resourceRepo,
        IMediator mediator,
        ITglJamProvider tglJamProvider)
    {
        _scheduleOpRepo = scheduleOpRepo;
        _orderOpRepo = orderOpRepo;
        _kamarRepo = kamarRepo;
        _ppaRepo = ppaRepo;
        _opCaseRepo = opCaseRepo;
        _resourceRepo = resourceRepo;
        _mediator = mediator;
        _tglJamProvider = tglJamProvider;
    }

    public async Task<OkScheduleOpSaveWorkbenchResponse> Handle(OkScheduleOpSaveWorkbenchCommand request, CancellationToken cancellationToken)
    {
        var occurredAt = _tglJamProvider.Now;

        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.OrderOpId, nameof(request.OrderOpId));
        Guard.Against.NullOrWhiteSpace(request.KamarId, nameof(request.KamarId));
        Guard.Against.InvalidDateFormat(request.Tgl, nameof(request.Tgl));
        Guard.Against.NegativeOrZero(request.Durasi, nameof(request.Durasi));

        var requestTglOp = DateTime.ParseExact($"{request.Tgl} {request.Jam}",
            "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(request.OrderOpId))
            .GetValueOrThrow($"Order Operasi ID {request.OrderOpId} tidak ditemukan.");

        var kamar = _kamarRepo.LoadEntity(KamarType.Key(request.KamarId))
            .GetValueOrThrow($"Kamar Operasi ID {request.KamarId} tidak ditemukan.");

        var opCase = _opCaseRepo.LoadEntity(orderOp)
            .GetValueOrThrow("Invalid Order Operasi. OpCase data tidak ditemukan.");

        // Load existing active schedule
        var listSchedule = _scheduleOpRepo.ListData(PasienModel.Key(orderOp.Pasien.PasienId))?.ToList() ?? [];
        var scheduleWithOrderOpId = listSchedule
            .Where(x => !x.IsVoid)
            .FirstOrDefault(x => x.OrderOp.OrderOpId == request.OrderOpId);
        var scheduleOp = scheduleWithOrderOpId != null
            ? _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(scheduleWithOrderOpId.ScheduleOpId)).GetValueOrDefault()
            : null;

        bool isDraftBefore = (opCase.OrderOpState == OpCaseStateEnum.Requested);
        bool isOfficial = (opCase.OrderOpState == OpCaseStateEnum.Scheduled);

        // Leader resolution (TD-15, TD-45)
        var existingLeaderId = scheduleOp?.TeamLead?.PpaId ?? string.Empty;
        var targetLeaderId = !string.IsNullOrWhiteSpace(request.DokterOperatorId)
            ? request.DokterOperatorId
            : (!string.IsNullOrWhiteSpace(existingLeaderId) ? existingLeaderId : orderOp.Dokter.PpaId);

        bool isLeaderChanged = !string.IsNullOrWhiteSpace(request.DokterOperatorId)
            && !string.Equals(request.DokterOperatorId, existingLeaderId, StringComparison.OrdinalIgnoreCase);

        PpaType targetLeaderPpa = PpaType.Default;
        if (!string.IsNullOrWhiteSpace(targetLeaderId) && targetLeaderId != "-")
        {
            targetLeaderPpa = _ppaRepo.LoadEntity(PpaType.Key(targetLeaderId))
                .GetValueOrThrow($"PPA Dokter Operator ID {targetLeaderId} tidak ditemukan.");
        }

        // Schedule delta
        bool isScheduleChanged = (scheduleOp == null)
            || (scheduleOp.KamarOp.KamarId != request.KamarId
                || scheduleOp.TglOp != requestTglOp
                || scheduleOp.Durasi != request.Durasi);

        // PPA delta
        bool isPpaChanged = false;
        if (request.ListPpaIds != null)
        {
            var currentSupportingPpaIds = scheduleOp?.ListPpa
                .Where(x => x.Ppa.PpaId != targetLeaderId)
                .Select(x => x.Ppa.PpaId)
                .ToHashSet() ?? new HashSet<string>();

            var requestedSupportingPpaIds = request.ListPpaIds
                .Where(x => !string.IsNullOrWhiteSpace(x) && x != targetLeaderId)
                .ToHashSet();

            isPpaChanged = !currentSupportingPpaIds.SetEquals(requestedSupportingPpaIds);
        }

        // Resource delta
        bool isResourceChanged = false;
        List<OrderOpResourceDto> existingResources = [];
        if (request.Resources != null)
        {
            existingResources = _resourceRepo.ListData(request.OrderOpId).ToList();
            if (existingResources.Count != request.Resources.Count)
            {
                isResourceChanged = true;
            }
            else
            {
                var existingMap = existingResources.ToDictionary(x => x.OrderOpResourceId, x => x);
                foreach (var r in request.Resources)
                {
                    if (string.IsNullOrWhiteSpace(r.Id) || !existingMap.TryGetValue(r.Id, out var existingItem))
                    {
                        isResourceChanged = true;
                        break;
                    }

                    var reqCat = OrderOpResourceDto.MapResourceTypeToCategory(
                        OrderOpResourceDto.MapCategoryToResourceType(r.Category), r.Category);

                    if (!string.Equals(existingItem.Name, r.Name, StringComparison.Ordinal) ||
                        existingItem.Qty != r.Qty ||
                        !string.Equals(existingItem.Unit, r.Unit, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(existingItem.Category, reqCat, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(existingItem.Notes ?? string.Empty, r.Notes ?? string.Empty, StringComparison.Ordinal))
                    {
                        isResourceChanged = true;
                        break;
                    }
                }
            }
        }

        bool willPublish = request.Publish && isDraftBefore;

        // No-Op safe guard: no delta, no publish requested, existing schedule exists
        bool hasDelta = isScheduleChanged || isLeaderChanged || isPpaChanged || isResourceChanged || willPublish;
        if (!hasDelta && scheduleOp != null)
        {
            var currentResources = existingResources.Count > 0
                ? existingResources
                : _resourceRepo.ListData(request.OrderOpId).ToList();

            return new OkScheduleOpSaveWorkbenchResponse(
                ScheduleOpId: scheduleOp.ScheduleOpId,
                IsDraft: isDraftBefore,
                Rescheduled: false,
                LeaderChanged: false,
                PpaChanged: false,
                Published: false,
                ResourceChanged: false,
                Resources: currentResources
            );
        }

        // Selective reason validation on official schedule (TD-46, TD-57)
        if (isOfficial)
        {
            if (isScheduleChanged)
            {
                if (string.IsNullOrWhiteSpace(request.AlasanReschedule) || request.AlasanReschedule.Trim().Length < 5)
                {
                    throw new ArgumentException("Alasan perubahan jadwal wajib diisi minimal 5 karakter.");
                }
            }

            if (isLeaderChanged)
            {
                if (string.IsNullOrWhiteSpace(request.AlasanAssignLeader) || request.AlasanAssignLeader.Trim().Length < 5)
                {
                    throw new ArgumentException("Alasan perubahan dokter operator wajib diisi minimal 5 karakter.");
                }
            }
        }

        // Mutate schedule
        ScheduleOpModel newScheduleOp;
        bool shouldClone = isScheduleChanged || isLeaderChanged || isPpaChanged || willPublish;

        if (scheduleOp != null)
        {
            if (shouldClone)
            {
                scheduleOp.CancelSchedule(request.UserId, occurredAt);
                newScheduleOp = ScheduleOpModel.CloneFrom(scheduleOp, occurredAt);
                newScheduleOp.SetSchedule(requestTglOp, kamar.ToReff(), request.Durasi, request.UserId, occurredAt);
            }
            else
            {
                newScheduleOp = scheduleOp;
            }
        }
        else
        {
            newScheduleOp = ScheduleOpModel.CreateFromOrder(orderOp, request.UserId, kamar, targetLeaderPpa, requestTglOp, occurredAt);
            newScheduleOp.SetSchedule(requestTglOp, kamar.ToReff(), request.Durasi, request.UserId, occurredAt);
        }

        // Mutate leader if needed
        if (targetLeaderPpa != null && targetLeaderPpa != PpaType.Default && shouldClone)
        {
            if (!newScheduleOp.ListPpa.Any(x => x.Ppa.PpaId == targetLeaderPpa.PpaId))
            {
                newScheduleOp.AddPpa(targetLeaderPpa, request.UserId, occurredAt);
            }
            if (newScheduleOp.TeamLead?.PpaId != targetLeaderPpa.PpaId)
            {
                newScheduleOp.AssignLeader(targetLeaderPpa, request.UserId, occurredAt);
            }

            var specialEq = string.IsNullOrWhiteSpace(orderOp.SpecialEquipment) ? "-" : orderOp.SpecialEquipment;
            orderOp.OperationalRequest(targetLeaderPpa, orderOp.EstimasiDurasiInMinutes, orderOp.PreferedDate, specialEq, orderOp.PreferedDate);
        }

        // Reconcile supporting PPAs if provided (TD-51)
        if (request.ListPpaIds != null && shouldClone)
        {
            var targetSupportingPpaIds = request.ListPpaIds
                .Where(x => !string.IsNullOrWhiteSpace(x) && x != targetLeaderId)
                .ToHashSet();

            var membersToRemove = newScheduleOp.ListPpa
                .Where(x => x.Ppa.PpaId != targetLeaderId && !targetSupportingPpaIds.Contains(x.Ppa.PpaId))
                .ToList();

            foreach (var member in membersToRemove)
            {
                var ppaEntity = _ppaRepo.LoadEntity(PpaType.Key(member.Ppa.PpaId)).GetValueOrDefault();
                if (ppaEntity != null)
                {
                    newScheduleOp.RemovePpa(ppaEntity, request.UserId, occurredAt);
                }
            }

            foreach (var ppaId in targetSupportingPpaIds)
            {
                if (!newScheduleOp.ListPpa.Any(x => x.Ppa.PpaId == ppaId))
                {
                    var ppaToAdd = _ppaRepo.LoadEntity(PpaType.Key(ppaId))
                        .GetValueOrThrow($"PPA ID {ppaId} tidak ditemukan.");
                    newScheduleOp.AddPpa(ppaToAdd, request.UserId, occurredAt);
                }
            }
        }

        // Lifecycle & publish determination (TD-31, TD-39, TD-49)
        bool shouldScheduleOpCase = willPublish || isOfficial;
        bool isDraftAfter;
        bool published;

        if (willPublish)
        {
            opCase.Schedule(newScheduleOp, occurredAt);
            isDraftAfter = false;
            published = true;
        }
        else if (isOfficial)
        {
            opCase.Schedule(newScheduleOp, occurredAt);
            isDraftAfter = false;
            published = false;
        }
        else
        {
            isDraftAfter = true;
            published = false;
        }

        // Atomic Transaction (TD-45, TD-56)
        List<OrderOpResourceDto> finalResources;

        using (var scope = TransHelper.NewScope())
        {
            if (shouldClone)
            {
                if (scheduleOp != null)
                    _scheduleOpRepo.SaveChanges(scheduleOp);
                _scheduleOpRepo.SaveChanges(newScheduleOp);

                if (shouldScheduleOpCase)
                    _opCaseRepo.SaveChanges(opCase);

                _orderOpRepo.SaveChanges(orderOp);
            }

            if (request.Resources != null)
            {
                var targetDtos = request.Resources.Select(r =>
                    OrderOpResourceDto.Create(
                        id: r.Id,
                        orderOpId: request.OrderOpId,
                        scheduleOpId: newScheduleOp.ScheduleOpId,
                        category: r.Category,
                        name: r.Name,
                        qty: r.Qty,
                        unit: r.Unit,
                        notes: r.Notes ?? string.Empty,
                        userId: request.UserId,
                        occurredAt: occurredAt
                    )
                ).ToList();

                finalResources = _resourceRepo.Reconcile(
                    request.OrderOpId,
                    newScheduleOp.ScheduleOpId,
                    targetDtos,
                    request.UserId
                );
            }
            else
            {
                finalResources = _resourceRepo.ListData(request.OrderOpId).ToList();
            }

            scope.Complete();
        }

        // Domain event suppression for drafts (TD-39 Revision, TD-45)
        if (isScheduleChanged && !isDraftAfter)
        {
            var domainEvent = new OkScheduleOpSetEvent(
                new OkScheduleOpSetCommand(request.OrderOpId, request.KamarId, request.Tgl, request.Jam, request.Durasi, request.UserId),
                newScheduleOp
            );
            await _mediator.Publish(domainEvent, cancellationToken);
        }

        return new OkScheduleOpSaveWorkbenchResponse(
            ScheduleOpId: newScheduleOp.ScheduleOpId,
            IsDraft: isDraftAfter,
            Rescheduled: isScheduleChanged,
            LeaderChanged: isLeaderChanged,
            PpaChanged: isPpaChanged,
            Published: published,
            ResourceChanged: isResourceChanged,
            Resources: finalResources
        );
    }
}
