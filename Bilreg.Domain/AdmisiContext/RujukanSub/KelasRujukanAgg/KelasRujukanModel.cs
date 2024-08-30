using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Domain.AdmisiContext.RujukanSub.KelasRujukanAgg;

public class KelasRujukanModel : IKelasRujukanKey
{
    // CONSTRUCTOR
    private KelasRujukanModel(string id, string name, decimal nilai) 
    {
        KelasRujukanId = id;
        KelasRujukanName = name;
        Nilai = nilai; 
    }

    // FACTORY METHOD
    public static KelasRujukanModel Create(string id, string name, decimal nilai)
    {
        return new KelasRujukanModel(id, name, nilai);
    }

    // PROPERTIES
    public string KelasRujukanId { get; private set; }
    public string KelasRujukanName { get; private set; }
    public decimal Nilai { get; private set; }
}

public interface IKelasRujukanKey
{
    string KelasRujukanId { get; }
}