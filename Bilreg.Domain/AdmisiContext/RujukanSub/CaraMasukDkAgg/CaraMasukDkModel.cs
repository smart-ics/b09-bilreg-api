using Bilreg.Domain.PasienContext.StatusSosialSub.PekerjaanDkAgg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Domain.AdmisiContext.RujukanSub.CaraMasukDkAgg
{
    public class CaraMasukDkModel : ICaraMasukDkKey
    {
        // Constructor
        private CaraMasukDkModel(string id, string name)
        {
            CaraMasukDkId = id;
            CaraMasukDkName = name;
        }

        // Factory Method
        public static CaraMasukDkModel Create(string id, string name)
        {
            return new CaraMasukDkModel(id, name);
        }

        // Properties
        public string CaraMasukDkId { get; private set; }
        public string CaraMasukDkName { get; private set; }
    }

    public interface ICaraMasukDkKey
    {
        string CaraMasukDkId { get; }
    }

}
