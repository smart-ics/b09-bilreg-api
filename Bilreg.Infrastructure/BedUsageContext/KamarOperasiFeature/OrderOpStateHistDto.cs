using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature
{
    public record OrderOpStateHistDto(string OrderOpId, int NoUrut, int OrderOpState, DateTime StateTimestamp)
    {
        public static OrderOpStateHistDto FromModel(string orderOpId, OrderOpStateHistType model)
        {
            return new OrderOpStateHistDto(
                orderOpId,
                model.NoUrut,
                (int)model.OrderOpState,
                model.StateTimestamp
            );
        }

        public OrderOpStateHistType ToModel()
        {
            return new OrderOpStateHistType(
                NoUrut,
                (OrderOpStateEnum)OrderOpState,
                StateTimestamp
            );
        }
    }
}
