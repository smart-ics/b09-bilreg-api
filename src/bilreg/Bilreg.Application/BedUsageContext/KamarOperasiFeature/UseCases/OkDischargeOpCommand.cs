using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using System.Globalization;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkDischargeOpCommand(string OrderOpId,
    string dischargeDate, string dischargeTime,
    int patientCondition, string KamarId,
    string postOpNote, string userId) : IOrderOpKey, IKamarKey, IRequest<OkDischargeOpResponse>;

public record OkDischargeOpResponse(string OrderOpId, string CurrentState, string DischargeAt, KamarReff targetWard);

public class OkDischargeOpHandler : IRequestHandler<OkDischargeOpCommand, OkDischargeOpResponse>
{
    private readonly IDischargeOpRepo _dischargeOpRepo;
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IKamarRepo _kamarRepo;
    private readonly IOpCaseRepo _opCaseRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public OkDischargeOpHandler(IDischargeOpRepo dischargeOpRepo,
        IOrderOpRepo orderOpRepo,
        IKamarRepo kamarRepo,
        IOpCaseRepo opCaseRepo,
        ITglJamProvider tglJamProvider)
    {
        _dischargeOpRepo = dischargeOpRepo;
        _orderOpRepo = orderOpRepo;
        _kamarRepo = kamarRepo;
        _opCaseRepo = opCaseRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<OkDischargeOpResponse> Handle(OkDischargeOpCommand request, CancellationToken cancellationToken)
    {
        var occurredAt = _tglJamProvider.Now;
        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(request.OrderOpId))
            .GetValueOrThrow($"Order Operasi ID {request.OrderOpId} tidak ditemukan.");

        var opCase = _opCaseRepo.LoadEntity(orderOp)
            .GetValueOrThrow("Invalid Order Operasi. OpCase data tidak ditemukan.");

        var kamar = _kamarRepo.LoadEntity(KamarType.Key(request.KamarId))
            .GetValueOrThrow($"Kamar ID {request.KamarId} tidak ditemukan.");

        var dischargeDateTime = DateTime.ParseExact($"{request.dischargeDate} {request.dischargeTime}",
            "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        var newDischarge = DischargeOpModel.Create(orderOp, dischargeDateTime, kamar, (PatientConditionEnum)request.patientCondition,
            request.postOpNote, request.userId, occurredAt);

        opCase.Discharge(newDischarge, occurredAt);

        using var trans = TransHelper.NewScope();
        _dischargeOpRepo.SaveChanges(newDischarge);
        _opCaseRepo.SaveChanges(opCase);
        trans.Complete();

        return Task.FromResult(new OkDischargeOpResponse(orderOp.OrderOpId, OpCaseStateEnum.Discharged.ToString(),
            $"{ request.dischargeDate} { request.dischargeTime}", new KamarReff(kamar.KamarId, kamar.KamarName)));
    }
}
