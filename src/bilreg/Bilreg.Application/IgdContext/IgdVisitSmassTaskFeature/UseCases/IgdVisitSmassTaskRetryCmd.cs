using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Application.IgdContext.Integration;
using Bilreg.Domain.IgdContext.IgdVisitSmassTaskFeature;
using MediatR;
using Microsoft.Extensions.Options;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature.UseCases;

/// <summary>
/// Manual retry of one Failed SMASS operation (architecture §5.2, §6.2, §6.3, D-07,
/// BR-12). The task is loaded, its state machine is validated
/// (<c>AssertCanManualRetry</c> — INV-T6/IR-01), the payload is rebuilt from immutable
/// sources (no <c>PayloadJson</c> is stored or replayed, AR-12), the gateway is invoked
/// exactly once and the outcome is recorded on the task row.
/// </summary>
public record IgdVisitSmassTaskRetryCmd(string IgdVisitSmassTaskId)
    : IRequest<IgdVisitSmassTaskView>, IIgdVisitSmassTaskKey;

public class IgdVisitSmassTaskRetryHandler
    : IRequestHandler<IgdVisitSmassTaskRetryCmd, IgdVisitSmassTaskView>
{
    private readonly IIgdVisitSmassTaskRepo _taskRepo;
    private readonly IIgdVisitRepo _igdVisitRepo;
    private readonly IRegRepo _regRepo;
    private readonly ISmassAssessmentGateway _gateway;
    private readonly IgdVisitOptions _options;

    public IgdVisitSmassTaskRetryHandler(
        IIgdVisitSmassTaskRepo taskRepo,
        IIgdVisitRepo igdVisitRepo,
        IRegRepo regRepo,
        ISmassAssessmentGateway gateway,
        IOptions<IgdVisitOptions> options)
    {
        _taskRepo = taskRepo;
        _igdVisitRepo = igdVisitRepo;
        _regRepo = regRepo;
        _gateway = gateway;
        _options = options.Value;
    }

    public async Task<IgdVisitSmassTaskView> Handle(
        IgdVisitSmassTaskRetryCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(
            request.IgdVisitSmassTaskId, nameof(request.IgdVisitSmassTaskId));

        var task = _taskRepo.LoadEntity(request)
            .GetValueOrThrow($"IgdVisitSmassTask '{request.IgdVisitSmassTaskId}' not found");

        // INV-T6 / IR-01 — manual retry only from Failed.
        task.AssertCanManualRetry();

        await IgdVisitSmassTaskRetryExecutor.ExecuteAsync(
            _taskRepo, _igdVisitRepo, _regRepo, _gateway, _options, task, cancellationToken);

        return IgdVisitSmassTaskView.FromModel(task);
    }
}
