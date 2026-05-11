using Bilreg.Application.IgdContext.BedIgdFeature;
using Bilreg.Domain.IgdContext.BedIgdFeature;

namespace Bilreg.Infrastructure.IgdContext.BedIgdFeature;

public record PakaiBedDto(
    string PakaiBedId,
    string IgdVisitId,
    string BedIgdId,
    string BedIgdName,
    DateTime CheckInDateTime,
    string CheckInUserId,
    DateTime CheckOutDateTime,
    string CheckOutUserId)
{
    public static PakaiBedDto FromModel(PakaiBedModel model)
        => new(
            PakaiBedId: model.PakaiBedId,
            IgdVisitId: model.IgdVisitId,
            BedIgdId: model.BedIgdId,
            BedIgdName: model.BedIgdName,
            CheckInDateTime: model.CheckInDateTime,
            CheckInUserId: model.CheckInUserId,
            CheckOutDateTime: model.CheckOutDateTime,
            CheckOutUserId: model.CheckOutUserId == "-" ? "" : model.CheckOutUserId);

    public PakaiBedModel ToModel()
        => new(
            pakaiBedId: PakaiBedId,
            igdVisitId: IgdVisitId,
            bedIgdId: BedIgdId,
            bedIgdName: BedIgdName,
            checkInDateTime: CheckInDateTime,
            checkInUserId: CheckInUserId,
            checkOutDateTime: CheckOutDateTime,
            checkOutUserId: string.IsNullOrEmpty(CheckOutUserId) ? "-" : CheckOutUserId);

    public PakaiBedView ToView()
        => new(
            PakaiBedId: PakaiBedId,
            IgdVisitId: IgdVisitId,
            BedIgdId: BedIgdId,
            BedIgdName: BedIgdName,
            CheckInDateTime: CheckInDateTime,
            CheckOutDateTime: CheckOutDateTime);
}
