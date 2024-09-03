using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SatTugasAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SmfAgg;
using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;

public class PetugasMedisModelTest
{
    #region SMF
    [Fact]
    public void GivenValidSmf_WhenSet_ThenSmfIsSet()
    {
        var petugasMedis = new PetugasMedisModel("A", "B");
        var smf = SmfModel.Create("SMF-001", "SMF 1");
        petugasMedis.Set(smf);
        petugasMedis.SmfId.Should().Be(smf.SmfId);
        petugasMedis.SmfName.Should().Be(smf.SmfName);
    }
    [Fact]
    public void GivenNullSmf_WhenSet_ThenThrowEx()
    {
        var petugasMedis = new PetugasMedisModel("A", "B");
        var actual = () => petugasMedis.Set(null!);
        actual.Should().Throw<ArgumentNullException>();
    }
    [Fact]
    public void GivenEmptySmfId_WhenSet_ThenThrowEx()
    {
        var petugasMedis = new PetugasMedisModel("A", "B");
        var smf = SmfModel.Create("", "B");
        var actual = () => petugasMedis.Set(smf);
        actual.Should().Throw<ArgumentException>();
    }
    [Fact]
    public void GivenEmptySmfName_WhenSet_ThenThrowEx()
    {
        var petugasMedis = new PetugasMedisModel("A", "B");
        var smf = SmfModel.Create("C", "");
        var actual = () => petugasMedis.Set(smf);
        actual.Should().Throw<ArgumentException>();
    }
    #endregion

    #region SATUAN TUGAS
    [Fact]
    public void GivenValidSatTugas_WhenAdd_ThenSatTugasIsAdded()
    {
        var petugasMedis = new PetugasMedisModel("A", "B");
        var satTugas = SatuanTugasModel.Create("C", "D", false);
        petugasMedis.Add(satTugas);
        petugasMedis.ListSatTugas.Should().ContainEquivalentOf(satTugas);
    }
    [Fact]
    public void GivenNullSatTugas_WhenAdd_ThenThrowEx()
    {
        var petugasMedis = new PetugasMedisModel("A", "B");
        var actual = () => petugasMedis.Add((SatuanTugasModel)null!);
        actual.Should().Throw<ArgumentNullException>();
    }
    [Fact]
    public void GivenEmptySatTugasId_WhenAdd_ThenThrowEx()
    {
        var petugasMedis = new PetugasMedisModel("A", "B");
        var satTugas = SatuanTugasModel.Create("", "D", false);
        var actual = () => petugasMedis.Add(satTugas);
        actual.Should().Throw<ArgumentException>();
    }
    [Fact]
    public void GivenEmptySatTugasName_WhenAdd_ThenThrowEx()
    {
        var petugasMedis = new PetugasMedisModel("A", "B");
        var satTugas = SatuanTugasModel.Create("C", "", false);
        var actual = () => petugasMedis.Add(satTugas);
        actual.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenValidSatTugas_WhenRemove_ThenSatTugasIsRemoved()
    {
        var petugasMedis = new PetugasMedisModel("A", "B");
        var satTugas = SatuanTugasModel.Create("C", "D", false);
        petugasMedis.Add(satTugas);
        petugasMedis.Remove(x => x.SatTugasId == satTugas.SatuanTugasId);
        petugasMedis.ListSatTugas.Should().NotContainEquivalentOf(satTugas);
    }

    [Fact]
    public void GivenDuplicatedSatTugas_WhenAdd_ThenThrowEx()
    {
        var petugasMedis = new PetugasMedisModel("A", "B");
        var satTugas1 = SatuanTugasModel.Create("C", "D", false);
        petugasMedis.Add(satTugas1);
        var satTugas2 = SatuanTugasModel.Create("C", "D", false);
        var actual = () => petugasMedis.Add(satTugas2);
        actual.Should().Throw<ArgumentException>();
    }
    [Fact]
    public void GivenValidSatTugas_WhenSetAsUtama_ThenSatTugasIsSetAsUtama()
    {
        var petugasMedis = new PetugasMedisModel("A", "B");
        var satTugas = SatuanTugasModel.Create("C", "D", false);
        petugasMedis.Add(satTugas);
        petugasMedis.SetAsSatTugasUtama(x => x.SatTugasId == "C");
        petugasMedis.ListSatTugas.First(x => x.SatTugasId == "C").IsUtama.Should().BeTrue();
    }
    [Fact]
    public void GivenValidSatTugas_WhenSatAsUtama_ThenTheOthersAreNotUtama()
    {
        var petugasMedis = new PetugasMedisModel("A", "B");
        var satTugas1 = SatuanTugasModel.Create("C", "D", false);
        petugasMedis.Add(satTugas1);
        var satTugas2 = SatuanTugasModel.Create("E", "F", true);
        petugasMedis.Add(satTugas2);
        petugasMedis.SetAsSatTugasUtama(x => x.SatTugasId == "E");
        petugasMedis.SetAsSatTugasUtama(x => x.SatTugasId == "C");
        petugasMedis.ListSatTugas.First(x => x.SatTugasId == "C").IsUtama.Should().BeTrue();
        petugasMedis.ListSatTugas.First(x => x.SatTugasId == "E").IsUtama.Should().BeFalse();
    }
    #endregion

    #region LAYANAN
    [Fact]
    public void GivenValidLayanan_WhenAdd_ThenLayananIsAdded()
    {
        var petugasMedis = new PetugasMedisModel("A", "B");
        var layanan = new LayananModel("C", "D");
        petugasMedis.Add(layanan);
        petugasMedis.ListLayanan.Should().ContainEquivalentOf(layanan);
    }
    [Fact]
    public void GivenNullLayanan_WhenAdd_ThenThrowEx()
    {
        var petugasMedis = new PetugasMedisModel("A", "B");
        var actual = () => petugasMedis.Add((LayananModel)null!);
        actual.Should().Throw<ArgumentNullException>();
    }
    [Fact]
    public void GivenEmptyLayananId_WhenAdd_ThenThrowEx()
    {
        var petugasMedis = new PetugasMedisModel("A", "B");
        var layanan = new LayananModel("", "B");
        var actual = () => petugasMedis.Add(layanan);
        actual.Should().Throw<ArgumentException>();
    }
    [Fact]
    public void GivenEmptyLayananName_WhenAdd_ThenThrowEx()
    {
        var petugasMedis = new PetugasMedisModel("A", "B");
        var layanan = new LayananModel("C", "");
        var actual = () => petugasMedis.Add(layanan);
        actual.Should().Throw<ArgumentException>();
    }
    [Fact]
    public void GivenValidLayanan_WhenRemove_ThenLayananIsRemoved()
    {
        var petugasMedis = new PetugasMedisModel("A", "B");
        var layanan = new LayananModel("C", "D");
        petugasMedis.Add(layanan);
        petugasMedis.Remove(x => x.LayananId == layanan.LayananId);
        petugasMedis.ListLayanan.Should().NotContainEquivalentOf(layanan);
    }
    [Fact]
    public void GivenDuplicatedLayanan_WhenAdd_ThenThrowEx()
    {
        var petugasMedis = new PetugasMedisModel("A", "B");
        var layanan1 = new LayananModel("C", "D");
        var layanan2 = new LayananModel("C", "D");
        petugasMedis.Add(layanan1);
        var actual = () => petugasMedis.Add(layanan2);
        actual.Should().Throw<ArgumentException>();
    }
    #endregion
}