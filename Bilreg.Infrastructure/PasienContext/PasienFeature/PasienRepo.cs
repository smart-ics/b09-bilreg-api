using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Moq;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Infrastructure.PasienContext.PasienFeature;

public class PasienRepo : IPasienRepo
{
    private readonly IPasienDal _pasienDal;
    private readonly IPasien2Dal _pasien2Dal;
    private readonly IParamSistemDal _paramSistemDal;
    private readonly INunaCounterBL _counter;
    private readonly IGenderDal _genderDal;
    
    private const string KODE_RS_PARAM_KEY = "RS__XXXXXX_KODE";
    private const string NO_MR_PARAM_KEY = "NOMR";
    
    public PasienRepo(IPasienDal pasienDal, 
        IPasien2Dal pasien2Dal, 
        IParamSistemDal paramSistemDal, 
        INunaCounterBL counter, IGenderDal genderDal)
    {
        _pasienDal = pasienDal;
        _pasien2Dal = pasien2Dal;
        _paramSistemDal = paramSistemDal;
        _counter = counter;
        _genderDal = genderDal;
    }

    public Result<IPasienKey> SaveChanges(PasienModel model)
    {
        if (model.PasienId == "[NEW]")
            model.SetPasienId(NewPasienId());
        
        using var trans = TransHelper.NewScope();
        
        var pasienDb = _pasienDal.GetData(PasienModel.Key(model.PasienId));
        if (pasienDb.HasValue)
            _pasienDal.Update(new PasienDto(model));
        else
            _pasienDal.Insert(new PasienDto(model));
        
        var pasien2Db = _pasien2Dal.GetData(PasienModel.Key(model.PasienId));
        if (pasien2Db.HasValue)
            _pasien2Dal.Update(Pasien2Dto.Create(model));
        else
            _pasien2Dal.Insert(Pasien2Dto.Create(model));
        
        trans.Complete();
        
        return Result<IPasienKey>.Success(PasienModel.Key(model.PasienId));
    }

    public MayBe<PasienModel> LoadEntity(IPasienKey key)
    {
        var pasienDtoMaybe = _pasienDal.GetData(key);
        if (!pasienDtoMaybe.HasValue)
            return MayBe<PasienModel>.None;
        var pasien = pasienDtoMaybe.Value.ToModel(_genderDal);

        var pasien2 = _pasien2Dal.GetData(key);
        var alamatKtp = AlamatType.Default;
        if (pasien2.HasValue)
            alamatKtp = pasien2.Value.GetAlamatKtp();
        
        pasien.SetAdministrativeInfo(pasien.AlamatDomisili, alamatKtp,
            pasien.Kelurahan, pasien.Identitas, pasien.KartuKeluarga, pasien.ListContact,
            pasien.PasienKeluarga);
        
        return MayBe.From(pasien);
    }

    public Result DeleteEntity(IPasienKey key)
    {
        try
        {
            using var trans = TransHelper.NewScope();
        
            _pasienDal.Delete(PasienModel.Key(key.PasienId));
            _pasien2Dal.Delete(PasienModel.Key(key.PasienId));
        
            trans.Complete();
        }
        catch (Exception e)
        {
            return Result.Failure(e.Message);
        }
        
        return Result.Success();
    }


    public MayBe<IEnumerable<PasienReff>> ListData(SearchKeyword filter)
    {
        var parsedKeyword = filter.Parse();
        var listDtoMayBe = _pasienDal.ListData(parsedKeyword.TglLahir);
        if (!listDtoMayBe.HasValue)
            return MayBe<IEnumerable<PasienReff>>.None;
        
        var searchName = parsedKeyword.Name
            .NormalizeToEyd()
            .ToLower();
        var list = listDtoMayBe.Value.ToList();
        var listIdName = list
            .Select(x => new NormalizedName(x.fs_mr, x.fs_nm_pasien.NormalizeToEyd().ToLower()))
            .ToList();
        
        var listCompareName = listIdName
            .Where(x => x.PasienName.Similiarity(searchName) > 0.8) 
            .ToList();
        var listWordName = searchName.Split(' ');
        var listSimilarWord = listIdName
            .Where(x => listWordName.All(y => x.PasienName.Contains(y)))
            .ToList();
        listCompareName.AddRange(listSimilarWord);
        
        if (listCompareName.Count == 0)
            return MayBe<IEnumerable<PasienReff>>.None;

        var result = list
            .Where(x => listCompareName.Any(y => y.PasienId == x.fs_mr))
            .Distinct()
            .Select(x => x.ToModel(_genderDal).ToReff());        
        
        return MayBe.From(result);
    }
    
