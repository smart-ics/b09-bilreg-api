// using Farinv.Domain.InventoryContext.StokFeature;
// using Farinv.Infrastructure.InventoryContext.StokFeature;
// using Moq;
//
// public class StokRepoTests
// {
//     private readonly Mock<Itb_stok_dal> _tb_stok_dalMock;
//     private readonly Mock<Itb_buku_dal> _tb_buku_dalMock;
//     private readonly Mock<IStokLayerDal> _stokLayerDalMock;
//     private readonly Mock<IStokBukuMapDal> _stokBukuMapDalMock;
//     private readonly Mock<IStokDal> _stokDalMock;
//     private readonly StokRepo _repository;
//     public StokRepoTests()
//     {
//         _tb_stok_dalMock = new Mock<Itb_stok_dal>();
//         _tb_buku_dalMock = new Mock<Itb_buku_dal>();
//         _stokLayerDalMock = new Mock<IStokLayerDal>();
//         _stokBukuMapDalMock = new Mock<IStokBukuMapDal>();
//         _stokDalMock = new Mock<IStokDal>();
//         _repository = new StokRepo(
//             _stokDalMock.Object,
//             _stokLayerDalMock.Object,
//             _stokBukuMapDalMock.Object,
//             _tb_stok_dalMock.Object,
//             _tb_buku_dalMock.Object);
//     }
//     
//     [Fact]
//     public void UT1_GivenNoTbStok_WhenRekonstruksiStok_ThenNoOperation()
//     {
//         // Arrange
//         var key = CreateTestKey();
//         _tb_stok_dalMock
//             .Setup(x => x.ListData(key, key))
//             .Returns(new List<tb_stok_dto>());
//         // Act
//         _repository.RekonstruksiStok(key);
//         // Assert
//         _tb_buku_dalMock.Verify(x => x.ListData(key, It.IsAny<List<string>>()), Times.Never);
//         _tb_stok_dalMock.Verify(x => x.Delete(It.IsAny<string>()), Times.Never);
//         _tb_stok_dalMock.Verify(x => x.Insert(It.IsAny<tb_stok_dto>()), Times.Never);
//         _stokLayerDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<StokLayerDto>>()), Times.Never);
//         _stokBukuMapDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<StokBukuMapDto>>()), Times.Never);
//         _stokDalMock.Verify(x => x.Insert(It.IsAny<StokDto>()), Times.Never);
//     }
//     
//     [Fact]
//     public void UT2_GivenTbStokWithConsistentQty_WhenRekonstruksiStok_ThenOnlyCreateLayerAndStok()
//     {
//         // Arrange
//         var key = CreateTestKey();
//         var tbStokList = new List<tb_stok_dto> { CreateTestTbStokDto() };
//         var tbBukuList = new List<tb_buku_dto>
//         {
//             CreateTestTbBukuDto1(),
//             CreateTestTbBukuDto2(),
//         };
//         _tb_stok_dalMock
//             .Setup(x => x.ListData(key, key))
//             .Returns(tbStokList);
//         _tb_buku_dalMock
//             .Setup(x => x.ListData(key, It.IsAny<List<string>>()))
//             .Returns(tbBukuList);
//         // Act
//         _repository.RekonstruksiStok(key);
//         // Assert
//         _tb_stok_dalMock.Verify(x => x.Delete(It.IsAny<string>()), Times.Never);
//         _tb_stok_dalMock.Verify(x => x.Insert(It.IsAny<tb_stok_dto>()), Times.Never);
//         _stokLayerDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<StokLayerDto>>()), Times.Once);
//         _stokBukuMapDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<StokBukuMapDto>>()), Times.Once);
//         _stokDalMock.Verify(x => x.Insert(It.IsAny<StokDto>()), Times.Once);
//     }
//     
//     [Fact]
//     public void UT3_GivenTbStokWithInconsistentQtyZero_WhenRekonstruksiStok_ThenDeleteTbStok()
//     {
//         // Arrange
//         var key = CreateTestKey();
//         var tbStokList = new List<tb_stok_dto> 
//         { 
//             CreateTestTbStokDto()
//         };
//         var tbBukuList = new List<tb_buku_dto>
//         {
//             CreateTestTbBukuDto1(),
//             CreateTestTbBukuDto2() with {fn_stok_out = 3},
//         };
//         _tb_stok_dalMock
//             .Setup(x => x.ListData(key, key))
//             .Returns(tbStokList);
//         _tb_buku_dalMock
//             .Setup(x => x.ListData(key, It.IsAny<List<string>>()))
//             .Returns(tbBukuList);
//         // Act
//         _repository.RekonstruksiStok(key);
//         // Assert
//         _tb_stok_dalMock.Verify(x => x.Delete(It.IsAny<string>()), Times.Once);
//         //_tb_stok_dalMock.Verify(x => x.Insert(It.IsAny<tb_stok_dto>()), Times.Never);
//         _stokLayerDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<StokLayerDto>>()), Times.Once);
//         _stokBukuMapDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<StokBukuMapDto>>()), Times.Once);
//         _stokDalMock.Verify(x => x.Insert(It.IsAny<StokDto>()), Times.Once);
//     }
//     
//     [Fact]
//     public void UT4_GivenTbStokWithInconsistentQtyNonZero_WhenRekonstruksiStok_ThenUpdateTbStok()
//     {
//         // Arrange
//         var key = CreateTestKey();
//         var tbStokList = new List<tb_stok_dto> 
//         { 
//             CreateTestTbStokDto() 
//         };
//         var tbBukuList = new List<tb_buku_dto> 
//         { 
//             CreateTestTbBukuDto1(),
//             CreateTestTbBukuDto2() with {fn_stok_out = 1},
//         };
//         _tb_stok_dalMock
//             .Setup(x => x.ListData(key, key))
//             .Returns(tbStokList);
//         _tb_buku_dalMock
//             .Setup(x => x.ListData(key, It.IsAny<List<string>>()))
//             .Returns(tbBukuList);
//         // Act
//         _repository.RekonstruksiStok(key);
//         // Assert
//         _tb_stok_dalMock.Verify(x => x.Delete(It.IsAny<string>()), Times.Never);
//         _tb_stok_dalMock.Verify(x => x.Insert(It.IsAny<tb_stok_dto>()), Times.Never);
//         _tb_stok_dalMock.Verify(x => x.Update(It.IsAny<tb_stok_dto>()), Times.Once);
//         _stokLayerDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<StokLayerDto>>()), Times.Once);
//         _stokBukuMapDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<StokBukuMapDto>>()), Times.Once);
//         _stokDalMock.Verify(x => x.Insert(It.IsAny<StokDto>()), Times.Once);
//     }
//     
//     private static tb_stok_dto CreateTestTbStokDto()
//         => new tb_stok_dto("A", "B",  "C",
//             "D","E", "2025-12-13", "G", 3, 1, 3, "2025-11-15", "08:55", "K", "L", "M", "N", "O", "P");
//     
//     private static tb_buku_dto CreateTestTbBukuDto1()
//         => new tb_buku_dto("A", "B",  "C",
//             "D","E", "F", "G", 3, 0, 3, "I", "J", "K", "L", "M", "N", "O", "P", "Q");
//     
//     private static tb_buku_dto CreateTestTbBukuDto2()
//         => new tb_buku_dto("A", "B",  "C",
//             "D","E", "F", "G", 0, 2, 3, "I", "J", "K", "L", "M", "N", "O", "P", "Q");
//     
//     private static IStokKey CreateTestKey()
//         => StokModel.Key("B", "C"); 
// }