using System.Diagnostics;
using Bilreg.Application.Shared;
using Microsoft.Extensions.Options;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.Shared.Helpers;

public class TglJamProvider : ITglJamProvider, IBusinessDateStatus
{
    private readonly ISqlServerClock _sqlServerClock;
    private readonly BusinessDateOptions _options;
    private readonly object _initializeLock = new();
    private DateTime? _systemBaseTime;
    private DateTime _businessBaseTime;
    private Stopwatch? _stopwatch;

    public TglJamProvider(
        ISqlServerClock sqlServerClock,
        IOptions<BusinessDateOptions> options)
    {
        _sqlServerClock = sqlServerClock;
        _options = options.Value;
    }

    public DateTime Now => BusinessNow;
    public string Mode => _options.Mode.ToString();
    public DateOnly? FixedDate => _options.FixedDate;
    public bool IsSimulation => _options.Mode == BusinessDateMode.Fixed;
    public DateTime BusinessNow => GetCurrentTimes().BusinessNow;
    public DateTime SystemNow => GetCurrentTimes().SystemNow;

    private (DateTime BusinessNow, DateTime SystemNow) GetCurrentTimes()
    {
        EnsureInitialized();
        var elapsed = _stopwatch!.Elapsed;
        return (_businessBaseTime + elapsed, _systemBaseTime!.Value + elapsed);
    }

    private void EnsureInitialized()
    {
        if (_systemBaseTime.HasValue)
            return;

        lock (_initializeLock)
        {
            if (_systemBaseTime.HasValue)
                return;

            var systemBaseTime = _sqlServerClock.GetDate();
            _businessBaseTime = _options.Mode == BusinessDateMode.Fixed
                ? _options.FixedDate!.Value.ToDateTime(TimeOnly.FromDateTime(systemBaseTime))
                : systemBaseTime;
            _systemBaseTime = systemBaseTime;
            _stopwatch = Stopwatch.StartNew();
        }
    }
}
