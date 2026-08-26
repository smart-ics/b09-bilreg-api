using Bilreg.Domain.SalesContext.ReturJualFeature;

namespace Bilreg.Infrastructure.SalesContext.ReturJualFeature;

public class ReturJualDto
{
    public string ReturJualId { get; set; } = string.Empty;
    public DateTime TglJam { get; set; }
    public string UserId { get; set; } = string.Empty;

    public static ReturJualDto FromModel(ReturJualModel model)
    {
        return new ReturJualDto();
    }

    public ReturJualModel ToModel(IEnumerable<ReturJualItemDto> listItem)
    {
        return ReturJualModel.Default;
    }
}
