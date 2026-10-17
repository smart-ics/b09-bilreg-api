using Bilreg.Domain.BrmContext.MutasiBerkasFeature;

namespace Bilreg.Application.BrmContext.MutasiBerkasFeature;

public interface IMutasiBerkasIdGenerator
{
    string Generate(MutasiBerkasStatusEnum status);
}
