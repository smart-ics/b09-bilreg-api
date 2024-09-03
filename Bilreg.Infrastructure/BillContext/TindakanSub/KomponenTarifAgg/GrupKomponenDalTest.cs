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
            var gk = new GrupKomponenModel("A", "B");
            expected.Set(gk);
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
    }
}