    private string NewPasienId()
    {
        var kodeRsEncrypted = _paramSistemDal.GetData(KODE_RS_PARAM_KEY)?.Value ?? string.Empty;
        var kodeRs = X1EncryptionHelper.DecodingNeo(kodeRsEncrypted);
        var newId = _counter.GenerateDec(NO_MR_PARAM_KEY, kodeRs, 15, string.Empty);
        return newId;
    }

}

public record NormalizedName(string PasienId, string PasienName);
public class PasienRepoTest
{
    private readonly IPasienRepo _repo;
    private readonly Mock<IPasienDal> _pasienDal;
    private readonly Mock<IPasien2Dal> _pasien2Dal;
    private readonly Mock<IParamSistemDal> _paramSistemDal;
    private readonly Mock<INunaCounterBL> _counter;
    private readonly Mock<IGenderDal> _genderDal;

    public PasienRepoTest()
    {
        _pasienDal = new Mock<IPasienDal>();
        _pasien2Dal = new Mock<IPasien2Dal>();
        _paramSistemDal = new Mock<IParamSistemDal>();
        _counter = new Mock<INunaCounterBL>();
        _genderDal = new Mock<IGenderDal>();
        _repo = new PasienRepo(_pasienDal.Object, 
            _pasien2Dal.Object, 
            _paramSistemDal.Object, 
            _counter.Object, 
            _genderDal.Object);
    }

    [Fact]
    public void UT1_GivenNameYudis_WhenListDataYudhis_ThenFound()
    {
        var gender = MayBe<GenderType>.Some(GenderType.Default);
        _genderDal.Setup(x => x.GetData(It.IsAny<string>())).Returns(gender);
        var pasien = new PasienDto { fd_tgl_lahir = "2000-01-01", fs_nm_pasien = "YUDIS" };
        pasien.RemoveNull();
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>()))
            .Returns(MayBe<IEnumerable<PasienDto>>.Some(new List<PasienDto> { pasien }));

        var actual = _repo.ListData(new SearchKeyword("yudhis 2000-01-01"));
        
        actual.Value.Should().HaveCount(1);
    }
    
    [Fact]
    public void UT2_GivenNameYudis_WhenListDataJoedhis_ThenFound()
    {
        var gender = MayBe<GenderType>.Some(GenderType.Default);
        _genderDal.Setup(x => x.GetData(It.IsAny<string>())).Returns(gender);
        var pasien = new PasienDto { fd_tgl_lahir = "2000-01-01", fs_nm_pasien = "YUDIS" };
        pasien.RemoveNull();
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>()))
            .Returns(MayBe<IEnumerable<PasienDto>>.Some(new List<PasienDto> { pasien }));

        var actual = _repo.ListData(new SearchKeyword("joedis 2000-01-01"));
        
        actual.Value.Should().HaveCount(1);
    }
    
    [Fact]
    public void UT3_GivenNameYudis_WhenListDataBudi_ThenNotFound()
    {
        var gender = MayBe<GenderType>.Some(GenderType.Default);
        _genderDal.Setup(x => x.GetData(It.IsAny<string>())).Returns(gender);
        var pasien = new PasienDto { fd_tgl_lahir = "2000-01-01", fs_nm_pasien = "YUDIS" };
        pasien.RemoveNull();
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>()))
            .Returns(MayBe<IEnumerable<PasienDto>>.Some(new List<PasienDto> { pasien }));

        var actual = _repo.ListData(new SearchKeyword("budi 2000-01-01"));
        
        actual.HasValue.Should().BeFalse();
    }    
    
    [Fact]
    public void UT4_GivenNameAgusBudi_WhenListDataBudiAgus_ThenFound()
    {
        var gender = MayBe<GenderType>.Some(GenderType.Default);
        _genderDal.Setup(x => x.GetData(It.IsAny<string>())).Returns(gender);
        var pasien = new PasienDto { fd_tgl_lahir = "2000-01-01", fs_nm_pasien = "AGUS BUDI" };
        pasien.RemoveNull();
        _pasienDal.Setup(x => x.ListData(It.IsAny<DateTime>()))
            .Returns(MayBe<IEnumerable<PasienDto>>.Some(new List<PasienDto> { pasien }));

        var actual = _repo.ListData(new SearchKeyword("budi 2000-01-01"));
        
        actual.Value.Count().Should().Be(1);
    }    

}