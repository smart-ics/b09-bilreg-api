using Bilreg.Application.BillContext.RekapCetakSub.RekapCetakDkAgg;
using Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakDkAgg;
using Bilreg.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Infrastructure.BillContext.RekapCetakSub.RekapCetakDkAgg;
public class RekapCetakDkDal : IRekapCetakDkDal
{
    private DatabaseOptions _opt;

    public RekapCetakDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public RekapCetakDkModel GetData(IRekapCetakDkKey key)
    {
        const string sql = "";
    }

    public IEnumerable<RekapCetakDkModel> ListData()
    {
        throw new NotImplementedException();
    }
}
