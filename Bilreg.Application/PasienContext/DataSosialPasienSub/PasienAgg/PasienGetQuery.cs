using System.Collections;
using System.Text.Json;
using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Application.Helpers;
using Bilreg.Application.PasienContext.ParamContext.ParamSistemAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public record PasienGetQuery(string PasienId): IRequest<PasienGetResponse>, IPasienKey;

public record PasienLogGetResponse(
    string LogDate,
    string Activity,
    string UserId,
    IEnumerable<PasienChangeLogResponse> ChangeLog
);

public record PasienChangeLogResponse(
    string PropertyName,
    string OldValue,
    string NewValue
);

public record PasienGetResponse(
    string PasienId,
    string NomorMedrec,
    string PasienName,
    string NickName,
    string TempatLahir,
    string TglLahir,
    string Gender,
    string TglMedrec,
    string IbuKandung,
    string GolDarah,
    string StatusNikahId,
    string StatusNikahName,
    string AgamaId,
    string AgamaName,
    string SukuId,
    string SukuName,
    string PekerjaanDkId,
    string PekerjaanDkName,
    string PendidikanDkId,
    string PendidikanDkName,
    string Alamat,
    string Alamat2,
    string Alamat3,
    string Kota,
    string KodePos,
    string KelurahanId,
    string KelurahanName,
    string KecamatanName,
    string KabupatenName,
    string PropinsiName,
    string JenisId,
    string NomorId,
    string NomorKk,
    string Email,
    string NoTelp,
    string NoHp,
    string KeluargaName,
    string KeluargaRelasi,
    string KeluargaNoTelp,
    string KeluargaAlamat1,
    string KeluargaAlamat2,
    string KeluargaKota,
    string KeluargaKodePos,
    IEnumerable<PasienLogGetResponse> ListLog
);

public class PasienGetHandler: IRequestHandler<PasienGetQuery, PasienGetResponse>
{
    private readonly IParamSistemDal _paramSistemDal;
    private readonly IFactoryLoad<PasienModel, IPasienKey> _factory;
    private const string KODE_RS_PARAM_KEY = "RS__XXXXXX_KODE";

    public PasienGetHandler(IParamSistemDal paramSistemDal, IFactoryLoad<PasienModel, IPasienKey> factory)
    {
        _paramSistemDal = paramSistemDal;
        _factory = factory;
    }

    public Task<PasienGetResponse> Handle(PasienGetQuery request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsTrue(request.PasienId.IsValidA(x => x.Length is 6 or 8 or 15));
        
        // QUERY
        var pasienGetQuery = GetPasienId(request.PasienId);
        var pasien = _factory.Load(pasienGetQuery);
        pasien.RemoveNull();
        
        // RESPONSE
        var response = BuildPasienResponse(pasien);
        return Task.FromResult(response);
    }

    private PasienGetQuery GetPasienId(string pasienId)
    {
        var kodeRsEncrypted = _paramSistemDal.GetData(KODE_RS_PARAM_KEY)?.Value ?? string.Empty;
        var kodeRs = X1EncryptionHelper.DecodingNeo(kodeRsEncrypted);
        
        if (pasienId.Length is 6)
            return new PasienGetQuery($"{kodeRs}00{pasienId}");
        else if (pasienId.Length is 8)
            return new PasienGetQuery($"{kodeRs}{pasienId}");
        else
            return new PasienGetQuery(pasienId);
    }

    private string GetNomorMedrec(string pasienId)
    {
        var breakPasienId = pasienId[7..]
            .Chunk(2)
            .Select(x => new string(x))
            .ToList();
        return breakPasienId.Join("-");
    }

    private PasienGetResponse BuildPasienResponse(PasienModel pasien)
    {
        var listLog = pasien.ListLog.Select(x =>
        {
            var changeLogs = JsonSerializer.Deserialize<List<PasienChangeLogResponse>>(x.ChangeLog);
            return new PasienLogGetResponse(x.LogDate, x.Activity, x.UserId, changeLogs ?? []);
        });
        
        return new PasienGetResponse(
            pasien.PasienId,
            GetNomorMedrec(pasien.PasienId),
            pasien.PasienName,
            pasien.NickName,
            pasien.TempatLahir,
            pasien.TglLahir.ToString(DateFormatEnum.YMD),
            pasien.Gender,
            pasien.TglMedrec.ToString(DateFormatEnum.YMD),
            pasien.IbuKandung,
            pasien.GolDarah,
            pasien.StatusNikahId,
            pasien.StatusNikahName,
            pasien.AgamaId,
            pasien.AgamaName,
            pasien.SukuId,
            pasien.SukuName,
            pasien.PekerjaanDkId,
            pasien.PekerjaanDkName,
            pasien.PendidikanDkId,
            pasien.PendidikanDkName,
            pasien.Alamat,
            pasien.Alamat2,
            pasien.Alamat3,
            pasien.Kota,
            pasien.KodePos,
            pasien.KelurahanId,
            pasien.KelurahanName,
            pasien.KecamatanName,
            pasien.KabupatenName,
            pasien.PropinsiName,
            pasien.JenisId,
            pasien.NomorId,
            pasien.NomorKk,
            pasien.Email,
            pasien.NoTelp,
            pasien.NoHp,
            pasien.KeluargaName,
            pasien.KeluargaRelasi,
            pasien.KeluargaNoTelp,
            pasien.KeluargaAlamat1,
            pasien.KeluargaAlamat2,
            pasien.KeluargaKota,
            pasien.KeluargaKodePos,
            listLog
        );
    }
}

public class PasienGetQueryHandlerTest
{
    private readonly Mock<IParamSistemDal> _paramSistemDal;
    private readonly Mock<IFactoryLoad<PasienModel, IPasienKey>> _factory;
    private readonly PasienGetHandler _sut;

    public PasienGetQueryHandlerTest()
    {
        _paramSistemDal = new Mock<IParamSistemDal>();
        _factory = new Mock<IFactoryLoad<PasienModel, IPasienKey>>();
        _sut = new PasienGetHandler(_paramSistemDal.Object, _factory.Object);
    }

    [Fact]
    public async Task GivenInvalidLengthPasienId_ThenThrowArgumentException()
    {
        var request = new PasienGetQuery("AA");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenInvalidPasienId_ThenThrowKeyNotFoundException()
    {
        var request = new PasienGetQuery("1234567000000AA");
        var expected = new PasienModel("1234567000000AA", "AA");
        _factory.Setup(x => x.Load(It.IsAny<IPasienKey>()))
            .Throws<KeyNotFoundException>();
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidPasienId_ThenReturnExpected()
    {
        var request = new PasienGetQuery("1234567000000AA");
        var expected = new PasienModel("1234567000000AA", "AA");
        _factory.Setup(x => x.Load(It.IsAny<IPasienKey>()))
            .Returns(expected);
        var actual = await _sut.Handle(request, CancellationToken.None);
        actual.Should().BeEquivalentTo(expected);
    }
}