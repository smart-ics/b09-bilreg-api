using Farinv.Application.BrgContext.StandardFeature;
using Farinv.Domain.BrgContext.StandardFeature;
using Nuna.Lib.PatternHelper;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public class KfaRepo : IKfaRepo
{
    private readonly IKfaDal _kfaDal;
    private readonly IKfaReplacementDal _kfaReplacementDal;
    private readonly IKfaIngredientDal _kfaIngredientDal;
    private readonly IKfaDosageUsageDal _kfaDosageUsageDal;
    private readonly IKfaPackagingDal _kfaPackagingDal;

    public KfaRepo(
        IKfaDal kfaDal,
        IKfaReplacementDal kfaReplacementDal,
        IKfaIngredientDal kfaIngredientDal,
        IKfaDosageUsageDal kfaDosageUsageDal,
        IKfaPackagingDal kfaPackagingDal)
    {
        _kfaDal = kfaDal;
        _kfaReplacementDal = kfaReplacementDal;
        _kfaIngredientDal = kfaIngredientDal;
        _kfaDosageUsageDal = kfaDosageUsageDal;
        _kfaPackagingDal = kfaPackagingDal;
    }

    public MayBe<KfaType> LoadEntity(IKfaKey key)
    {
        var kfaDto = _kfaDal.GetData(key);
        if (kfaDto is null)
            return MayBe<KfaType>.None;

        var replacementDto = _kfaReplacementDal.GetData(key);
        var replacement = replacementDto?.ToModel() ?? KfaReplacementType.Default;

        var ingredients = _kfaIngredientDal.ListData(key).Select(dto => dto.ToModel()).ToList();
        var dosageUsage = _kfaDosageUsageDal.ListData(key).Select(dto => dto.ToModel()).ToList();
        var packaging = _kfaPackagingDal.ListData(key).Select(dto => dto.ToModel()).ToList();

        var model = kfaDto.ToModel(
            replacement,
            ingredients,
            dosageUsage,
            packaging
        );

        return MayBe.From(model);
    }

    public IEnumerable<KfaType> ListData(string filter)
    {
        var listKfaDto = _kfaDal.ListData(filter)?.ToList() ?? [];
        var result = new List<KfaType>();

        return listKfaDto.Select(dto =>
            dto.ToModel(KfaReplacementType.Default, [], [], []));
    }
}