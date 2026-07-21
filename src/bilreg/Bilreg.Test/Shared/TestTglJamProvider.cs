using Nuna.Lib.ValidationHelper;

namespace Bilreg.Test.Shared;

public sealed class TestTglJamProvider : ITglJamProvider
{
    public static readonly TestTglJamProvider Instance = new(new DateTime(2025, 5, 3, 10, 15, 30));

    public TestTglJamProvider(DateTime now) => Now = now;

    public DateTime Now { get; }
}
