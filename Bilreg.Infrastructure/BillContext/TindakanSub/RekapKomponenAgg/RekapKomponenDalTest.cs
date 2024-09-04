using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.TindakanSub.RekapKomponenAgg
{
    public class RekapKomponenDalTest
    {
        private readonly RekapKomponenDal _sut;

        public RekapKomponenDalTest() => _sut = new RekapKomponenDal(ConnStringHelper.GetTestEnv());

        [Fact]
        public void InsertTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new RekapKomponenModel("A", "B", decimal.MaxValue);
            var rk = new RekapKomponenModel("A", "B", decimal.One);
            expected.Set(rk);

            // Act
            _sut.Insert(expected);
        }

        [Fact]
        public void GetTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new RekapKomponenModel("A","B", decimal.MaxValue);
            var rk = new RekapKomponenModel("C", "",decimal.One);
            expected.Set(rk);
            _sut.Insert(expected);

            var actual = _sut.GetData(expected);
            actual.Should().BeEquivalentTo(expected);

        }

        [Fact]
        public void ListDataTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new RekapKomponenModel("A", "B", decimal.MaxValue);
            var rk = new RekapKomponenModel("C", "", decimal.One);
            expected.Set(rk);
            _sut.Insert(expected);

            var actual = _sut.ListData();
            actual.Should().ContainEquivalentOf(expected);

        }

        [Fact]
        public void UpdateTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new RekapKomponenModel("A", "B",decimal.One);
            var rk = new RekapKomponenModel("C", "D",decimal.MaxValue);
            expected.Set(rk);

            _sut.Update(expected);
        }
        [Fact]
        public void DeleteTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new RekapKomponenModel("A", "B",decimal.One);
            var rk = new RekapKomponenModel("C", "D", decimal.One);
            expected.Set(rk);

            _sut.Delete(expected);
        }

    }
}
