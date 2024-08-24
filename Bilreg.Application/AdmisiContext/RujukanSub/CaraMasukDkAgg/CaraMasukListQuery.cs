using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmisiContext.RujukanSub.CaraMasukDkAgg
{
    public record CaraMasukDkListQuery() : IRequest<IEnumerable<CaraMasukDkListResponse>>;
    public record CaraMasukDkListResponse(string CaraMasukDkId, string CaraMasukDkName);
    public class CaraMasukDkListHandler 
    {
        public Task<object> Handle(CaraMasukDkListQuery request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
