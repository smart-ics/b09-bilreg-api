using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Domain.BillContext.RekapCetakSub.GrupRekapCetakAgg;
public  class GrupRekapCetakModel(string id, string name) : IGrupRekapCetakKey
{
    public string GrupRekapCetakId { get; protected set; } = id;
    public string GrupRekapCetakName { get; protected set; } = name;
}
public interface IGrupRekapCetakKey
{
    string GrupRekapCetakId { get; }
}