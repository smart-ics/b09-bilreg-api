using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;

public record RegPasienVo
{
    private static readonly string[] ValidGender = ["L", "P", "W", "M", "F", "1", "0"];

    public string PasienId {get;} 
    public string PasienName {get;} 
    public string NoMedRec {get;} 
    public DateTime TglLahir {get;} 
    public string Gender {get;}  
    

    public RegPasienVo(PasienModel pasien)
    {
        PasienId = pasien.PasienId;
        PasienName = pasien.PasienName;
        NoMedRec = pasien.GetNoMedrec();
        TglLahir = pasien.TglLahir;
        Gender = pasien.Gender;

        Validate();
        //  GUARD
        Guard.IsNotNull(pasien);
        Guard.IsNotNullOrEmpty(pasien.PasienName);
    }

    public RegPasienVo(string pasienId, string pasienName, string noMedrec, DateTime tglLahie, string gender)
    {
        PasienId = pasienId;
        PasienName = pasienName;
        NoMedRec = noMedrec;
        TglLahir = tglLahie;
        Gender = gender;
        
        Validate();
    }

    private void Validate()
    {
        Guard.IsNotNullOrEmpty(PasienId);
        Guard.IsNotNullOrEmpty(PasienName);
        Guard.IsNotNullOrEmpty(NoMedRec);
        if(!ValidGender.Contains(Gender))
            throw new ArgumentException($"'{Gender}' is not a valid gender");
        if (TglLahir <= new DateTime(1900, 1, 1))
            throw new ArgumentException($"Tgl Lahir invalid");
    }
}

public class RegPasienVoTest
{
    private RegPasienVo _sut = null!;
    
    [Fact]
    public void T01_GivenValidCommand_ThenSuccess()
    {
        _sut = new RegPasienVo("A", "B", "C", new DateTime(1999, 1, 1), "L");
        _sut.PasienId.Should().Be("A");
        _sut.PasienName.Should().Be("B");
        _sut.NoMedRec.Should().Be("C");
        _sut.TglLahir.Should().Be(new DateTime(1999, 1, 1));
        _sut.Gender.Should().Be("L");
    }
    [Fact]
    public void T02_GivenTglLahir_KurangDari1900_ThenThrowEx()
    {
        var actual = () =>  _sut = new RegPasienVo("A", "B", "C", new DateTime(1899, 1, 1), "L");
        actual.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void T03_GivenInvalidGender_ThenThrowEx()
    {
        var actual = () => _sut = new RegPasienVo("A", "B", "C", new DateTime(1900, 1, 1), "X");
        actual.Should().Throw<ArgumentException>();
    }
}
