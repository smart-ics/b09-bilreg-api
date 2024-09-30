namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;

public class ClientCode
{
    void asdf()
    {
        var x = new Registo();
        //  this leads to error
        // x.Siswa = SiswaVo.Load("a", "b", 5);
        // x.Siswa.Name = "B";
    }
}
public class RegistoBuilder : Registo
{
    public RegistoBuilder Create(string regId)
    {
        RegId = regId;
        return this;
    }

    public RegistoBuilder WithSiswa(Person person, int grade)
    {
        Siswa = SiswaVo.Create(person, grade);
        Siswa = SiswaVo.Load("A", "B", 5);
        return this;
    }
}

public class Registo
{
    public string RegId { get; protected set; }
    public SiswaVo Siswa { get; protected set; }
}
public record Person(string Id, string Name);

public record SiswaVo(string Id, string Name, int grade)
{
    public static SiswaVo Create(Person person, int grade)
    {
        return new SiswaVo(person.Id, person.Name, grade);
    }

    public static SiswaVo Load(string id, string name, int grade)
    {
        return new SiswaVo(id, name, grade);
    }
};
