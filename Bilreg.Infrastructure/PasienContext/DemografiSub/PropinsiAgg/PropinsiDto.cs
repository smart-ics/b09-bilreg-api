using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.PasienContext.DemografiSub.PropinsiAgg;

public class PropinsiDto
{
    public string fs_kd_propinsi { get; set; }
    public string fs_nm_propinsi { get; set; }

    public PropinsiModel ToModel()
    {
        return new PropinsiModel(fs_kd_propinsi, fs_nm_propinsi);
    }
}

