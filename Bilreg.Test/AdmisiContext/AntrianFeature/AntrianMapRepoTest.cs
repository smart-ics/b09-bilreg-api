using System;
using System.Collections.Generic;
using System.Linq;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.AdmisiContext.AntrianFeature;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AntrianMapRepoTest
{
    private readonly Mock<IAntrianMapDal> _hdrMock;
    private readonly Mock<IAntrianMapDetilDal> _detilMock;
    private readonly AntrianMapRepo _sut;

    public AntrianMapRepoTest()
    {
        _hdrMock = new Mock<IAntrianMapDal>();
        _detilMock = new Mock<IAntrianMapDetilDal>();
        _sut = new AntrianMapRepo(_hdrMock.Object, _detilMock.Object);
    }

    [Fact]
    public void SaveChanges_ShouldInsertHeaderAndInsertAllDetails_WhenHeaderDoesNotExist()
    {
        // Arrange
        var model = BuildModel("AM1", 3);
        _hdrMock.Setup(x => x.GetData(It.IsAny<IAntrianMapKey>())).Returns((AntrianMapDto?)null);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _hdrMock.Verify(x => x.Insert(It.IsAny<AntrianMapDto>()), Times.Once);
        _hdrMock.Verify(x => x.Update(It.IsAny<AntrianMapDto>()), Times.Never);
        _detilMock.Verify(x => x.Delete(It.IsAny<AntrianMapModel>()), Times.Once);
        _detilMock.Verify(x => x.Insert(It.IsAny<AntrianMapDetilDto>()), Times.Exactly(3));
    }

    [Fact]
    public void SaveChanges_ShouldUpdateHeaderAndReplaceDetails_WhenHeaderExists()
    {
        // Arrange
        var model = BuildModel("AM2", 2);
        var existing = BuildDto("AM2");
        _hdrMock.Setup(x => x.GetData(It.IsAny<IAntrianMapKey>())).Returns(existing);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _hdrMock.Verify(x => x.Update(It.IsAny<AntrianMapDto>()), Times.Once);
        _hdrMock.Verify(x => x.Insert(It.IsAny<AntrianMapDto>()), Times.Never);
        _detilMock.Verify(x => x.Delete(It.IsAny<AntrianMapModel>()), Times.Once);
        _detilMock.Verify(x => x.Insert(It.IsAny<AntrianMapDetilDto>()), Times.Exactly(2));
    }

    [Fact]
    public void SaveChanges_ShouldDeleteDetailsAndInsertNone_WhenModelHasNoDetails()
    {
        // Arrange
        var model = BuildModel("AM3", 0);
        _hdrMock.Setup(x => x.GetData(It.IsAny<IAntrianMapKey>())).Returns((AntrianMapDto?)null);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _detilMock.Verify(x => x.Delete(It.IsAny<AntrianMapModel>()), Times.Once);
        _detilMock.Verify(x => x.Insert(It.IsAny<AntrianMapDetilDto>()), Times.Never);
    }

    [Fact]
    public void SaveChanges_ShouldPropagateException_WhenHeaderInsertThrows()
    {
        // Arrange
        var model = BuildModel("AM4", 1);
        _hdrMock.Setup(x => x.GetData(It.IsAny<IAntrianMapKey>())).Returns((AntrianMapDto?)null);
        _hdrMock.Setup(x => x.Insert(It.IsAny<AntrianMapDto>())).Throws(new InvalidOperationException("DB error"));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _sut.SaveChanges(model));
    }

    [Fact]
    public void LoadEntity_ShouldReturnNone_WhenHeaderNotFound()
    {
        // Arrange
        _hdrMock.Setup(x => x.GetData(It.IsAny<IAntrianMapKey>())).Returns((AntrianMapDto?)null);

        // Act
        var result = _sut.LoadEntity(AntrianMapModel.Key("X"));

        // Assert
        Assert.False(result.HasValue);
    }

    [Fact]
    public void LoadEntity_ShouldReturnModelWithDetails_WhenHeaderAndDetailsExist()
    {
        // Arrange
        var dto = BuildDto("AM5");
        var detils = new List<AntrianMapDetilDto> { BuildDetilDto("AM5", 1), BuildDetilDto("AM5", 2) };
        _hdrMock.Setup(x => x.GetData(It.IsAny<IAntrianMapKey>())).Returns(dto);
        _detilMock.Setup(x => x.ListData(It.IsAny<IAntrianMapKey>())).Returns(detils);

        // Act
        var result = _sut.LoadEntity(AntrianMapModel.Key("AM5"));

        // Assert
        Assert.True(result.HasValue);
        result.Match(
            onSome: model => Assert.Equal(2, model.ListMap.Count()),
            onNone: () => Assert.Fail("Expected Some but got None")
        );
    }

    [Fact]
    public void LoadEntity_ShouldReturnModelWithEmptyDetails_WhenDetailListIsNull()
    {
        // Arrange
        var dto = BuildDto("AM6");
        _hdrMock.Setup(x => x.GetData(It.IsAny<IAntrianMapKey>())).Returns(dto);
        _detilMock.Setup(x => x.ListData(It.IsAny<IAntrianMapKey>())).Returns((IEnumerable<AntrianMapDetilDto>?)null);

        // Act
        var result = _sut.LoadEntity(AntrianMapModel.Key("AM6"));

        // Assert
        Assert.True(result.HasValue);
        result.Match(
            onSome: model => Assert.Empty(model.ListMap),
            onNone: () => Assert.Fail("Expected Some but got None")
        );
    }

    [Fact]
    public void LoadEntity_ShouldPropagateException_WhenHeaderGetDataThrows()
    {
        // Arrange
        _hdrMock.Setup(x => x.GetData(It.IsAny<IAntrianMapKey>())).Throws(new InvalidOperationException("DB error"));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _sut.LoadEntity(AntrianMapModel.Key("AM7")));
    }

    [Fact]
    public void ListData_ShouldReturnEmptyList_WhenNoDataFound()
    {
        // Arrange
        _hdrMock.Setup(x => x.ListData(It.IsAny<ILayananKey>(), It.IsAny<IPpaKey>(), It.IsAny<DateOnly>())).Returns((IEnumerable<AntrianMapDto>?)null);

        // Act
        var result = _sut.ListData(new LayananReff("L1", "L1"), new PpaReff("D1", "D1"), DateOnly.FromDateTime(DateTime.Today));

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ListData_ShouldReturnHdrViewList_WhenDataExists()
    {
        // Arrange
        var dto = BuildDto("AM8");
        _hdrMock.Setup(x => x.ListData(It.IsAny<ILayananKey>(), It.IsAny<IPpaKey>(), It.IsAny<DateOnly>())).Returns(new[] { dto });

        // Act
        var result = _sut.ListData(new LayananReff("L2", "L2"), new PpaReff("D2", "D2"), DateOnly.FromDateTime(DateTime.Today));

        // Assert
        Assert.Single(result);
        Assert.NotNull(result.First());
    }

    [Fact]
    public void ListData_ShouldPropagateException_WhenListDataThrows()
    {
        // Arrange
        _hdrMock.Setup(x => x.ListData(It.IsAny<ILayananKey>(), It.IsAny<IPpaKey>(), It.IsAny<DateOnly>())).Throws(new InvalidOperationException("DB error"));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _sut.ListData(new LayananReff("L3", "L3"), new PpaReff("D3", "D3"), DateOnly.FromDateTime(DateTime.Today)));
    }

    #region Helpers
    private static AntrianMapModel BuildModel(string id, int detilCount)
    {
        var dokter = new PpaReff("D1", "Dokter");
        var layanan = new LayananReff("L1", "Layanan");
        var tgl = DateOnly.FromDateTime(DateTime.Today);
        var jam = TimeOnly.Parse("09:00");
        var jamPraktek = TimeOnly.Parse("09:30");

        var detils = new List<AntrianMapDetilModel>();
        for (int i = 1; i <= detilCount; i++)
        {
            var pasien = new PasienReff($"MR{i}", $"Name{i}", new DateOnly(2000, 1, 1), "M");
            detils.Add(new AntrianMapDetilModel(i, pasien.PasienName, pasien.PasienId, $"REF{i}", "FLAG", false));
        }

        return new AntrianMapModel(id, "J1", new PpaReff("D1", "Dokter"), new LayananReff("L1", "Layanan"), tgl, jam, jamPraktek, "PAT", 10, detils);
    }

    private static AntrianMapDto BuildDto(string id)
    {
        return new AntrianMapDto(id, "J1", "D1", "L1", DateTime.Today, "09:00", "09:30", "PAT", 10, "Dokter", "Layanan");
    }

    private static AntrianMapDetilDto BuildDetilDto(string id, int no)
    {
        return new AntrianMapDetilDto(id, "D1", "L1", DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd"), "09:00", no, "FLAG", $"MR{no}", $"Name{no}", $"REF{no}", false, "Dokter", "Layanan");
    }
    #endregion
}