using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.TindakanSub.KomponenTarifAgg
{
    public class GrupKomponenDalTest
    {
        private readonly GrupKomponenDal _sut;
        public GrupKomponenDalTest()
        {
            _sut = new GrupKomponenDal(ConnStringHelper.GetTestEnv());
        }

        [Fact]
        public void InsertTest() {
            using var trans = TransHelper.NewScope();
            var expected = new GrupKomponenModel("A", "B");
            _sut.Insert(expected);
        }

        [Fact]
        public void GetTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new GrupKomponenModel("A", "B");
            _sut.Insert(expected);


            var actual = _sut.GetData(expected);
            actual.Should().BeEquivalentTo(expected);
        }
        [Fact]
        public void ListTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new GrupKomponenModel("A", "B");
            _sut.Insert(expected);

            var actual = _sut.ListData();
            actual.Should().ContainEquivalentOf(expected);

        }

        [Fact]
        public void DeleteTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new GrupKomponenModel("A","B");
            _sut.Delete(expected);
        }

        [Fact]
        public void UpdateTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new GrupKomponenModel("A", "B");
            _sut.Update(expected);
        }
    }
}
