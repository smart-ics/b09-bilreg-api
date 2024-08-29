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
