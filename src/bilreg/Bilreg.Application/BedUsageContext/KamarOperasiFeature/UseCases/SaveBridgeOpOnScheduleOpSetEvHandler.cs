using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.Shared.Param;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public class SaveBridgeOpOnScheduleOpSetEvHandler : INotificationHandler<OkScheduleOpSetEvent>
{
    private readonly IGetProjectIdService _projectIdSvc;
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IBridgeOperasiSaveService _bridgeOperasiSaveService;
    private readonly IRegRepo _regRepo;

    public SaveBridgeOpOnScheduleOpSetEvHandler(IGetProjectIdService projectIdSvc,
        IOrderOpRepo orderOpRepo,
        IBridgeOperasiSaveService bridgeOperasiSaveService,
        IRegRepo regRepo)
    {
        _projectIdSvc = projectIdSvc;
        _orderOpRepo = orderOpRepo;
        _bridgeOperasiSaveService = bridgeOperasiSaveService;
        _regRepo = regRepo;
    }

    public Task Handle(OkScheduleOpSetEvent notification, CancellationToken cancellationToken)
    {
        var projectId = _projectIdSvc.Execute();

        // TODO: Bagaimana agar tdk perlu panggil orderOpRepo lagi
        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(notification.Command.OrderOpId))
            .GetValueOrDefault();
        if (orderOp == null) return Task.CompletedTask;

        var reg = _regRepo.LoadEntity(RegModel.Key(notification.Aggregate.Reg.RegId))
            .GetValueOrDefault();
        if (reg == null) return Task.CompletedTask;

        var namaOperasi = !string.IsNullOrWhiteSpace(orderOp.TarifOperasi?.TarifName) && orderOp.TarifOperasi.TarifName != "-"
            ? orderOp.TarifOperasi.TarifName
            : orderOp.NamaOperasi;

        var bridgeOpCommand = new BridgeOperasiCmd(projectId, reg.RegId,
            notification.Aggregate.TglOp.ToString(DateFormatEnum.YMD),
            namaOperasi, reg.Layanan.LayananId, reg.Layanan.LayananName,
            "0", reg.Polis.NoPolis, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        _bridgeOperasiSaveService.Execute(bridgeOpCommand);

        return Task.CompletedTask;
    }
}
