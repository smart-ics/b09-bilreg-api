namespace Bilreg.Domain.AdmisiContext.RujukanSub.CaraMasukDkAgg;

public class CaraMasukDkModel : ICaraMasukDkKey
{
    public  CaraMasukDkModel(string id, string name)
    {
        if (id == string.Empty ^ name == string.Empty)
            throw new ArgumentException("CaraMasukDK invalid");

        CaraMasukDkId = id;
        CaraMasukDkName = name;
    }

    public static CaraMasukDkModel Default => new CaraMasukDkModel(string.Empty, string.Empty);

    public CaraMasukDkModel()
    {
    }
    
    public string CaraMasukDkId { get; private set; }
    public string CaraMasukDkName { get; private set; }
}