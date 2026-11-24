using System.Reflection;
using Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.Integration;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AktivasiBerkasFeature.Integration;

public class AktivasiBerkasContractTest
{
    [Fact]
    public void IAktivasiBerkasEmrGateway_ShouldDefineAllRequiredMethods()
    {
        var interfaceType = typeof(IAktivasiBerkasEmrGateway);

        // Verify interface exists and is an interface
        interfaceType.IsInterface.Should().BeTrue();

        // Verify ListData method
        var listDataMethod = interfaceType.GetMethod(nameof(IAktivasiBerkasEmrGateway.ListData));
        listDataMethod.Should().NotBeNull();
        listDataMethod!.ReturnType.Should().Be(typeof(Task<IEnumerable<AktivasiBerkasListItemDto>>));

        // Verify GetData method
        var getDataMethod = interfaceType.GetMethod(nameof(IAktivasiBerkasEmrGateway.GetData));
        getDataMethod.Should().NotBeNull();
        getDataMethod!.ReturnType.Should().Be(typeof(Task<AktivasiBerkasDetailDto>));

        // Verify Save method
        var saveMethod = interfaceType.GetMethod(nameof(IAktivasiBerkasEmrGateway.Save));
        saveMethod.Should().NotBeNull();
        saveMethod!.ReturnType.Should().Be(typeof(Task<AktivasiBerkasMutationResult>));

        // Verify Update method
        var updateMethod = interfaceType.GetMethod(nameof(IAktivasiBerkasEmrGateway.Update));
        updateMethod.Should().NotBeNull();
        updateMethod!.ReturnType.Should().Be(typeof(Task<AktivasiBerkasMutationResult>));

        // Verify FindRegister method
        var findRegisterMethod = interfaceType.GetMethod(nameof(IAktivasiBerkasEmrGateway.FindRegister));
        findRegisterMethod.Should().NotBeNull();
        findRegisterMethod!.ReturnType.Should().Be(typeof(Task<IEnumerable<AktivasiBerkasRegisterItemDto>>));

        // Verify ListReaktif method
        var listReaktifMethod = interfaceType.GetMethod(nameof(IAktivasiBerkasEmrGateway.ListReaktif));
        listReaktifMethod.Should().NotBeNull();
        listReaktifMethod!.ReturnType.Should().Be(typeof(Task<IEnumerable<AktivasiBerkasListReaktifDto>>));

        // Verify Deactivate method
        var deactivateMethod = interfaceType.GetMethod(nameof(IAktivasiBerkasEmrGateway.Deactivate));
        deactivateMethod.Should().NotBeNull();
        deactivateMethod!.ReturnType.Should().Be(typeof(Task<AktivasiBerkasMutationResult>));

        // Verify CleanUp method
        var cleanUpMethod = interfaceType.GetMethod(nameof(IAktivasiBerkasEmrGateway.CleanUp));
        cleanUpMethod.Should().NotBeNull();
        cleanUpMethod!.ReturnType.Should().Be(typeof(Task<AktivasiBerkasMutationResult>));
    }

    [Fact]
    public void AktivasiBerkasListItemDto_ShouldInitializePropertiesCorrectly()
    {
        var item = new AktivasiBerkasListItemDto
        {
            AktifasiBerkasId = "AB001",
            TglJam = "2026-11-09 10:00:00",
            ReqUserrId = "DOK01",
            ReqUserrName = "dr. Budi",
            AktifasiUserrId = "MED01",
            AktifasiUserrName = "Petugas RM",
            TglAktif = "2026-11-09",
            LayananId = "LAY01",
            LayananName = "Poli Penyakit Dalam",
            ListBerkas =
            [
                new AktivasiBerkasItemDetilDto
                {
                    AktifasiBerkasId = "AB001",
                    RegId = "REG123",
                    PasienId = "PX001",
                    PasienName = "John Doe"
                }
            ]
        };

        item.AktifasiBerkasId.Should().Be("AB001");
        item.ReqUserrId.Should().Be("DOK01");
        item.ReqUserrName.Should().Be("dr. Budi");
        item.AktifasiUserrId.Should().Be("MED01");
        item.AktifasiUserrName.Should().Be("Petugas RM");
        item.LayananId.Should().Be("LAY01");
        item.ListBerkas.Should().HaveCount(1);
        item.ListBerkas.First().RegId.Should().Be("REG123");
    }

    [Fact]
    public void AktivasiBerkasMutationResult_OkAndFail_ShouldSetPropertiesProperly()
    {
        var okResult = AktivasiBerkasMutationResult.Ok("AB100");
        okResult.Success.Should().BeTrue();
        okResult.AktifasiBerkasId.Should().Be("AB100");
        okResult.Message.Should().NotBeEmpty();

        var failResult = AktivasiBerkasMutationResult.Fail("Koneksi gagal");
        failResult.Success.Should().BeFalse();
        failResult.AktifasiBerkasId.Should().BeNull();
        failResult.Message.Should().Be("Koneksi gagal");
    }

    [Theory]
    [InlineData("3000-01-01", false)]
    [InlineData("01/01/3000", false)]
    [InlineData("", false)]
    [InlineData("2026-11-08", true)]
    [InlineData("2026-11-08 14:30:00", true)]
    public void AktivasiBerkasRegisterItemDto_IsVoid_ShouldReflectVoidStatusAccurately(string tglVoid, bool expectedIsVoid)
    {
        var registerItem = new AktivasiBerkasRegisterItemDto
        {
            RegId = "REG999",
            NamaPasien = "Jane Doe",
            TglVoid = tglVoid
        };

        registerItem.IsVoid.Should().Be(expectedIsVoid);
    }

    [Fact]
    public void AktivasiBerkasSaveDto_ShouldInitializeCorrectly()
    {
        var saveDto = new AktivasiBerkasSaveDto
        {
            TglJam = "2026-11-09 10:00:00",
            ReqUserrId = "DOK01",
            TglAktif = "2026-11-09",
            AktifasiUserrId = "MED01",
            LayananId = "LAY01",
            ListRegister = [new AktivasiBerkasRegisterRefDto("REG001"), new AktivasiBerkasRegisterRefDto("REG002")]
        };

        saveDto.ReqUserrId.Should().Be("DOK01");
        saveDto.AktifasiUserrId.Should().Be("MED01");
        saveDto.ListRegister.Should().HaveCount(2);
    }
}
