using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Application.IgdContext.Integration;
using Bilreg.Domain.IgdContext.IgdVisitSmassTaskFeature;
using MediatR;
using Microsoft.Extensions.Options;

namespace Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature.UseCases;

/// <summary>
/// Batch variant of <see cref="IgdVisitSmassTaskRetryCmd"/> for the operator worklist
/// (architecture §5.2, §6.2, §6.3, D-07, BR-12, BR-33). It loops every Failed task
/// (<c>ListProcessable</c>, oldest first) with exactly the same retry semantics as the
/// single-task command.
/// </summary>
public record IgdVisitSmassTaskProcessCmd : IRequest<IgdVisitSmassTaskProcessResult>;

public record IgdVisitSmassTaskProcessResult(int Total, int Succeeded, int Failed);

public class IgdVisitSmassTaskProcessHandler
    : IRequestHandler<IgdVisitSmassTaskProcessCmd, IgdVisitSmassTaskProcessResult>
{
    private readonly IIgdVisitSmassTaskRepo _taskRepo;
    private readonly IIgdVisitRepo _igdVisitRepo;
    private readonly IRegRepo _regRepo;
    private readonly ISmassAssessmentGateway _gateway;
    private readonly IgdVisitOptions _options;

    public IgdVisitSmassTaskProcessHandler(
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

    public async Task<IgdVisitSmassTaskProcessResult> Handle(
        IgdVisitSmassTaskProcessCmd request,
        CancellationToken cancellationToken)
    {
        var tasks = _taskRepo.ListProcessable().ToList();
        var succeeded = 0;
        var failed = 0;

        foreach (var task in tasks)
        {
            try
            {
                await IgdVisitSmassTaskRetryExecutor.ExecuteAsync(
                    _taskRepo, _igdVisitRepo, _regRepo, _gateway, _options, task, cancellationToken);

                if (task.TaskStatus == SmassTaskStatusEnum.Succeeded)
                    succeeded++;
                else
                    failed++;
            }
            catch
            {
                // One task must never abort the batch; the row stays Failed and is
                // reported in the summary (it remains in the worklist).
                failed++;
            }
        }

        return new IgdVisitSmassTaskProcessResult(tasks.Count, succeeded, failed);
    }
}
