using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SmfAgg;
using FluentValidation;
using FluentValidation.TestHelper;
using Xunit;

namespace Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;

public class PetugasMedisValidator : AbstractValidator<PetugasMedisModel>
{
    public PetugasMedisValidator()
    {
        RuleFor(x => x.PetugasMedisId).NotEmpty();
        RuleFor(x => x.PetugasMedisName).NotEmpty();
        RuleFor(x => x.SmfName)
            .NotEmpty().When(y => y.SmfId.Length > 0)
            .WithMessage("SMF ID invalid");
    }
}

public class PetugasMedisValidatorTest
{
    private readonly PetugasMedisValidator _validator;

    public PetugasMedisValidatorTest()
    {
        _validator = new PetugasMedisValidator();
    }

    [Fact]
    public void GivenEmptySmfName_AndSmfIdHasValue_ThenShouldHaveError()
    {
        // Arrange
        var model = new PetugasMedisModel("A", "B");
        model.Set(SmfModel.Create("C", ""));

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SmfName);
    }
}