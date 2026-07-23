using Bilreg.Application.IgdContext.BedIgdFeature;
using Bilreg.Domain.IgdContext.BedIgdFeature;

namespace Bilreg.Infrastructure.IgdContext.BedIgdFeature;

public record PakaiBedIgdDto(
    string PakaiBedIgdId,
    string IgdVisitId,
    string BedIgdId,
    string BedIgdName,
    DateTime CheckInDateTime,
    string CheckInUserId,
    DateTime CheckOutDateTime,
    string CheckOutUserId)
{
    public static PakaiBedIgdDto FromModel(PakaiBedIgdModel model)
        => new(
            PakaiBedIgdId: model.PakaiBedIgdId,
            IgdVisitId: model.IgdVisitId,
            BedIgdId: model.BedIgdId,
            BedIgdName: model.BedIgdName,
            CheckInDateTime: model.CheckInDateTime,
            CheckInUserId: model.CheckInUserId,
            CheckOutDateTime: model.CheckOutDateTime,
            CheckOutUserId: model.CheckOutUserId == "-" ? "" : model.CheckOutUserId);

    public PakaiBedIgdModel ToModel()
        => new(
            pakaiBedIgdId: PakaiBedIgdId,
            igdVisitId: IgdVisitId,
            bedIgdId: BedIgdId,
            bedIgdName: BedIgdName,
            checkInDateTime: CheckInDateTime,
            checkInUserId: CheckInUserId,
            checkOutDateTime: CheckOutDateTime,
            checkOutUserId: string.IsNullOrEmpty(CheckOutUserId) ? "-" : CheckOutUserId);

    public PakaiBedIgdView ToView()
        => new(
            PakaiBedIgdId: PakaiBedIgdId,
            IgdVisitId: IgdVisitId,
            BedIgdId: BedIgdId,
            BedIgdName: BedIgdName,
            CheckInDateTime: CheckInDateTime,
            CheckOutDateTime: CheckOutDateTime);
}
