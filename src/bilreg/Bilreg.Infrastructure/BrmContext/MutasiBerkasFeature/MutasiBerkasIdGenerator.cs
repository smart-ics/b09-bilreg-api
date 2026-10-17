using Bilreg.Application.BrmContext.MutasiBerkasFeature;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Infrastructure.BrmContext.MutasiBerkasFeature;

public class MutasiBerkasIdGenerator : IMutasiBerkasIdGenerator
{
    private readonly INunaCounterDecDal? _counterDal;
    private static int _fallbackCounter = 0;

    public MutasiBerkasIdGenerator(INunaCounterDecDal? counterDal = null)
    {
        _counterDal = counterDal;
    }

    public string Generate(MutasiBerkasStatusEnum status)
    {
        var prefix = status switch
        {
            MutasiBerkasStatusEnum.Dispatched => "MK",
            MutasiBerkasStatusEnum.Received => "MP",
            MutasiBerkasStatusEnum.Returned => "MM",
            MutasiBerkasStatusEnum.Missing => "MH",
            _ => throw new ArgumentOutOfRangeException(nameof(status), $"Status {status} is not supported for MutasiBerkas ID generation.")
        };

        long sequence = 0;
        if (_counterDal is not null)
        {
            try
            {
                var current = _counterDal.GetNewDecNumber(prefix);
                if (current <= 0)
                {
                    sequence = 1;
                    _counterDal.InsertNewDecNumber(prefix, sequence);
                }
                else
                {
                    sequence = current + 1;
                    _counterDal.UpdateNewDecNumber(prefix, sequence);
                }
            }
            catch
            {
                sequence = 0;
            }
        }

        if (sequence <= 0)
        {
            var count = Interlocked.Increment(ref _fallbackCounter);
            sequence = count % 100_000_000;
            if (sequence <= 0)
                sequence = 1;
        }

        var sequenceStr = (sequence % 100_000_000).ToString("D8");
        return $"{prefix}{sequenceStr}";
    }
}
