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
        // CONSTRUCTOR
        private CaraMasukDkModel(String id , String name) 
        {
            CaraMasukDkId = id;
            CaraMasukDkName = name;
        }

        // FACTORY METHODS      
        public static CaraMasukDkModel Create(String id, String name) 
        {
            return new CaraMasukDkModel(id, name);
        }

        // PROPERTIES
        public string CaraMasukDkId { get; private set; }
        public string CaraMasukDkName { get; private set; }
    }

    public interface ICaraMasukDkKey
    {
        String CaraMasukDkId { get;}
    }
}
