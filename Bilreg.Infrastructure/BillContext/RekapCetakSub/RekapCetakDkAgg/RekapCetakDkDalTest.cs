using Bilreg.Domain.AdmisiContext.RujukanSub.TipeRujukanAgg;
using Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakDkAgg;
using Bilreg.Infrastructure.AdmisiContext.RujukanSub.TipeRujukanAgg;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.RekapCetakSub.RekapCetakDkAgg;
public class RekapCetakDkDalTest
{
    private readonly RekapCetakDkDal _sut;

    public RekapCetakDkDalTest()
    {
        _sut = new RekapCetakDkDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void GetTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new RekapCetakDkModel("A","B");
        expected.SetId("A");
        _sut.GetData(expected);

        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }
}
    