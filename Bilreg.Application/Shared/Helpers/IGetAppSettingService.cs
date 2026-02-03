using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.Shared.Helpers;

public interface IGetAppSettingService : INunaService<AppSetting>
{
}


public record AppSetting(RemoteCetakSetting Registrasi);

public record RemoteCetakSetting(string RemoteCetakRegistrasi);

