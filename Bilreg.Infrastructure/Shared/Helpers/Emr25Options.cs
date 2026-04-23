using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.Shared.Helpers;

public class EmrOptions
{
    public const string SECTION_NAME = "Emr";
    public string BaseApiUrl { get; set; } = string.Empty;

    public static IOptions<EmrOptions> GetTestEnv()
    {
        var result = Options.Create<EmrOptions>(new EmrOptions
        {
            BaseApiUrl = "http://dev.smart-ics.com:8089/emr25api",

        });
        return result;
    }
}
