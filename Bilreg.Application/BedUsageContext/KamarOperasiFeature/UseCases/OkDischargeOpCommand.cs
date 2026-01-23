using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkDischargeOpCommand(string OrderOpId,
    string dischargeDate, string dischargeTime,
    string patientCondition, string KamarId,
    string postOpNote, string userId) : IOrderOpKey, IKamarKey, IRequest<OkDischargeOpResponse>;

public record OkDischargeOpResponse(string OrderOpId, string CurrentState, string DischargeAt, KamarReff targetWard);

public class OkDischargeOpHandler : IRequestHandler<OkDischargeOpCommand, OkDischargeOpResponse>
{
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IOpCaseRepo _opCaseRepo;

    public OkDischargeOpHandler(IOrderOpRepo orderOpRepo,
        IOpCaseRepo opCaseRepo)
    {
        _orderOpRepo = orderOpRepo;
        _opCaseRepo = opCaseRepo;
    }

    public Task<OkDischargeOpResponse> Handle(OkDischargeOpCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
