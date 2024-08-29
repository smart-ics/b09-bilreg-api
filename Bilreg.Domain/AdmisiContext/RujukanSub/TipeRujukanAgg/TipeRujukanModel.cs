using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SatTugasAgg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Domain.AdmisiContext.RujukanSub.TipeRujukanAgg;
public class TipeRujukanModel : ITipeRujukanKey
{
    // CONSTRUCTOR
    private TipeRujukanModel(string id, string name) 
    {
        TipeRujukanId = id;
        TipeRujukanName = name;
    }

    //FACTORY METHOD
    public static TipeRujukanModel Create(string id, string name)
    {
        return new TipeRujukanModel(id, name);
    }

    //PROPERTIES
    public string TipeRujukanId { get; private set; }
    public string TipeRujukanName { get; private set; }
}

public interface ITipeRujukanKey
{
    string TipeRujukanId { get; }
}