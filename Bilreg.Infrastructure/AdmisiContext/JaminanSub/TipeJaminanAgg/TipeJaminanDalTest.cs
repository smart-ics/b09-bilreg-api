using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanSub.TipeJaminanAgg
{
    public class TipeJaminanDalTest
    {
        private readonly TipeJaminanDal _sut;

        public TipeJaminanDalTest()
        {
            _sut = new TipeJaminanDal(ConnStringHelper.GetTestEnv());
        }

        [Fact]
        public void InsertTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new TipeJaminanModel("A", "B");
            var jmn = new JaminanModel("C", "D");
            expected.Set(jmn);
            expected.Activate();

            _sut.Insert(expected);
        }

        [Fact]
        public void UpdateTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new TipeJaminanModel("A", "B");
            var jmn = new JaminanModel("C", "D");
            expected.Set(jmn);
            expected.Activate();

            _sut.Update(expected);
        }
        [Fact]
        public void DeleteTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new TipeJaminanModel("A", "B");
            var jmn = new JaminanModel("C", "D");
            expected.Set(jmn);
            expected.Activate();

            _sut.Delete(expected);
        }

        [Fact]
        public void GetTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new TipeJaminanModel("A", "B");
            var jmn = new JaminanModel("C", "");
            expected.Set(jmn);
            expected.Activate();
            _sut.Insert(expected);

            var actual = _sut.GetData(expected);
            actual.Should().BeEquivalentTo(expected);
        }
        [Fact]
        public void ListDataTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new TipeJaminanModel("A", "B");
            var jmn = new JaminanModel("C", "");
            expected.Set(jmn);
            expected.Activate();
            _sut.Insert(expected);

            var actual = _sut.ListData();
            actual.Should().ContainEquivalentOf(expected);
        }
        [Fact]
        public void ListDataByJaminanIdTest()
        {
            using var trans = TransHelper.NewScope();
            var expected = new TipeJaminanModel("A", "B");
            var jmn = new JaminanModel("C", "");
            expected.Set(jmn);
            expected.Activate();
            _sut.Insert(expected);

            var actual = _sut.ListData(jmn);
            actual.Should().ContainEquivalentOf(expected);
        }

    }
}
