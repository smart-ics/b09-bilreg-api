namespace Bilreg.Infrastructure.Shared.Helpers;

public class JSend<T>
{
    public string Status { get; set; }
    public string Code { get; set; }
    public T Data { get; set; }
}
