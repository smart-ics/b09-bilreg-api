using Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakDkAgg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.BillContext.RekapCetakSub.RekapCetakDkAgg;

public class RekapCetakDkListQuery : IRekapCetakDkKey
{
    string IRekapCetakDkKey.RekapCetakDkId => throw new NotImplementedException();
}
