using Bilreg.Application.ApotekContext.DispensingFeature.UseCases;
using Bilreg.Application.ApotekContext.IntegrationFeature.UseCases;
using Bilreg.Application.ApotekContext.InvoiceFeature.UseCases;
using Bilreg.Application.ApotekContext.JualBebasFeature.UseCases;
using Bilreg.Application.ApotekContext.QueueFeature.UseCases;
using Bilreg.Application.ApotekContext.ResepKerjaFeature.UseCases;
using Bilreg.Application.ApotekContext.SalesOrderFeature.UseCases;
using Bilreg.Application.ApotekContext.TelaahResepFeature.UseCases;
using Bilreg.Application.ApotekContext.WorklistFeature;
using Bilreg.Application.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.ApotekContext;

[Route("api/v1/apotek")]
[ApiController]
[Authorize]
[ServiceFilter(typeof(ApotekExceptionFilter))]
public class ApotekController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserContext _user;

    public ApotekController(IMediator mediator, ICurrentUserContext user)
    {
        _mediator = mediator;
        _user = user;
    }

    [HttpPost("resep-kerja/intake-electronic")]
    public async Task<IActionResult> IntakeElectronic(ResepKerjaIntakeElectronicCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("resep-kerja/intake-physical")]
    public async Task<IActionResult> IntakePhysical(ResepKerjaIntakePhysicalCmd cmd)
    {
        Response.Headers["X-Release-Gate"] = "BC-13";
        return Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));
    }

    [HttpPost("jual-bebas/accept")]
    public async Task<IActionResult> AcceptJualBebas(JualBebasAcceptCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("jual-bebas/decline-after-accept")]
    public async Task<IActionResult> DeclineJualBebas(JualBebasDeclineAfterAcceptCmd cmd)
    {
        await _mediator.Send(cmd with { UserId = AptActor.Require(_user) });
        return Ok(new JSendOk("declined"));
    }

    [HttpPost("telaah/start")]
    public async Task<IActionResult> TelaahStart(TelaahStartCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("telaah/item")]
    public async Task<IActionResult> TelaahItem(TelaahUpdateItemCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("telaah/complete")]
    public async Task<IActionResult> TelaahComplete(TelaahCompleteCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("sales-order/establish")]
    public async Task<IActionResult> EstablishSalesOrder(SalesOrderEstablishCmd cmd)
    {
        Response.Headers["X-Release-Gate"] = "PD-09";
        return Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));
    }

    [HttpPost("sales-order/unfulfilled")]
    public async Task<IActionResult> Unfulfilled(SalesOrderAppendUnfulfilledCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("sales-order/coverage")]
    public async Task<IActionResult> Coverage(SalesOrderApplyCoverageCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("queue/map")]
    public async Task<IActionResult> Map(QueueMapCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("queue/close")]
    public async Task<IActionResult> Close(QueueCloseCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("invoice/establish")]
    public async Task<IActionResult> InvoiceEstablish(InvoiceEstablishCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("invoice/issue")]
    public async Task<IActionResult> InvoiceIssue(InvoiceIssueCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("invoice/payment")]
    public async Task<IActionResult> InvoicePayment(InvoiceRecordPaymentCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("invoice/revise")]
    public async Task<IActionResult> InvoiceRevise(InvoiceReviseCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("invoice/correction")]
    public async Task<IActionResult> InvoiceCorrection(InvoiceRecordCorrectionCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("dispensing/establish")]
    public async Task<IActionResult> DispensingEstablish(DispensingEstablishCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("dispensing/release")]
    public async Task<IActionResult> DispensingRelease(DispensingReleaseCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("dispensing/start")]
    public async Task<IActionResult> DispensingStart(DispensingStartCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("dispensing/prepare")]
    public async Task<IActionResult> DispensingPrepare(DispensingPrepareCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("dispensing/pickup-call")]
    public async Task<IActionResult> PickupCall(DispensingPickupCallCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("dispensing/final-review")]
    public async Task<IActionResult> FinalReview(DispensingFinalReviewCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("dispensing/education")]
    public async Task<IActionResult> Education(DispensingEducationCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("dispensing/override")]
    public async Task<IActionResult> OverrideWindow(DispensingOverrideCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("dispensing/handover")]
    public async Task<IActionResult> Handover(DispensingHandoverCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpPost("dispensing/no-show")]
    public async Task<IActionResult> NoShow(DispensingNoShowCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));

    [HttpGet("worklist/telaah")]
    public async Task<IActionResult> TelaahWorklist()
        => Ok(new JSendOk(await _mediator.Send(new TelaahWorklistQuery())));

    [HttpGet("worklist/pelayanan")]
    public async Task<IActionResult> Pelayanan([FromQuery] string antrianId, [FromQuery] int? noUrut)
        => Ok(new JSendOk(await _mediator.Send(new PelayananWorklistQuery(antrianId ?? "", noUrut))));

    [HttpGet("worklist/dispensing")]
    public async Task<IActionResult> DispensingWorklist()
        => Ok(new JSendOk(await _mediator.Send(new DispensingWorklistQuery())));

    [HttpGet("worklist/serah")]
    public async Task<IActionResult> Serah([FromQuery] DateTime? asOf)
        => Ok(new JSendOk(await _mediator.Send(new SerahWorklistQuery(asOf ?? DateTime.Now))));

    [HttpGet("journey")]
    public async Task<IActionResult> Journey([FromQuery] string antrianId, [FromQuery] int noUrut)
        => Ok(new JSendOk(await _mediator.Send(new JourneyQuery(antrianId, noUrut))));

    [HttpGet("reporting/unified-sales")]
    public async Task<IActionResult> UnifiedSales([FromQuery] DateTime date1, [FromQuery] DateTime date2)
        => Ok(new JSendOk(await _mediator.Send(new UnifiedSalesReportQuery(date1, date2))));

    [HttpGet("integration/failures")]
    public async Task<IActionResult> Failures([FromQuery] AptIntegrationFailureQuery query)
        => Ok(new JSendOk(await _mediator.Send(query)));

    [HttpPost("integration/retry")]
    public async Task<IActionResult> Retry(AptIntegrationRetryCmd cmd)
        => Ok(new JSendOk(await _mediator.Send(cmd with { UserId = AptActor.Require(_user) })));
}
