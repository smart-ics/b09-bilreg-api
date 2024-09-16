namespace Bilreg.Domain.ParamContext;

public class ParamSistemModel(string id, string name, string value)
    :IParamSistemKey
{
    public string ParamSistemId { get; protected set; } = id;
    public string ParamSistemName { get; protected set; } = name;
    public string Value { get; protected set; } = value;
}

public interface IParamSistemKey
{
    string ParamSistemId { get; }
}