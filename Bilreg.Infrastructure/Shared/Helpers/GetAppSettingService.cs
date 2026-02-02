using Bilreg.Application.Shared.Helpers;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.Shared.Helpers;

public class GetAppSettingService : IGetAppSettingService
{
    private readonly RemoteCetakOptions _remoteCetakOpt;

    public GetAppSettingService(IOptions<RemoteCetakOptions> remoteCetakOpt)
    {
        _remoteCetakOpt = remoteCetakOpt.Value;
    }

    public AppSetting Execute()
    {
        var rmtCetakSeting = new RemoteCetakSetting(_remoteCetakOpt.RemoteCetakRegistrasi);
        var appSetting = new AppSetting(rmtCetakSeting);

        return appSetting;
    }
}