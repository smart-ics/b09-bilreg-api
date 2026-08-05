// using Farinv.Application.InventoryContext.StokFeature;
// using Farinv.Domain.InventoryContext.StokFeature;
// using Nuna.Lib.PatternHelper;
//
// // resharper disable inconsistentnaming
// namespace Farinv.Infrastructure.InventoryContext.StokFeature;
//
// public class StokRepo : IStokRepo
// {
//     private readonly IStokDal _stokDal;
//     private readonly IStokLayerDal _stokLayerDal;
//     private readonly Itb_stok_dal _tb_stok_dal;
//     private readonly Itb_buku_dal _tb_buku_dal;
//
//
//     public StokRepo(IStokDal stokDal, IStokLayerDal stokLayerDal,
//         Itb_stok_dal tbStokDal,
//         Itb_buku_dal tbBukuDal)
//     {
//         _stokDal = stokDal;
//         _stokLayerDal = stokLayerDal;
//         _tb_stok_dal = tbStokDal;
//         _tb_buku_dal = tbBukuDal;
//     }
//
//     public void SaveChanges(StokModel model)
//     {
//         throw new NotImplementedException();
//     }
//
//     public MayBe<StokModel> LoadEntity(IStokKey key)
//     {
//         // var brgDto = _stokDal.GetData(key);
//         // if (brgDto is null)
//         //     RekonstruksiStok(key);
//         //
//         // brgDto = _stokDal.GetData(key);
//         // if (brgDto is null)
//         //     return MayBe<StokModel>.None;
//         //
//         // var listLayerDto = _stokLayerDal.ListData(key)?.ToList() ?? [];
//         // var listStokLayerKey = listLayerDto.Select(x => StokLayerModel.Key(x.StokLayerId)).ToList();
//         // var listBukuDto = _tb_buku_dal.ListData(listStokLayerKey)?.ToList() ?? [];
//         //
//         // var listBukuByLayer = listBukuDto
//         //     .GroupBy(dto => dto.fs_stok_layer_id)
//         //     .ToDictionary(group => group.Key, group => group.Select(dto => dto.ToModel()).AsEnumerable());
//         //
//         // var listLayer = listLayerDto
//         //     .Select(x => x.ToModel(listBukuByLayer.First(y => y.Key == x.StokLayerId).Value)).ToList();
//         //
//         // var stok = brgDto.ToModel(listLayer);
//         // return MayBe<StokModel>.Some(stok);
//         throw new NotImplementedException();
//     }
//
//     public void DeleteEntity(IStokKey key)
//     {
//         throw new NotImplementedException();
//     }
//     public IEnumerable<StokBalanceView> ListData()
//     {
//         throw new NotImplementedException();
//     }
//
//     #region PRIVATE-HELPER
//
//     public void RekonstruksiStok(IStokKey key)
//     {
//         //  1. ambil tb_stok
//         var listTbStok = _tb_stok_dal.ListData(key, key)?.ToList() ?? [];
//         if (listTbStok.Count == 0)
//             return;
//         //  2. list tb_buku by kode do dari listTbStok
//         var listTbBuku = _tb_buku_dal
//             .ListData(
//                 key, listTbStok
//                     .Select(x => x.fs_kd_do)
//                     .ToList())?.ToList() ?? [];
//         //  3. cek dan perbaiki konsistensi tb_stok dan tb_buku
//         foreach (var item in listTbStok)
//         {
//             var qtyTbBuku = listTbBuku
//                 .Where(x => x.fs_kd_do == item.fs_kd_do)
//                 .Sum(x => x.fn_stok_in - x.fn_stok_out);
//             if (qtyTbBuku == 0)
//             {
//                 _tb_stok_dal.Delete(item.fs_kd_trs);
//                 continue;
//             }
//             if (qtyTbBuku == item.fn_qty) continue;
//             var validTbStokItem = item with { fn_qty = qtyTbBuku };
//             _tb_stok_dal.Update(validTbStokItem);
//         }
//         //  4. membentuk FARIN_StokLayer
//         listTbStok = _tb_stok_dal.ListData(key, key)?.ToList() ?? [];
//         var listStokLayerDto = new List<StokLayerDto>();
//         // foreach (var item in listTbStok)
//         // {
//         //     var newStokLayerId = Ulid.NewUlid().ToString();
//         //     var stokLayerDto = new StokLayerDto(newStokLayerId, item.fs_kd_barang, item.fs_kd_layanan,
//         //         item.fs_kd_do, item.fd_tgl_do.ToDate(DateFormatEnum.YMD),
//         //         item.fs_kd_po, item.fs_kd_do, item.fd_tgl_ed.ToDate(DateFormatEnum.YMD),
//         //         item.fs_no_batch, item.fn_qty_in, item.fn_qty, item.fn_hpp, item.fs_nm_barang, item.fs_nm_layanan);
//         //     listStokLayerDto.Add(stokLayerDto);
//         // }
//         _stokLayerDal.Insert(listStokLayerDto);
//         //  5. membentuk FARIN_StokBukuMap
//         // foreach (var item in listStokLayerDto)
//         // {
//         //     var listBukuDto = listTbBuku.Where(x => x.fs_kd_do == item.ReceiveId).ToList();
//         //     var newBukuMap = listBukuDto
//         //         .Select(x => new StokBukuMapDto(item.StokLayerId, x.fs_kd_trs));
//         //
//         //     _stokBukuMapDal.Insert(newBukuMap);
//         // }
//         // 6. membentuk FARIN_Stok
//         var qty = listStokLayerDto.Sum(x => x.QtySisa);
//         var satuan = listTbBuku.First().fs_kd_satuan;
//         var brgName = listTbBuku.First().fs_nm_barang;
//         var layananName = listTbStok.First().fs_nm_layanan;
//         var newFarinStok = new StokDto(key.BrgId, key.LayananId, qty, satuan, brgName, layananName);
//         _stokDal.Insert(newFarinStok);
//     }
//
//     private void RekonstruksiStok2(IStokKey key)
//     {
//         var listTbStok = _tb_stok_dal.ListData(key, key)?.ToList() ?? [];
//         if (listTbStok.Count == 0) return;
//
//         var listTbBuku = _tb_buku_dal.ListData(key, listTbStok
//             .Select(x => x.fs_kd_do)
//             .ToList())?.ToList() ?? [];
//         var qtyStok = listTbStok.Sum(x => x.fn_qty);
//         var qtyBuku = listTbBuku.Sum(x => x.fn_stok_in - x.fn_stok_out);
//         if (qtyStok != qtyBuku)
//             throw new InvalidOperationException("Inkonsisten data tb_buku vs tb_stok");
//         var stok = StokModel.
//         var listLayer = GenerateLayer(listTbBuku);
//     }
//
//     private IEnumerable<StokLayerModel> GenerateLayer(IEnumerable<tb_buku_dto> listTbBuku)
//     {
//         var listStokMasuk = listTbBuku
//             .Where(x => x.fn_stok_in > 0)
//             .GroupBy(x => new { x.fs_kd_po, x.fs_kd_do, x.fs_kd_mutasi })
//             .Select(x => StokLayerModel.Create())
//
//     }
//
//     #endregion
// }
