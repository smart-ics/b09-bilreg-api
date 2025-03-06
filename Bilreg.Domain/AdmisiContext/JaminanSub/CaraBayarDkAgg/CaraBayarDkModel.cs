namespace Bilreg.Domain.AdmisiContext.JaminanSub.CaraBayarDkAgg;

public class CaraBayarDkModel: ICaraBayarDkKey
{
    public CaraBayarDkModel(string id, string name)
    {
        if (id == string.Empty ^ name == string.Empty)
            throw new ArgumentException("Invalid CaraBayarDk");
        CaraBayarDkId = id;
        CaraBayarDkName = name;
    }

    public static CaraBayarDkModel Default => new(string.Empty, string.Empty);

    public CaraBayarDkModel()
    {
    }
    public string CaraBayarDkId { get; private set; }
    public string CaraBayarDkName { get; private set; }
}