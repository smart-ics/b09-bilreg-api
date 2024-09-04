using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Domain.BillContext.RekapCetakSub.GrupRekapCetakAgg;
public  class GrupRekapCetakModel(string id, string name) : IGrupRekapCetakKey
{
    public string GrupRekapCetakId { get; set; } = id;
    public string GrupRekapCrtakName { get; set; } = name;
}
public interface IGrupRekapCetakKey
{
    string GrupRekapCetakId { get; }
}