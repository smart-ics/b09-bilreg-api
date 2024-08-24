namespace Bilreg.Domain.AdmisiContext.JaminanSub.CaraBayarDkAgg;

public class CaraBayarDkModel: ICaraBayarDkKey
{
    // CONSTUCTOR
    public CaraBayarDkModel(string id, string name) => (CaraBayarDkId, CaraBayarDkName) = (id, name);
    
    // FACTORY METHOD
    public static CaraBayarDkModel Create(string id, string name) => new CaraBayarDkModel(id, name);
    
    // PROPERTIES
    public string CaraBayarDkId { get; private set; }
    
    public string CaraBayarDkName { get; private set; }
}

public interface ICaraBayarDkKey
{
    string CaraBayarDkId { get; }
}