VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
  Persistable = 0  'NotPersistable
  DataBindingBehavior = 0  'vbNone
  DataSourceBehavior  = 0  'vbNone
  MTSTransactionMode  = 0  'NotAnMTSObject
END
Attribute VB_Name = "clbGenStokX1"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
Option Explicit

Const MUTASI_DO = "DO"
Const MUTASI_DO_VOID = "DO_V"
Const MUTASI_MUTASI_OUT = "MT_OUT"
Const MUTASI_MUTASI_IN = "MT_IN"
Const MUTASI_MUTASI_VOID_OUT = "MT_OUT_V"
Const MUTASI_MUTASI_VOID_IN = "MT_IN_V"
Const MUTASI_PAKAI = "PK"
Const MUTASI_PAKAI_VOID = "PK_V"
Const MUTASI_JUAL = "DB"
Const MUTASI_JUAL_VOID = "DB_V"
Const MUTASI_JUAL_UMUM = "DU"
Const MUTASI_JUAL_UMUM_VOID = "DU_V"
Const MUTASI_JUAL_TIPE = "DT"
Const MUTASI_JUAL_TIPE_VOID = "DT_V"

Const MUTASI_MUSNAH = "MN"
Const MUTASI_MUSNAH_VOID = "MN_V"
Const MUTASI_RETUR_BELI = "RB"
Const MUTASI_RETUR_BELI_VOID = "RB_V"
Const MUTASI_RETUR_JUAL = "RJ"
Const MUTASI_RETUR_JUAL_VOID = "RJ_V"
Const MUTASI_RETUR_JUAL_UMUM = "RU"
Const MUTASI_RETUR_JUAL_UMUM_VOID = "RU_V"
Const MUTASI_RETUR_JUAL_TIPE = "RT"
Const MUTASI_RETUR_JUAL_TIPE_VOID = "RT_V"


Const MUTASI_ADJUSTMENT_PLUS = "AJ_PLUS"
Const MUTASI_ADJUSTMENT_MIN = "AJ_MIN"
Const MUTASI_ADJUSTMENT_VOID_PLUS = "AJ_PLUS_V"
Const MUTASI_ADJUSTMENT_VOID_MIN = "AJ_MIN_V"
Const MUTASI_REPACK_ASAL = "RP_ASAL"
Const MUTASI_REPACK_HASIL = "RP_HASIL"
Const MUTASI_REPACK_VOID_ASAL = "RP_ASAL_V"
Const MUTASI_REPACK_VOID_HASIL = "RP_HASIL_V"

Const MUTASI_RESERVED_ORDER_IN = "DU-R_IN"
Const MUTASI_RESERVED_ORDER_OUT = "DU-R_OUT"
Const MUTASI_RESERVED_ORDER_IN_VOID = "DU-R_IN_V"
Const MUTASI_RESERVED_ORDER_OUT_VOID = "DU-R_OUT_V"
Const MUTASI_SERAH = "DU-S"
Const MUTASI_SERAH_VOID = "DU-S_V"

Const TRS_DO = "DM"
Const TRS_MUTASI = "MT"
Const TRS_PAKAI = "PK"
Const TRS_JUAL_UMUM = "DU"
Const TRS_JUAL_TIPE = "DT"
Const TRS_MUSNAH = "MN"
Const TRS_RETUR_BELI = "RB"
Const TRS_RETUR_JUAL_UMUM = "RU"
Const TRS_RETUR_JUAL_TIPE = "RT"

Const TRS_ADJUSTMENT = "AJ"
Const TRS_REPACK = "RP"

Const TRS_DU_RESERVED = "DR"
Const TRS_DU_SERAH = "DS"


Const KODE_PREFIX_BUKU = "BK"
Const KODE_LENGTH_BUKU = 10

Const KODE_PREFIX_STOK = "ST"
Const KODE_LENGTH_STOK = 10

Public LastMsg As String

Public Function Generate(ByVal xKodeTrs As String, _
                         ByVal xVoid As Boolean, _
                         ByVal xTglVoid As String, _
                         ByVal xJamVoid As String, _
                         ByVal xVoidDelete As Boolean) As Boolean
                         
    Dim xPrefix As String
    Dim xLanjut As Boolean
    xLanjut = False
    
    If Len(xKodeTrs) <> 10 Then
        Err.Raise vbError + 1, "Generate Stok", "KODE TRANSAKSI tidak dikenal"
    End If
    
    xPrefix = Left(xKodeTrs, 2)
    Select Case xPrefix
        Case TRS_DO
            If Not xVoid Then
                xLanjut = GenStokDO(xKodeTrs)
            Else
                xLanjut = GenStokDOVoid(xKodeTrs, xTglVoid, xJamVoid, xVoidDelete)
            End If
        
        Case TRS_MUTASI
            If Not xVoid Then
                xLanjut = GenStokMutasi(xKodeTrs)
            Else
                xLanjut = GenStokMutasiVoid(xKodeTrs, xTglVoid, xJamVoid, xVoidDelete)
            End If
        
        Case TRS_PAKAI
            If Not xVoid Then
                xLanjut = GenStokPakai(xKodeTrs)
            Else
                xLanjut = GenStokPakaiVoid(xKodeTrs, xTglVoid, xJamVoid, xVoidDelete)
            End If
        
        Case TRS_JUAL_UMUM
            If Not xVoid Then
                xLanjut = GenStokDOBillUmum(xKodeTrs)
            Else
                xLanjut = GenStokDOBillUmumVoid(xKodeTrs, xTglVoid, xJamVoid, xVoidDelete)
            End If
        
        '---reserved (sama aja dengan mutasi)
        Case TRS_DU_RESERVED
            xKodeTrs = "MT" & Right(xKodeTrs, 8)
            If Not xVoid Then
                xLanjut = GenStokDOBillUmumReserved(xKodeTrs)
            Else
                xLanjut = GenStokDOBillUmumReservedVoid(xKodeTrs, xTglVoid, xJamVoid, xVoidDelete)
            End If
        
        '---serah
        Case TRS_DU_SERAH
            xKodeTrs = "DU" & Right(xKodeTrs, 8)
            If Not xVoid Then
                xLanjut = GenStokDOBillUmumSerah(xKodeTrs)
            Else
                xLanjut = GenStokDOBillUmumSerahVoid(xKodeTrs, xTglVoid, xJamVoid, xVoidDelete)
            End If
        
        Case TRS_JUAL_TIPE
            If Not xVoid Then
                xLanjut = GenStokDOBillTipe(xKodeTrs)
            Else
                xLanjut = GenStokDOBillTipeVoid(xKodeTrs, xTglVoid, xJamVoid, xVoidDelete)
            End If
            
        Case TRS_MUSNAH
            If Not xVoid Then
                xLanjut = GenStokMusnah(xKodeTrs)
            Else
                xLanjut = GenStokMusnahVoid(xKodeTrs, xTglVoid, xJamVoid, xVoidDelete)
            End If

        Case TRS_RETUR_BELI
            If Not xVoid Then
                xLanjut = GenStokReturBeli(xKodeTrs)
            Else
                xLanjut = GenStokReturBeliVoid(xKodeTrs, xTglVoid, xJamVoid, xVoidDelete)
            End If

        Case TRS_RETUR_JUAL_UMUM
            If Not xVoid Then
                xLanjut = GenStokReturJualUmum(xKodeTrs)
            Else
                xLanjut = GenStokReturJualUmumVoid(xKodeTrs, xTglVoid, xJamVoid, xVoidDelete)
            End If
        
        Case TRS_RETUR_JUAL_TIPE
            If Not xVoid Then
                xLanjut = GenStokReturJualTipe(xKodeTrs)
            Else
                xLanjut = GenStokReturJualTipeVoid(xKodeTrs, xTglVoid, xJamVoid, xVoidDelete)
            End If
        
        Case TRS_ADJUSTMENT
            If Not xVoid Then
                xLanjut = GenStokAdjust(xKodeTrs)
            Else
                xLanjut = GenStokAdjustVoid(xKodeTrs, xTglVoid, xJamVoid, xVoidDelete)
            End If
            
        Case TRS_REPACK
            If Not xVoid Then
                xLanjut = GenStokRepack(xKodeTrs)
            Else
                xLanjut = GenStokRepackVoid(xKodeTrs, xTglVoid, xJamVoid, xVoidDelete)
            End If
    End Select
    
    Generate = xLanjut
End Function

'--IN
Private Function GenStokDO(ByVal xKodeTrs As String) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    '---ambil data FO (tb_trs_do2)
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_qty, fs_kd_satuan, fd_tgl_ed, fs_no_batch ," _
         & "            fd_tgl_trs, fs_jam_trs, fs_kd_po, fn_harga, fn_diskon, fn_tax " _
         & " FROM       tb_trs_do aa " _
         & " INNER JOIN tb_trs_do2 bb ON aa.fs_kd_trs = bb.fs_kd_trs" _
         & " WHERE      aa.fs_kd_trs = '" & xKodeTrs & "' "
    
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    
    If Not oRS.BOF Then
        Dim oBrg As clbBrg: Set oBrg = New clbBrg
        
        Do While Not oRS.EOF
            Dim xQty As Double
            Dim xHPP As Double
            Dim xDiskon As Double
            xQty = oBrg(oRS("fs_kd_barang").Value).ConvToTerkecil(oRS("fs_kd_satuan").Value, oRS("fn_qty").Value)
            xDiskon = oRS("fn_diskon").Value / (xQty / oRS("fn_qty").Value)
                 
            Dim xHargaBeli As Double
            Dim xTax As Double
            xTax = oRS("fn_tax").Value / xQty
            xHargaBeli = oRS("fn_harga").Value / (xQty / oRS("fn_qty").Value) '- xDiskon
            
            Select Case MyParam.Sistem.MetodePersediaanHPP
                Case "HPP"
                    xHPP = xHargaBeli
                Case "HPP_DIS"
                    xHPP = xHargaBeli - xDiskon
                Case "HPP_DIS_TAX"
                    xHPP = xHargaBeli - xDiskon + xTax
                Case Else
                    xHPP = xHargaBeli - xDiskon + xTax
            End Select
            
            AddStok oRS("fs_kd_barang").Value, _
                    oRS("fs_kd_layanan").Value, _
                    xQty, _
                    oBrg.SatuanTerkecil, _
                    xHPP, _
                    My.TglDMY(oRS("fd_tgl_ed").Value), _
                    oRS("fs_no_batch").Value, _
                    MUTASI_DO, _
                    xKodeTrs, _
                    My.TglDMY(oRS("fd_tgl_trs").Value), _
                    oRS("fs_jam_trs").Value, _
                    oRS("fs_kd_po").Value, _
                    xKodeTrs, _
                    False
            
            oRS.MoveNext
        Loop
        Set oBrg = Nothing
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokDO = xRetVal
End Function

'--OUT
Private Function GenStokDOVoid(ByVal xKodeTrs As String, _
                          ByVal xTglVoid As String, _
                          ByVal xJamVoid As String, _
                          ByVal xVoidDelete As Boolean) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_hpp, fn_stok_in, fn_stok_out, " _
         & "            fs_kd_po, fs_kd_do, fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi, fs_kd_satuan, " _
         & "            fd_tgl_ed, fs_no_batch " _
         & " FROM       tb_buku " _
         & " WHERE      fs_kd_mutasi = '" & xKodeTrs & "' " _
         & "        AND fs_kd_jenis_mutasi = '" & MUTASI_DO & "' "
         
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Do While Not oRS.EOF
            Dim xKodePO As String
            Dim xKodeDO As String
            Dim xNilaiHPP As Double
            RemoveStok oRS("fs_kd_barang").Value, _
                       oRS("fs_kd_layanan").Value, _
                       oRS("fn_stok_in").Value, _
                       oRS("fs_kd_satuan").Value, _
                       xNilaiHPP, _
                       My.TglDMY(oRS("fd_tgl_ed").Value), _
                       oRS("fs_no_batch").Value, _
                       MUTASI_DO_VOID, _
                       xKodeTrs, _
                       xTglVoid, _
                       xJamVoid, _
                       xKodePO, _
                       xKodeDO, _
                       xVoidDelete, _
                       xKodeTrs
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokDOVoid = xRetVal
End Function

'--OUT
Private Function GenStokReturBeli(ByVal xKodeTrs As String) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_qty, fs_kd_satuan, fd_tgl_ed, fs_no_batch ," _
         & "            fd_tgl_trs, fs_jam_trs, fs_kd_do " _
         & " FROM       tb_trs_rbeli aa" _
         & " INNER JOIN tb_trs_rbeli2 bb ON aa.fs_kd_trs = bb.fs_kd_trs " _
         & " WHERE      aa.fs_kd_trs = '" & xKodeTrs & "' "
    'sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_stok_in, fs_kd_satuan, fd_tgl_ed, fs_no_batch ," _
         & "            fd_tgl_mutasi, fs_jam_mutasi, fs_kd_do " _
         & " FROM       tb_buku " _
         & " WHERE      fs_kd_trs = '" & xKodeTrs & "' "
    
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
    
        Dim oBrg As clbBrg: Set oBrg = New clbBrg
        Dim xQty As Double
        Do While Not oRS.EOF
            '---byref variable
            Dim xKodePO As String
            Dim xKodeDO As String
            Dim xNilaiHPP As Double
            xQty = oBrg(oRS("fs_kd_barang").Value).ConvToTerkecil(oRS("fs_kd_satuan").Value, oRS("fn_qty").Value)
            
            Dim xKodeDO2 As String
            xKodeDO2 = oRS("fs_kd_do").Value
            '---<jude_was_here::2008-09-24::12:22 #43 ~2076 ^43>
            RemoveStok oRS("fs_kd_barang").Value, _
                       oRS("fs_kd_layanan").Value, _
                       xQty, _
                       oBrg.SatuanTerkecil, _
                       xNilaiHPP, _
                       My.TglDMY(oRS("fd_tgl_ed").Value), _
                       oRS("fs_no_batch").Value, _
                       MUTASI_RETUR_BELI, _
                       xKodeTrs, _
                       My.TglDMY(oRS("fd_tgl_trs").Value), _
                       oRS("fs_jam_trs").Value, _
                       xKodePO, _
                       xKodeDO, _
                       False, _
                       xKodeDO2
            
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    
    GenStokReturBeli = xRetVal
End Function

'--IN
Private Function GenStokReturBeliVoid(ByVal xKodeTrs As String, _
                                 ByVal xTglVoid As String, _
                                 ByVal xJamVoid As String, _
                                 ByVal xVoidDelete As Boolean) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_hpp, fn_stok_in, fn_stok_out, " _
         & "            fs_kd_po, fs_kd_do, fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi, fs_kd_satuan, " _
         & "            fd_tgl_ed, fs_no_batch " _
         & " FROM       tb_buku " _
         & " WHERE      fs_kd_mutasi = '" & xKodeTrs & "' " _
         & "        AND fs_kd_jenis_mutasi = '" & MUTASI_RETUR_BELI & "' "
         
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Do While Not oRS.EOF
            AddStok oRS("fs_kd_barang").Value, _
                    oRS("fs_kd_layanan").Value, _
                    oRS("fn_stok_out").Value, _
                    oRS("fs_kd_satuan").Value, _
                    oRS("fn_hpp").Value, _
                    My.TglDMY(oRS("fd_tgl_ed").Value), _
                    oRS("fs_no_batch").Value, _
                    MUTASI_RETUR_BELI_VOID, _
                    xKodeTrs, _
                    xTglVoid, _
                    xJamVoid, _
                    oRS("fs_kd_po").Value, _
                    oRS("fs_kd_do").Value, _
                    xVoidDelete
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokReturBeliVoid = xRetVal
End Function

'--OUT
Private Function GenStokDOBillUmum(ByVal xKodeTrs As String) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_qty, fs_kd_satuan, fd_tgl_ed, fs_no_batch ," _
         & "            fd_tgl_trs, fs_jam_trs " _
         & " FROM       tb_trs_dobill_umum aa" _
         & " INNER JOIN tb_trs_dobill_umum3 bb ON aa.fs_kd_trs = bb.fs_kd_trs " _
         & " WHERE      aa.fs_kd_trs = '" & xKodeTrs & "' "
    
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Dim oBrg As clbBrg: Set oBrg = New clbBrg
        Dim xQty As Double
        Do While Not oRS.EOF
            Dim xKodePO As String
            Dim xKodeDO As String
            Dim xNilaiHPP As Double
            xQty = oBrg(oRS("fs_kd_barang").Value).ConvToTerkecil(oRS("fs_kd_satuan").Value, oRS("fn_qty").Value)
            RemoveStok oRS("fs_kd_barang").Value, _
                       oRS("fs_kd_layanan").Value, _
                       xQty, _
                       oBrg.SatuanTerkecil, _
                       xNilaiHPP, _
                       My.TglDMY(oRS("fd_tgl_ed").Value), _
                       oRS("fs_no_batch").Value, _
                       MUTASI_JUAL_UMUM, _
                       xKodeTrs, _
                       My.TglDMY(oRS("fd_tgl_trs").Value), _
                       oRS("fs_jam_trs").Value, _
                       xKodePO, _
                       xKodeDO, _
                       False
                       
            '---update KODE PO dan KODE DO di front office
            '   untuk kepentingan retur jual
            sSQL = " UPDATE     tb_trs_dobill_umum3 " _
                 & " SET        fs_kd_po = '" & xKodePO & "', " _
                 & "            fs_kd_do = '" & xKodeDO & "',  " _
                 & "            fn_hpp = '" & xNilaiHPP & "'  " _
                 & " WHERE      fs_kd_trs = '" & xKodeTrs & "' " _
                 & "        AND fs_kd_barang = '" & oRS("fs_kd_barang").Value & "' " _
                 & "        AND fd_tgl_ed = '" & oRS("fd_tgl_ed").Value & "' "
            oConn.Execute sSQL
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokDOBillUmum = xRetVal
End Function

'--IN
Private Function GenStokDOBillUmumVoid(ByVal xKodeTrs As String, _
                                  ByVal xTglVoid As String, _
                                  ByVal xJamVoid As String, _
                                  ByVal xVoidDelete As Boolean) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_hpp, fn_stok_in, fn_stok_out, " _
         & "            fs_kd_po, fs_kd_do, fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi, " _
         & "            fd_tgl_ed, fs_no_batch, fs_kd_satuan " _
         & " FROM       tb_buku " _
         & " WHERE      fs_kd_mutasi = '" & xKodeTrs & "' " _
         & "        AND fs_kd_jenis_mutasi = '" & MUTASI_JUAL_UMUM & "' "
         
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Do While Not oRS.EOF
            AddStok oRS("fs_kd_barang").Value, _
                    oRS("fs_kd_layanan").Value, _
                    oRS("fn_stok_out").Value, _
                    oRS("fs_kd_satuan").Value, _
                    oRS("fn_hpp").Value, _
                    My.TglDMY(oRS("fd_tgl_ed").Value), _
                    oRS("fs_no_batch").Value, _
                    MUTASI_JUAL_UMUM_VOID, _
                    xKodeTrs, _
                    xTglVoid, _
                    xJamVoid, _
                    oRS("fs_kd_po").Value, _
                    oRS("fs_kd_do").Value, _
                    xVoidDelete
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokDOBillUmumVoid = xRetVal
End Function

'--OUT
Private Function GenStokDOBillUmumSerah(ByVal xKodeTrs As String) As Boolean
    Dim xLanjut As Boolean
    xLanjut = True
    
    '---ambil kode mutasi-nya
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_trs_mutasi, fd_tgl_serah, fs_jam_serah " & vbCrLf _
         & " FROM       tb_trs_dobill_umum " & vbCrLf _
         & " WHERE      fs_kd_trs = '" & xKodeTrs & "' " & vbCrLf
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Dim xKodeTrsMT As String
        Dim xTglSerah As String
        Dim xJamSerah As String
        xKodeTrsMT = oRS("fs_kd_trs_mutasi").Value
        xTglSerah = oRS("fd_tgl_serah").Value
        xJamSerah = oRS("fs_jam_serah").Value
    Else
        xLanjut = False
        LastMsg = "Kode Trs Mutasi tidak ditemukan"
    End If
    oRS.Close
    
    '---proses pemotongan stok
    If xLanjut Then
        sSQL = " SELECT     fs_kd_barang, fs_kd_layanan_tujuan as fs_kd_layanan, fn_qty, fs_kd_satuan, fd_tgl_ed, fs_no_batch " & vbCrLf _
             & "            " & vbCrLf _
             & " FROM       tb_trs_mutasi aa " & vbCrLf _
             & " INNER JOIN tb_trs_mutasi2 bb ON aa.fs_kd_trs = bb.fs_kd_trs " & vbCrLf _
             & " WHERE      aa.fs_kd_trs = '" & xKodeTrsMT & "' " & vbCrLf
        Set oRS = New adodb.Recordset
        oRS.Open sSQL, oConn
        
        If Not oRS.BOF Then
            Dim oBrg As clbBrg: Set oBrg = New clbBrg
            Dim xQty As Double
            Do While Not oRS.EOF
                Dim xKodePO As String
                Dim xKodeDO As String
                Dim xNilaiHPP As Double
                xQty = oBrg(oRS("fs_kd_barang").Value).ConvToTerkecil(oRS("fs_kd_satuan").Value, oRS("fn_qty").Value)
                RemoveStok oRS("fs_kd_barang").Value, _
                           oRS("fs_kd_layanan").Value, _
                           xQty, _
                           oBrg.SatuanTerkecil, _
                           xNilaiHPP, _
                           My.TglDMY(oRS("fd_tgl_ed").Value), _
                           oRS("fs_no_batch").Value, _
                           MUTASI_SERAH, _
                           xKodeTrs, _
                           My.TglDMY(xTglSerah), _
                           xJamSerah, _
                           xKodePO, _
                           xKodeDO, _
                           False
                           
                '---update KODE PO dan KODE DO di front office
                '   untuk kepentingan retur jual
                sSQL = " UPDATE     tb_trs_dobill_umum3 " _
                     & " SET        fs_kd_po = '" & xKodePO & "', " _
                     & "            fs_kd_do = '" & xKodeDO & "',  " _
                     & "            fn_hpp = '" & xNilaiHPP & "'  " _
                     & " WHERE      fs_kd_trs = '" & xKodeTrs & "' " _
                     & "        AND fs_kd_barang = '" & oRS("fs_kd_barang").Value & "' " _
                     & "        AND fd_tgl_ed = '" & oRS("fd_tgl_ed").Value & "' "
                oConn.Execute sSQL
                oRS.MoveNext
            Loop
        End If
        oRS.Close
        Set oRS = Nothing
    End If
    
    GenStokDOBillUmumSerah = xLanjut
End Function

'--IN
Private Function GenStokDOBillUmumSerahVoid(ByVal xKodeTrs As String, _
                                            ByVal xTglVoid As String, _
                                            ByVal xJamVoid As String, _
                                            ByVal xVoidDelete As Boolean) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_hpp, fn_stok_in, fn_stok_out, " _
         & "            fs_kd_po, fs_kd_do, fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi, " _
         & "            fd_tgl_ed, fs_no_batch, fs_kd_satuan " _
         & " FROM       tb_buku " _
         & " WHERE      fs_kd_mutasi = '" & xKodeTrs & "' " _
         & "        AND fs_kd_jenis_mutasi = '" & MUTASI_SERAH & "' "
         
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Do While Not oRS.EOF
            AddStok oRS("fs_kd_barang").Value, _
                    oRS("fs_kd_layanan").Value, _
                    oRS("fn_stok_out").Value, _
                    oRS("fs_kd_satuan").Value, _
                    oRS("fn_hpp").Value, _
                    My.TglDMY(oRS("fd_tgl_ed").Value), _
                    oRS("fs_no_batch").Value, _
                    MUTASI_SERAH_VOID, _
                    xKodeTrs, _
                    xTglVoid, _
                    xJamVoid, _
                    oRS("fs_kd_po").Value, _
                    oRS("fs_kd_do").Value, _
                    xVoidDelete
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokDOBillUmumSerahVoid = xRetVal
End Function


Private Function GenStokDOBillTipe(ByVal xKodeTrs As String) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_qty, bb.fs_kd_satuan, fd_tgl_ed, fs_no_batch ," _
         & "            fd_tgl_trs, fs_jam_trs " _
         & " FROM       tb_trs_dobill_tipe aa" _
         & " INNER JOIN tb_trs_dobill_tipe3 bb ON aa.fs_kd_trs = bb.fs_kd_trs " _
         & " WHERE      aa.fs_kd_trs = '" & xKodeTrs & "' "
    
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Dim oBrg As clbBrg: Set oBrg = New clbBrg
        Dim xQty As Double
        Do While Not oRS.EOF
            Dim xKodePO As String
            Dim xKodeDO As String
            Dim xNilaiHPP As Double
            xQty = oBrg(oRS("fs_kd_barang").Value).ConvToTerkecil(oRS("fs_kd_satuan").Value, oRS("fn_qty").Value)
            RemoveStok oRS("fs_kd_barang").Value, _
                       oRS("fs_kd_layanan").Value, _
                       xQty, _
                       oBrg.SatuanTerkecil, _
                       xNilaiHPP, _
                       My.TglDMY(oRS("fd_tgl_ed").Value), _
                       oRS("fs_no_batch").Value, _
                       MUTASI_JUAL_TIPE, _
                       xKodeTrs, _
                       My.TglDMY(oRS("fd_tgl_trs").Value), _
                       oRS("fs_jam_trs").Value, _
                       xKodePO, _
                       xKodeDO, _
                       False
                       
            '---update KODE PO dan KODE DO di front office
            '   untuk kepentingan retur jual
            sSQL = " UPDATE     tb_trs_dobill_tipe3 " _
                 & " SET        fs_kd_po = '" & xKodePO & "', " _
                 & "            fs_kd_do = '" & xKodeDO & "',  " _
                 & "            fn_hpp = '" & xNilaiHPP & "'  " _
                 & " WHERE      fs_kd_trs = '" & xKodeTrs & "' " _
                 & "        AND fs_kd_barang = '" & oRS("fs_kd_barang").Value & "' " _
                 & "        AND fd_tgl_ed = '" & oRS("fd_tgl_ed").Value & "' "
            oConn.Execute sSQL
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokDOBillTipe = xRetVal
End Function

'--IN
Private Function GenStokDOBillTipeVoid(ByVal xKodeTrs As String, _
                                  ByVal xTglVoid As String, _
                                  ByVal xJamVoid As String, _
                                  ByVal xVoidDelete As Boolean) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_hpp, fn_stok_in, fn_stok_out, " _
         & "            fs_kd_po, fs_kd_do, fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi, fs_kd_satuan " _
         & "            fd_tgl_ed, fs_no_batch " _
         & " FROM       tb_buku " _
         & " WHERE      fs_kd_mutasi = '" & xKodeTrs & "' " _
         & "        AND fs_kd_jenis_mutasi = '" & MUTASI_JUAL_TIPE & "' "
         
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Do While Not oRS.EOF
            AddStok oRS("fs_kd_barang").Value, _
                    oRS("fs_kd_layanan").Value, _
                    oRS("fn_stok_out").Value, _
                    oRS("fs_kd_satuan").Value, _
                    oRS("fn_hpp").Value, _
                    My.TglDMY(oRS("fd_tgl_ed").Value), _
                    oRS("fs_no_batch").Value, _
                    MUTASI_JUAL_UMUM_VOID, _
                    xKodeTrs, _
                    xTglVoid, _
                    xJamVoid, _
                    oRS("fs_kd_po").Value, _
                    oRS("fs_kd_do").Value, _
                    xVoidDelete
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokDOBillTipeVoid = xRetVal
End Function

'---IN
Private Function GenStokReturJualUmum(ByVal xKodeTrs As String) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    Dim sSQL As String
    
    '---<jude_was_here::2008-10-06::02:09 # ~ ^>
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_qty_retur, fs_kd_satuan, fd_tgl_ed, fs_no_batch ," _
         & "            fd_tgl_trs, fs_jam_trs, fs_kd_dobill_umum, fn_harga_retur, fs_kd_po, fn_hpp " _
         & " FROM       tb_trs_rjual_umum aa " _
         & " INNER JOIN tb_trs_rjual_umum2 bb ON aa.fs_kd_trs = bb.fs_kd_trs" _
         & " WHERE      aa.fs_kd_trs = '" & xKodeTrs & "' "
    
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    
    If Not oRS.BOF Then
        Dim oBrg As clbBrg: Set oBrg = New clbBrg
        
        Do While Not oRS.EOF
            Dim xQty As Double
            Dim xHPP As Double
            '---<jude_was_here::2008-10-06::02:08 # ~ ^>
            '   nilai persediaan ambil dari hpp retur, bukan harga retur
            xQty = oBrg(oRS("fs_kd_barang").Value).ConvToTerkecil(oRS("fs_kd_satuan").Value, oRS("fn_qty_retur").Value)
            xHPP = oRS("fn_hpp").Value / (xQty / oRS("fn_qty_retur").Value)
            
            Dim xSisa As Double
            xSisa = xQty
            
            sSQL = " SELECT fn_qty, fd_tgl_ed, fs_no_batch, fs_kd_do, fs_kd_po, fn_hpp " _
                 & " FROM   tb_trs_dobill_umum3 " _
                 & " WHERE  fs_kd_barang = '" & oRS("fs_kd_barang").Value & "' " _
                 & "    AND fs_kd_trs = '" & oRS("fs_kd_dobill_umum").Value & "' " _
                 & " ORDER BY fn_no_urut "
            Dim oRS1 As adodb.Recordset: Set oRS1 = New adodb.Recordset
            oRS1.Open sSQL, oConn
            If Not oRS1.BOF Then
                Do While (Not oRS1.EOF) And (xSisa <> 0)
                    Dim xStok As Double
                    If xSisa > oRS1("fn_qty").Value Then
                        xStok = oRS1("fn_qty").Value
                        AddStok oRS("fs_kd_barang").Value, _
                                oRS("fs_kd_layanan").Value, _
                                xStok, _
                                oBrg.SatuanTerkecil, _
                                oRS1("fn_hpp").Value, _
                                My.TglDMY(oRS1("fd_tgl_ed").Value), _
                                oRS1("fs_no_batch").Value, _
                                MUTASI_RETUR_JUAL_UMUM, _
                                xKodeTrs, _
                                My.TglDMY(oRS("fd_tgl_trs").Value), _
                                oRS("fs_jam_trs").Value, _
                                oRS1("fs_kd_po").Value, _
                                oRS1("fs_kd_do").Value, _
                                False
                    Else
                        xStok = xSisa
                        AddStok oRS("fs_kd_barang").Value, _
                                oRS("fs_kd_layanan").Value, _
                                xStok, _
                                oBrg.SatuanTerkecil, _
                                oRS1("fn_hpp").Value, _
                                My.TglDMY(oRS1("fd_tgl_ed").Value), _
                                oRS1("fs_no_batch").Value, _
                                MUTASI_RETUR_JUAL_UMUM, _
                                xKodeTrs, _
                                My.TglDMY(oRS("fd_tgl_trs").Value), _
                                oRS("fs_jam_trs").Value, _
                                oRS1("fs_kd_po").Value, _
                                oRS1("fs_kd_do").Value, _
                                False
                    End If
                    xSisa = xSisa - xStok
                    oRS1.MoveNext
                Loop
            End If
            oRS1.Close
            
            'AddStok oRS("fs_kd_barang").Value, _
                    oRS("fs_kd_layanan").Value, _
                    xQty, _
                    oBrg.SatuanTerkecil, _
                    xHPP, _
                    My.TglDMY(oRS("fd_tgl_ed").Value), _
                    oRS("fs_no_batch").Value, _
                    MUTASI_RETUR_JUAL_UMUM, _
                    xKodeTrs, _
                    My.TglDMY(oRS("fd_tgl_trs").Value), _
                    oRS("fs_jam_trs").Value, _
                    oRS("fs_kd_po").Value, _
                    xKodeTrs, _
                    False
            oRS.MoveNext
        Loop
        Set oBrg = Nothing
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokReturJualUmum = xRetVal
End Function

Private Function GenStokReturJualUmumVoid(ByVal xKodeTrs As String, _
                                     ByVal xTglVoid As String, _
                                     ByVal xJamVoid As String, _
                                     ByVal xVoidDelete As Boolean) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_hpp, fn_stok_in, fn_stok_out, " _
         & "            fs_kd_po, fs_kd_do, fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi, " _
         & "            fd_tgl_ed, fs_no_batch, fs_kd_satuan " _
         & " FROM       tb_buku " _
         & " WHERE      fs_kd_mutasi = '" & xKodeTrs & "' " _
         & "        AND fs_kd_jenis_mutasi = '" & MUTASI_RETUR_JUAL_UMUM & "' "
         
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Do While Not oRS.EOF
            Dim xKodePO As String
            Dim xKodeDO As String
            Dim xNilaiHPP As Double
            RemoveStok oRS("fs_kd_barang").Value, _
                       oRS("fs_kd_layanan").Value, _
                       oRS("fn_stok_in").Value, _
                       oRS("fs_kd_satuan").Value, _
                       xNilaiHPP, _
                       My.TglDMY(oRS("fd_tgl_ed").Value), _
                       oRS("fs_no_batch").Value, _
                       MUTASI_RETUR_JUAL_UMUM_VOID, _
                       xKodeTrs, _
                       xTglVoid, _
                       xJamVoid, _
                       xKodePO, _
                       xKodeDO, _
                       xVoidDelete, _
                       xKodeTrs

            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokReturJualUmumVoid = xRetVal
End Function

'---IN
Private Function GenStokReturJualTipe(ByVal xKodeTrs As String) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    Dim sSQL As String
    
    '---<jude_was_here::2008-10-06::02:10 # ~ ^>
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_qty_retur, fs_kd_satuan, fd_tgl_ed, fs_no_batch ," _
         & "            fd_tgl_trs, fs_jam_trs, fs_kd_dobill_tipe, fn_harga_retur, fs_kd_po, fn_hpp " _
         & " FROM       tb_trs_rjual_tipe aa " _
         & " INNER JOIN tb_trs_rjual_tipe2 bb ON aa.fs_kd_trs = bb.fs_kd_trs" _
         & " WHERE      aa.fs_kd_trs = '" & xKodeTrs & "' "
    
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    
    If Not oRS.BOF Then
        Dim oBrg As clbBrg: Set oBrg = New clbBrg
        
        Do While Not oRS.EOF
            Dim xQty As Double
            Dim xHPP As Double
            
            '---<jude_was_here::2008-10-06::02:11 # ~ ^>
            '   nilai persediaan ambil dari hpp retur, bukan harga retur
            xQty = oBrg(oRS("fs_kd_barang").Value).ConvToTerkecil(oRS("fs_kd_satuan").Value, oRS("fn_qty_retur").Value)
            xHPP = oRS("fn_hpp").Value / (xQty / oRS("fn_qty_retur").Value)
            
            AddStok oRS("fs_kd_barang").Value, _
                    oRS("fs_kd_layanan").Value, _
                    xQty, _
                    oBrg.SatuanTerkecil, _
                    xHPP, _
                    My.TglDMY(oRS("fd_tgl_ed").Value), _
                    oRS("fs_no_batch").Value, _
                    MUTASI_RETUR_JUAL_TIPE, _
                    xKodeTrs, _
                    My.TglDMY(oRS("fd_tgl_trs").Value), _
                    oRS("fs_jam_trs").Value, _
                    oRS("fs_kd_po").Value, _
                    xKodeTrs, _
                    False
            oRS.MoveNext
        Loop
        Set oBrg = Nothing
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokReturJualTipe = xRetVal
End Function
Private Function GenStokReturJualTipeVoid(ByVal xKodeTrs As String, _
                                     ByVal xTglVoid As String, _
                                     ByVal xJamVoid As String, _
                                     ByVal xVoidDelete As Boolean) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_hpp, fn_stok_in, fn_stok_out, " _
         & "            fs_kd_po, fs_kd_do, fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi, " _
         & "            fd_tgl_ed, fs_no_batch, fs_kd_satuan " _
         & " FROM       tb_buku " _
         & " WHERE      fs_kd_mutasi = '" & xKodeTrs & "' " _
         & "        AND fs_kd_jenis_mutasi = '" & MUTASI_RETUR_JUAL_TIPE & "' "
         
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Do While Not oRS.EOF
            Dim xKodePO As String
            Dim xKodeDO As String
            Dim xNilaiHPP As Double
            RemoveStok oRS("fs_kd_barang").Value, _
                       oRS("fs_kd_layanan").Value, _
                       oRS("fn_stok_in").Value, _
                       oRS("fs_kd_satuan").Value, _
                       xNilaiHPP, _
                       My.TglDMY(oRS("fd_tgl_ed").Value), _
                       oRS("fs_no_batch").Value, _
                       MUTASI_RETUR_JUAL_TIPE_VOID, _
                       xKodeTrs, _
                       xTglVoid, _
                       xJamVoid, _
                       xKodePO, _
                       xKodeDO, _
                       xVoidDelete, _
                       xKodeTrs

            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokReturJualTipeVoid = xRetVal
End Function

'---OUT n IN
Private Function GenStokMutasi(ByVal xKodeTrs As String) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    '---mutasi keluar
    '   sumber data = front office
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan_asal, fn_qty, fs_kd_satuan, fd_tgl_ed, fs_no_batch ," _
         & "            fd_tgl_trs, fs_jam_trs, fs_kd_layanan_tujuan " _
         & " FROM       tb_trs_mutasi aa " _
         & " INNER JOIN tb_trs_mutasi2 bb ON aa.fs_kd_trs = bb.fs_kd_trs " _
         & " WHERE      aa.fs_kd_trs = '" & xKodeTrs & "' "
    
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Dim xKodeLayananAsal As String
        Dim xKodeLayananTujuan As String
        Dim xTglTrs As String
        Dim xJamTrs As String
        xKodeLayananAsal = oRS("fs_kd_layanan_asal").Value
        xKodeLayananTujuan = oRS("fs_kd_layanan_tujuan").Value
        xTglTrs = My.TglDMY(oRS("fd_tgl_trs").Value)
        xJamTrs = oRS("fs_jam_trs").Value
        
        Dim oBrg As clbBrg: Set oBrg = New clbBrg
        Dim xQty As Double
        Do While Not oRS.EOF
            Dim xKodePO As String
            Dim xKodeDO As String
            Dim xNilaiHPP As Double
            xQty = oBrg(oRS("fs_kd_barang").Value).ConvToTerkecil(oRS("fs_kd_satuan").Value, oRS("fn_qty").Value)
            RemoveStok oRS("fs_kd_barang").Value, _
                       xKodeLayananAsal, _
                       xQty, _
                       oBrg.SatuanTerkecil, _
                       xNilaiHPP, _
                       My.TglDMY(oRS("fd_tgl_ed").Value), _
                       oRS("fs_no_batch").Value, _
                       MUTASI_MUTASI_OUT, _
                       xKodeTrs, _
                       My.TglDMY(oRS("fd_tgl_trs").Value), _
                       oRS("fs_jam_trs").Value, _
                       xKodePO, _
                       xKodeDO, _
                       False
            '---update kode HPP-nya
            'sSQL = " UPDATE     tb_trs_mutasi2 " _
                 & " SET        fn_hpp = '" & xNilaiHPP & "'  " _
                 & " WHERE      fs_kd_trs = '" & xKodeTrs & "' " _
                 & "        AND fs_kd_barang = '" & oRS("fs_kd_barang").Value & "' " _
                 & "        AND fd_tgl_ed = '" & oRS("fd_tgl_ed").Value & "' "
            oConn.Execute sSQL
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    
    '---mutasi masuk
    '   sumber data = tb_buku
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_hpp, fn_stok_in, fn_stok_out, " _
         & "            fs_kd_po, fs_kd_do, fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi, " _
         & "            fd_tgl_ed, fs_no_batch, fs_kd_satuan " _
         & " FROM       tb_buku " _
         & " WHERE      fs_kd_mutasi = '" & xKodeTrs & "' " _
         & "        AND fs_kd_jenis_mutasi = '" & MUTASI_MUTASI_OUT & "' "
    
    Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Do While Not oRS.EOF
            AddStok oRS("fs_kd_barang").Value, _
                    xKodeLayananTujuan, _
                    oRS("fn_stok_out").Value, _
                    oRS("fs_kd_satuan").Value, _
                    oRS("fn_hpp").Value, _
                    My.TglDMY(oRS("fd_tgl_ed").Value), _
                    oRS("fs_no_batch").Value, _
                    MUTASI_MUTASI_IN, _
                    xKodeTrs, _
                    xTglTrs, _
                    xJamTrs, _
                    oRS("fs_kd_po").Value, _
                    oRS("fs_kd_do").Value, _
                    False
                    
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokMutasi = xRetVal
End Function

Private Function GenStokMutasiVoid(ByVal xKodeTrs As String, _
                              ByVal xTglVoid As String, _
                              ByVal xJamVoid As String, _
                              ByVal xVoidDelete As Boolean) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    '---void mutasi masuk
    '   sumber data = tb_buku
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_hpp, fn_stok_in, fn_stok_out, " _
         & "            fs_kd_po, fs_kd_do, fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi, " _
         & "            fd_tgl_ed, fs_no_batch, fs_kd_satuan " _
         & " FROM       tb_buku " _
         & " WHERE      fs_kd_mutasi = '" & xKodeTrs & "' " _
         & "        AND fs_kd_jenis_mutasi = '" & MUTASI_MUTASI_IN & "' "
    
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Dim oBrg As clbBrg: Set oBrg = New clbBrg
        Dim xQty As Double
        Do While Not oRS.EOF
            Dim xKodePO As String
            Dim xKodeDO As String
            Dim xNilaiHPP As Double
            
            RemoveStok oRS("fs_kd_barang").Value, _
                       oRS("fs_kd_layanan").Value, _
                       oRS("fn_stok_in").Value, _
                       oRS("fs_kd_satuan").Value, _
                       xNilaiHPP, _
                       My.TglDMY(oRS("fd_tgl_ed").Value), _
                       oRS("fs_no_batch").Value, _
                       MUTASI_MUTASI_VOID_IN, _
                       xKodeTrs, _
                       xTglVoid, _
                       xJamVoid, _
                       xKodePO, _
                       xKodeDO, _
                       xVoidDelete, xKodeTrs
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    
    '---void mutasi keluar
    '   sumber data = tb_buku
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_hpp, fn_stok_in, fn_stok_out, " _
         & "            fs_kd_po, fs_kd_do, fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi, " _
         & "            fd_tgl_ed, fs_no_batch, fs_kd_satuan " _
         & " FROM       tb_buku " _
         & " WHERE      fs_kd_mutasi = '" & xKodeTrs & "' " _
         & "        AND fs_kd_jenis_mutasi = '" & MUTASI_MUTASI_OUT & "' "
    
    Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Set oBrg = New clbBrg
        Do While Not oRS.EOF
            AddStok oRS("fs_kd_barang").Value, _
                    oRS("fs_kd_layanan").Value, _
                    oRS("fn_stok_out").Value, _
                    oRS("fs_kd_satuan").Value, _
                    oRS("fn_hpp").Value, _
                    My.TglDMY(oRS("fd_tgl_ed").Value), _
                    oRS("fs_no_batch").Value, _
                    MUTASI_MUTASI_VOID_OUT, _
                    xKodeTrs, _
                    xTglVoid, _
                    xJamVoid, _
                    oRS("fs_kd_po").Value, _
                    oRS("fs_kd_do").Value, _
                    xVoidDelete
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokMutasiVoid = xRetVal
End Function

'---OUT n IN
Private Function GenStokDOBillUmumReserved(ByVal xKodeTrs As String) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    '---mutasi keluar
    '   sumber data = front office
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan_asal, fn_qty, fs_kd_satuan, fd_tgl_ed, fs_no_batch ," _
         & "            fd_tgl_trs, fs_jam_trs, fs_kd_layanan_tujuan " _
         & " FROM       tb_trs_mutasi aa " _
         & " INNER JOIN tb_trs_mutasi2 bb ON aa.fs_kd_trs = bb.fs_kd_trs " _
         & " WHERE      aa.fs_kd_trs = '" & xKodeTrs & "' "
    
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Dim xKodeLayananAsal As String
        Dim xKodeLayananTujuan As String
        Dim xTglTrs As String
        Dim xJamTrs As String
        xKodeLayananAsal = oRS("fs_kd_layanan_asal").Value
        xKodeLayananTujuan = oRS("fs_kd_layanan_tujuan").Value
        xTglTrs = My.TglDMY(oRS("fd_tgl_trs").Value)
        xJamTrs = oRS("fs_jam_trs").Value
        
        Dim oBrg As clbBrg: Set oBrg = New clbBrg
        Dim xQty As Double
        Do While Not oRS.EOF
            Dim xKodePO As String
            Dim xKodeDO As String
            Dim xNilaiHPP As Double
            xQty = oBrg(oRS("fs_kd_barang").Value).ConvToTerkecil(oRS("fs_kd_satuan").Value, oRS("fn_qty").Value)
            RemoveStok oRS("fs_kd_barang").Value, _
                       xKodeLayananAsal, _
                       xQty, _
                       oBrg.SatuanTerkecil, _
                       xNilaiHPP, _
                       My.TglDMY(oRS("fd_tgl_ed").Value), _
                       oRS("fs_no_batch").Value, _
                       MUTASI_RESERVED_ORDER_OUT, _
                       xKodeTrs, _
                       My.TglDMY(oRS("fd_tgl_trs").Value), _
                       oRS("fs_jam_trs").Value, _
                       xKodePO, _
                       xKodeDO, _
                       False
            '---update kode HPP-nya
            sSQL = " UPDATE     tb_trs_mutasi2 " _
                 & " SET        fn_hpp = '" & xNilaiHPP & "'  " _
                 & " WHERE      fs_kd_trs = '" & xKodeTrs & "' " _
                 & "        AND fs_kd_barang = '" & oRS("fs_kd_barang").Value & "' " _
                 & "        AND fd_tgl_ed = '" & oRS("fd_tgl_ed").Value & "' "
            oConn.Execute sSQL
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    
    '---mutasi masuk
    '   sumber data = tb_buku
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_hpp, fn_stok_in, fn_stok_out, " _
         & "            fs_kd_po, fs_kd_do, fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi, " _
         & "            fd_tgl_ed, fs_no_batch, fs_kd_satuan " _
         & " FROM       tb_buku " _
         & " WHERE      fs_kd_mutasi = '" & xKodeTrs & "' " _
         & "        AND fs_kd_jenis_mutasi = '" & MUTASI_RESERVED_ORDER_OUT & "' "
    
    Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Do While Not oRS.EOF
            AddStok oRS("fs_kd_barang").Value, _
                    xKodeLayananTujuan, _
                    oRS("fn_stok_out").Value, _
                    oRS("fs_kd_satuan").Value, _
                    oRS("fn_hpp").Value, _
                    My.TglDMY(oRS("fd_tgl_ed").Value), _
                    oRS("fs_no_batch").Value, _
                    MUTASI_RESERVED_ORDER_IN, _
                    xKodeTrs, _
                    xTglTrs, _
                    xJamTrs, _
                    oRS("fs_kd_po").Value, _
                    oRS("fs_kd_do").Value, _
                    False
                    
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokDOBillUmumReserved = xRetVal
End Function

Private Function GenStokDOBillUmumReservedVoid(ByVal xKodeTrs As String, _
                                               ByVal xTglVoid As String, _
                                               ByVal xJamVoid As String, _
                                               ByVal xVoidDelete As Boolean) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    '---void mutasi masuk
    '   sumber data = tb_buku
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_hpp, fn_stok_in, fn_stok_out, " _
         & "            fs_kd_po, fs_kd_do, fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi, " _
         & "            fd_tgl_ed, fs_no_batch, fs_kd_satuan " _
         & " FROM       tb_buku " _
         & " WHERE      fs_kd_mutasi = '" & xKodeTrs & "' " _
         & "        AND fs_kd_jenis_mutasi = '" & MUTASI_RESERVED_ORDER_IN & "' "
    
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Dim oBrg As clbBrg: Set oBrg = New clbBrg
        Dim xQty As Double
        Do While Not oRS.EOF
            Dim xKodePO As String
            Dim xKodeDO As String
            Dim xNilaiHPP As Double
            
            RemoveStok oRS("fs_kd_barang").Value, _
                       oRS("fs_kd_layanan").Value, _
                       oRS("fn_stok_in").Value, _
                       oRS("fs_kd_satuan").Value, _
                       xNilaiHPP, _
                       My.TglDMY(oRS("fd_tgl_ed").Value), _
                       oRS("fs_no_batch").Value, _
                       MUTASI_RESERVED_ORDER_IN_VOID, _
                       xKodeTrs, _
                       xTglVoid, _
                       xJamVoid, _
                       xKodePO, _
                       xKodeDO, _
                       xVoidDelete
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    
    '---void mutasi keluar
    '   sumber data = tb_buku
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_hpp, fn_stok_in, fn_stok_out, " _
         & "            fs_kd_po, fs_kd_do, fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi, " _
         & "            fd_tgl_ed, fs_no_batch, fs_kd_satuan " _
         & " FROM       tb_buku " _
         & " WHERE      fs_kd_mutasi = '" & xKodeTrs & "' " _
         & "        AND fs_kd_jenis_mutasi = '" & MUTASI_RESERVED_ORDER_OUT & "' "
    
    Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Set oBrg = New clbBrg
        Do While Not oRS.EOF
            AddStok oRS("fs_kd_barang").Value, _
                    oRS("fs_kd_layanan").Value, _
                    oRS("fn_stok_out").Value, _
                    oRS("fs_kd_satuan").Value, _
                    oRS("fn_hpp").Value, _
                    My.TglDMY(oRS("fd_tgl_ed").Value), _
                    oRS("fs_no_batch").Value, _
                    MUTASI_RESERVED_ORDER_OUT_VOID, _
                    xKodeTrs, _
                    xTglVoid, _
                    xJamVoid, _
                    oRS("fs_kd_po").Value, _
                    oRS("fs_kd_do").Value, _
                    xVoidDelete
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokDOBillUmumReservedVoid = xRetVal
End Function


'---OUT
Private Function GenStokPakai(ByVal xKodeTrs As String) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_qty, fs_kd_satuan, fd_tgl_ed, fs_no_batch ," _
         & "            fd_tgl_trs, fs_jam_trs " _
         & " FROM       tb_trs_pakai aa" _
         & " INNER JOIN tb_trs_pakai2 bb ON aa.fs_kd_trs = bb.fs_kd_trs " _
         & " WHERE      aa.fs_kd_trs = '" & xKodeTrs & "' "
    
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Dim oBrg As clbBrg: Set oBrg = New clbBrg
        
        Do While Not oRS.EOF
            Dim xKodePO As String
            Dim xKodeDO As String
            Dim xQty As Double
            Dim xNilaiHPP As Double
            xQty = oBrg(oRS("fs_kd_barang").Value).ConvToTerkecil(oRS("fs_kd_satuan").Value, oRS("fn_qty").Value)
            RemoveStok oRS("fs_kd_barang").Value, _
                       oRS("fs_kd_layanan").Value, _
                       xQty, _
                       oBrg.SatuanTerkecil, _
                       xNilaiHPP, _
                       My.TglDMY(oRS("fd_tgl_ed").Value), _
                       oRS("fs_no_batch").Value, _
                       MUTASI_PAKAI, _
                       xKodeTrs, _
                       My.TglDMY(oRS("fd_tgl_trs").Value), _
                       oRS("fs_jam_trs").Value, _
                       xKodePO, _
                       xKodeDO, _
                       False
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokPakai = xRetVal
End Function

'---IN
Private Function GenStokPakaiVoid(ByVal xKodeTrs As String, _
                             ByVal xTglVoid As String, _
                             ByVal xJamVoid As String, _
                             ByVal xVoidDelete As Boolean) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    '---void mutasi keluar
    '   sumber data = tb_buku
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_hpp, fn_stok_in, fn_stok_out, " _
         & "            fs_kd_po, fs_kd_do, fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi, " _
         & "            fd_tgl_ed, fs_no_batch, fs_kd_satuan " _
         & " FROM       tb_buku " _
         & " WHERE      fs_kd_mutasi = '" & xKodeTrs & "' " _
         & "        AND fs_kd_jenis_mutasi = '" & MUTASI_PAKAI & "' "
    
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Dim oBrg As clbBrg: Set oBrg = New clbBrg
        Dim xQty As Double
        Do While Not oRS.EOF
            Dim xKodePO As String
            Dim xKodeDO As String
            AddStok oRS("fs_kd_barang").Value, _
                    oRS("fs_kd_layanan").Value, _
                    oRS("fn_stok_out").Value, _
                    oRS("fs_kd_satuan").Value, _
                    oRS("fn_hpp").Value, _
                    My.TglDMY(oRS("fd_tgl_ed").Value), _
                    oRS("fs_no_batch").Value, _
                    MUTASI_PAKAI_VOID, _
                    xKodeTrs, _
                    xTglVoid, _
                    xJamVoid, _
                    xKodePO, _
                    xKodeDO, _
                    xVoidDelete
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokPakaiVoid = xRetVal
End Function

'---OUT
Private Function GenStokMusnah(ByVal xKodeTrs As String) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_qty, fs_kd_satuan, fd_tgl_ed, fs_no_batch ," _
         & "            fd_tgl_trs, fs_jam_trs " _
         & " FROM       tb_trs_musnah aa" _
         & " INNER JOIN tb_trs_musnah2 bb ON aa.fs_kd_trs = bb.fs_kd_trs " _
         & " WHERE      aa.fs_kd_trs = '" & xKodeTrs & "' "
    
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Dim oBrg As clbBrg: Set oBrg = New clbBrg
        
        Do While Not oRS.EOF
            Dim xKodePO As String
            Dim xKodeDO As String
            Dim xQty As Double
            Dim xNilaiHPP As Double
            xQty = oBrg(oRS("fs_kd_barang").Value).ConvToTerkecil(oRS("fs_kd_satuan").Value, oRS("fn_qty").Value)
            RemoveStok oRS("fs_kd_barang").Value, _
                       oRS("fs_kd_layanan").Value, _
                       xQty, _
                       oBrg.SatuanTerkecil, _
                       xNilaiHPP, _
                       My.TglDMY(oRS("fd_tgl_ed").Value), _
                       oRS("fs_no_batch").Value, _
                       MUTASI_MUSNAH, _
                       xKodeTrs, _
                       My.TglDMY(oRS("fd_tgl_trs").Value), _
                       oRS("fs_jam_trs").Value, _
                       xKodePO, _
                       xKodeDO, _
                       False
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokMusnah = xRetVal
End Function

'---IN
Private Function GenStokMusnahVoid(ByVal xKodeTrs As String, _
                              ByVal xTglVoid As String, _
                              ByVal xJamVoid As String, _
                              ByVal xVoidDelete As Boolean) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    '---void mutasi keluar
    '   sumber data = tb_buku
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_hpp, fn_stok_in, fn_stok_out, " _
         & "            fs_kd_po, fs_kd_do, fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi, " _
         & "            fd_tgl_ed, fs_no_batch, fs_kd_satuan " _
         & " FROM       tb_buku " _
         & " WHERE      fs_kd_mutasi = '" & xKodeTrs & "' " _
         & "        AND fs_kd_jenis_mutasi = '" & MUTASI_MUSNAH & "' "
    
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Dim oBrg As clbBrg: Set oBrg = New clbBrg
        Dim xQty As Double
        Do While Not oRS.EOF
            Dim xKodePO As String
            Dim xKodeDO As String
            AddStok oRS("fs_kd_barang").Value, _
                    oRS("fs_kd_layanan").Value, _
                    oRS("fn_stok_out").Value, _
                    oRS("fs_kd_satuan").Value, _
                    oRS("fn_hpp").Value, _
                    My.TglDMY(oRS("fd_tgl_ed").Value), _
                    oRS("fs_no_batch").Value, _
                    MUTASI_MUSNAH_VOID, _
                    xKodeTrs, _
                    xTglVoid, _
                    xJamVoid, _
                    xKodePO, _
                    xKodeDO, _
                    xVoidDelete
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokMusnahVoid = xRetVal
End Function

Private Function GenStokAdjust(ByVal xKodeTrs As String) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    '---adjust min
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_qty_adjust, fs_kd_satuan, fd_tgl_ed, fs_no_batch ," _
         & "            fd_tgl_trs, fs_jam_trs " _
         & " FROM       tb_trs_adjustment aa" _
         & " INNER JOIN tb_trs_adjustment2 bb ON aa.fs_kd_trs = bb.fs_kd_trs " _
         & " WHERE      aa.fs_kd_trs = '" & xKodeTrs & "' " _
         & "        AND fn_qty_adjust < 0 "
    
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Dim oBrg As clbBrg: Set oBrg = New clbBrg
        
        Do While Not oRS.EOF
            Dim xKodePO As String
            Dim xKodeDO As String
            Dim xQty As Double
            Dim xNilaiHPP As Double
            xQty = oBrg(oRS("fs_kd_barang").Value).ConvToTerkecil(oRS("fs_kd_satuan").Value, -(oRS("fn_qty_adjust").Value))
            RemoveStok oRS("fs_kd_barang").Value, _
                       oRS("fs_kd_layanan").Value, _
                       xQty, _
                       oBrg.SatuanTerkecil, _
                       xNilaiHPP, _
                       My.TglDMY(oRS("fd_tgl_ed").Value), _
                       oRS("fs_no_batch").Value, _
                       MUTASI_ADJUSTMENT_MIN, _
                       xKodeTrs, _
                       My.TglDMY(oRS("fd_tgl_trs").Value), _
                       oRS("fs_jam_trs").Value, _
                       xKodePO, _
                       xKodeDO, _
                       False
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing

    '---adjust plus
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_qty_adjust, fs_kd_satuan, fd_tgl_ed, fs_no_batch ," _
         & "            fd_tgl_trs, fs_jam_trs, fn_nilai " _
         & " FROM       tb_trs_adjustment aa" _
         & " INNER JOIN tb_trs_adjustment2 bb ON aa.fs_kd_trs = bb.fs_kd_trs " _
         & " WHERE      aa.fs_kd_trs = '" & xKodeTrs & "' " _
         & "        AND fn_qty_adjust > 0 "
    
    Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Set oBrg = New clbBrg
        
        Do While Not oRS.EOF
            Dim xHPP As Double
            
            xQty = oBrg(oRS("fs_kd_barang").Value).ConvToTerkecil(oRS("fs_kd_satuan").Value, oRS("fn_qty_adjust").Value)
            If xQty <> 0 Then
                xHPP = oRS("fn_nilai").Value / (xQty / oRS("fn_qty_adjust").Value)
            Else
                xHPP = 0
            End If
            AddStok oRS("fs_kd_barang").Value, _
                    oRS("fs_kd_layanan").Value, _
                    xQty, _
                    oBrg.SatuanTerkecil, _
                    xHPP, _
                    My.TglDMY(oRS("fd_tgl_ed").Value), _
                    oRS("fs_no_batch").Value, _
                    MUTASI_ADJUSTMENT_PLUS, _
                    xKodeTrs, _
                    My.TglDMY(oRS("fd_tgl_trs").Value), _
                    oRS("fs_jam_trs").Value, _
                    xKodeTrs, _
                    xKodeTrs, _
                    False
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokAdjust = xRetVal
End Function
Private Function GenStokAdjustVoid(ByVal xKodeTrs As String, _
                              ByVal xTglVoid As String, _
                              ByVal xJamVoid As String, _
                              ByVal xVoidDelete As Boolean) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    '---void adjustment min
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_hpp, fn_stok_in, fn_stok_out, " _
         & "            fs_kd_po, fs_kd_do, fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi, " _
         & "            fd_tgl_ed, fs_no_batch, fs_kd_satuan " _
         & " FROM       tb_buku " _
         & " WHERE      fs_kd_mutasi = '" & xKodeTrs & "' " _
         & "        AND fs_kd_jenis_mutasi = '" & MUTASI_ADJUSTMENT_MIN & "' "
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Dim oBrg As clbBrg: Set oBrg = New clbBrg
        Dim xQty As Double
        Do While Not oRS.EOF
            Dim xKodePO As String
            Dim xKodeDO As String
            AddStok oRS("fs_kd_barang").Value, _
                    oRS("fs_kd_layanan").Value, _
                    oRS("fn_stok_out").Value, _
                    oRS("fs_kd_satuan").Value, _
                    oRS("fn_hpp").Value, _
                    My.TglDMY(oRS("fd_tgl_ed").Value), _
                    oRS("fs_no_batch").Value, _
                    MUTASI_ADJUSTMENT_VOID_MIN, _
                    xKodeTrs, _
                    xTglVoid, _
                    xJamVoid, _
                    oRS("fs_kd_po").Value, _
                    oRS("fs_kd_do").Value, _
                    xVoidDelete
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    
    '---void adjustment plus
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_hpp, fn_stok_in, fn_stok_out, " _
         & "            fs_kd_po, fs_kd_do, fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi, " _
         & "            fd_tgl_ed, fs_no_batch, fs_kd_satuan " _
         & " FROM       tb_buku " _
         & " WHERE      fs_kd_mutasi = '" & xKodeTrs & "' " _
         & "        AND fs_kd_jenis_mutasi = '" & MUTASI_ADJUSTMENT_PLUS & "' "
    Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Set oBrg = New clbBrg
        Do While Not oRS.EOF
            Dim xNilaiHPP As Double
            RemoveStok oRS("fs_kd_barang").Value, _
                       oRS("fs_kd_layanan").Value, _
                       oRS("fn_stok_in").Value, _
                       oRS("fs_kd_satuan").Value, _
                       xNilaiHPP, _
                       My.TglDMY(oRS("fd_tgl_ed").Value), _
                       oRS("fs_no_batch").Value, _
                       MUTASI_ADJUSTMENT_VOID_PLUS, _
                       xKodeTrs, _
                       xTglVoid, _
                       xJamVoid, _
                       xKodePO, _
                       xKodeDO, _
                       xVoidDelete
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokAdjustVoid = xRetVal
End Function


Private Function GenStokRepack(ByVal xKodeTrs As String) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    '---repack material
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_trs3, fs_kd_barang, fs_kd_layanan, fn_qty, fs_kd_satuan, fd_tgl_ed, " _
         & "            fs_no_batch, fd_tgl_trs, fs_jam_trs " _
         & " FROM       tb_trs_repack2_material2 aa" _
         & " INNER JOIN tb_trs_repack bb ON aa.fs_kd_trs = bb.fs_kd_trs " _
         & " WHERE      aa.fs_kd_trs = '" & xKodeTrs & "' "
    
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Dim oBrg As clbBrg: Set oBrg = New clbBrg
    
        Do While Not oRS.EOF
            Dim xKodePO As String
            Dim xKodeDO As String
            Dim xQty As Double
            Dim xNilaiHPP As Double
            xQty = oBrg(oRS("fs_kd_barang").Value).ConvToTerkecil(oRS("fs_kd_satuan").Value, oRS("fn_qty").Value)
            RemoveStok oRS("fs_kd_barang").Value, _
                       oRS("fs_kd_layanan").Value, _
                       xQty, _
                       oBrg.SatuanTerkecil, _
                       xNilaiHPP, _
                       My.TglDMY(oRS("fd_tgl_ed").Value), _
                       oRS("fs_no_batch").Value, _
                       MUTASI_REPACK_ASAL, _
                       xKodeTrs, _
                       My.TglDMY(oRS("fd_tgl_trs").Value), _
                       oRS("fs_jam_trs").Value, _
                       xKodePO, _
                       xKodeDO, _
                       False
    
            '---update fn_hpp di tb_trs_repack2_material2
            sSQL = " UPDATE     tb_trs_repack2_material2 " _
                 & " SET        fs_kd_po = '" & xKodePO & "', " _
                 & "            fs_kd_do = '" & xKodeDO & "',  " _
                 & "            fn_hpp = '" & xNilaiHPP & "'  " _
                 & " WHERE      fs_kd_trs = '" & xKodeTrs & "' " _
                 & "        AND fs_kd_barang = '" & oRS("fs_kd_barang").Value & "' " _
                 & "        AND fd_tgl_ed = '" & oRS("fd_tgl_ed").Value & "' "
            oConn.Execute sSQL
    
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    
    '---repack hasil
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_qty,fn_hpp, fs_kd_satuan, fd_tgl_ed, fs_no_batch, " _
         & "            fn_hpp_prosen, fd_tgl_trs, fs_jam_trs " _
         & " FROM       tb_trs_repack2_hasil aa" _
         & " INNER JOIN tb_trs_repack bb ON aa.fs_kd_trs = bb.fs_kd_trs " _
         & " WHERE      aa.fs_kd_trs = '" & xKodeTrs & "' "
    
    Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Set oBrg = New clbBrg
        Do While Not oRS.EOF
            '--ambil total hpp
            sSQL = " SELECT SUM(fn_hpp * fn_stok_out) fn_total " _
                 & " FROM   tb_buku " _
                 & " WHERE  fs_kd_mutasi = '" & xKodeTrs & "' " _
                 & "    AND fs_kd_jenis_mutasi = '" & MUTASI_REPACK_ASAL & "' "
            Dim oRS1 As adodb.Recordset: Set oRS1 = New adodb.Recordset
            Dim xTotalHpp As Double
            oRS1.Open sSQL, oConn
            If Not oRS1.BOF Then
                xTotalHpp = oRS1("fn_total").Value
            End If
            oRS1.Close
            Set oRS1 = Nothing
            
            Dim xHPP As Double
            xQty = oBrg(oRS("fs_kd_barang").Value).ConvToTerkecil(oRS("fs_kd_satuan").Value, oRS("fn_qty").Value)
            '--konfersi nilai hpp
            'xHPP = oRS("fn_hpp").Value / (xQty / oRS("fn_qty").Value)
            xHPP = (oRS("fn_hpp_prosen").Value / 100 * xTotalHpp) / xQty
    
            AddStok oRS("fs_kd_barang").Value, _
                    oRS("fs_kd_layanan").Value, _
                    xQty, _
                    oBrg.SatuanTerkecil, _
                    xHPP, _
                    My.TglDMY(oRS("fd_tgl_ed").Value), _
                    oRS("fs_no_batch").Value, _
                    MUTASI_REPACK_HASIL, _
                    xKodeTrs, _
                    My.TglDMY(oRS("fd_tgl_trs").Value), _
                    oRS("fs_jam_trs").Value, _
                    xKodeTrs, _
                    xKodeTrs, _
                    False
            oRS.MoveNext
        Loop
        Set oBrg = Nothing
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokRepack = xRetVal
End Function

Private Function GenStokRepackVoid(ByVal xKodeTrs As String, _
                              ByVal xTglVoid As String, _
                              ByVal xJamVoid As String, _
                              ByVal xVoidDelete As Boolean) As Boolean
    Dim xRetVal As Boolean
    xRetVal = True
    
    '---void repack asal
    Dim sSQL As String
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_hpp, fn_stok_in, fn_stok_out, " _
         & "            fs_kd_po, fs_kd_do, fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi, " _
         & "            fd_tgl_ed, fs_no_batch, fs_kd_satuan " _
         & " FROM       tb_buku " _
         & " WHERE      fs_kd_mutasi = '" & xKodeTrs & "' " _
         & "        AND fs_kd_jenis_mutasi = '" & MUTASI_REPACK_ASAL & "' "
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Dim oBrg As clbBrg: Set oBrg = New clbBrg
        Dim xQty As Double
        Do While Not oRS.EOF
            Dim xKodePO As String
            Dim xKodeDO As String
            AddStok oRS("fs_kd_barang").Value, _
                    oRS("fs_kd_layanan").Value, _
                    oRS("fn_stok_out").Value, _
                    oRS("fs_kd_satuan").Value, _
                    oRS("fn_hpp").Value, _
                    My.TglDMY(oRS("fd_tgl_ed").Value), _
                    oRS("fs_no_batch").Value, _
                    MUTASI_REPACK_VOID_ASAL, _
                    xKodeTrs, _
                    xTglVoid, _
                    xJamVoid, _
                    oRS("fs_kd_po").Value, _
                    oRS("fs_kd_do").Value, _
                    xVoidDelete
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    
    '---void repack hasil
    sSQL = " SELECT     fs_kd_barang, fs_kd_layanan, fn_hpp, fn_stok_in, fn_stok_out, " _
         & "            fs_kd_po, fs_kd_do, fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi, " _
         & "            fd_tgl_ed, fs_no_batch, fs_kd_satuan " _
         & " FROM       tb_buku " _
         & " WHERE      fs_kd_mutasi = '" & xKodeTrs & "' " _
         & "        AND fs_kd_jenis_mutasi = '" & MUTASI_REPACK_HASIL & "' "
    Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn
    If Not oRS.BOF Then
        Set oBrg = New clbBrg
        Do While Not oRS.EOF
            Dim xNilaiHPP As Double
            RemoveStok oRS("fs_kd_barang").Value, _
                       oRS("fs_kd_layanan").Value, _
                       oRS("fn_stok_in").Value, _
                       oRS("fs_kd_satuan").Value, _
                       xNilaiHPP, _
                       My.TglDMY(oRS("fd_tgl_ed").Value), _
                       oRS("fs_no_batch").Value, _
                       MUTASI_REPACK_VOID_HASIL, _
                       xKodeTrs, _
                       xTglVoid, _
                       xJamVoid, _
                       xKodePO, _
                       xKodeDO, _
                       xVoidDelete
            oRS.MoveNext
        Loop
    End If
    oRS.Close
    Set oRS = Nothing
    GenStokRepackVoid = xRetVal
End Function

'---procedure2 bantu---'
Private Function GenNewIDBuku() As String
    Dim xIdNum As Long
    xIdNum = MyParam.No.BukuStok
    GenNewIDBuku = KODE_PREFIX_BUKU & My.AddSpace(Trim(Str(xIdNum)), KODE_LENGTH_BUKU - Len(KODE_PREFIX_BUKU), iseeRight, "0")
End Function

Private Function GenNewIDStok() As String
    Dim xIdNum As Long
    xIdNum = MyParam.No.BrgStok
    GenNewIDStok = KODE_PREFIX_STOK & My.AddSpace(Trim(Str(xIdNum)), KODE_LENGTH_STOK - Len(KODE_PREFIX_STOK), iseeRight, "0")
End Function

Private Sub AddStok(ByVal xKodeBarang As String, _
                    ByVal xKodeLayanan As String, _
                    ByVal xQty As Double, _
                    ByVal xKodeSatuan As String, _
                    ByVal xHPP As Double, _
                    ByVal xTglED As String, _
                    ByVal xNoBatch As String, _
                    ByVal xJenisMutasi As String, _
                    ByVal xKodeTrs As String, _
                    ByVal xTglTrs As String, _
                    ByVal xJamTrs As String, _
                    ByVal xKodePO As String, _
                    ByVal xKodeDO As String, _
                    ByVal xVoidDelete As Boolean)
    
    '---jika sudah ada closing stok, maka di-reject transaksinya
    
    '---insert buku
    If Not xVoidDelete Then
        Dim xKodeTrsBuku As String
        xKodeTrsBuku = GenNewIDBuku
        My.SQL.Add "fs_kd_trs", xKodeTrsBuku
        My.SQL.Add "fs_kd_jenis_mutasi", xJenisMutasi
        My.SQL.Add "fs_kd_barang", xKodeBarang
        My.SQL.Add "fs_kd_layanan", xKodeLayanan
        My.SQL.Add "fn_hpp", xHPP
        My.SQL.Add "fn_stok_in", xQty
        My.SQL.Add "fn_stok_out", 0
        My.SQL.Add "fs_kd_po", xKodePO
        My.SQL.Add "fs_kd_do", xKodeDO
        My.SQL.Add "fs_kd_mutasi", xKodeTrs
        My.SQL.Add "fd_tgl_mutasi", My.TglYMD(xTglTrs)
        My.SQL.Add "fs_jam_mutasi", xJamTrs
        My.SQL.Add "fd_tgl_ed", My.TglYMD(xTglED)
        My.SQL.Add "fs_no_batch", xNoBatch
        My.SQL.Add "fs_kd_satuan", xKodeSatuan
        
        Dim sSQL As String
        Dim oSys As clsSystemField: Set oSys = New clsSystemField
        Set oSys = GetSysData
        sSQL = My.SQLGenerator2("tb_buku", oSys, iseeInsert)
        oConn.Execute sSQL
    Else
        sSQL = " DELETE     tb_buku " _
             & " WHERE      fs_kd_barang = '" & xKodeBarang & "' " _
             & "        AND fs_kd_mutasi = '" & xKodeTrs & "' " _
             & "        AND fd_tgl_ed = '" & My.TglYMD(xTglED) & "' "
        oConn.Execute sSQL
    End If
    
    '---insert stok
    Dim xKodeTrsStok As String
    xKodeTrsStok = GenNewIDStok
    My.SQL.Add "fs_kd_trs", xKodeTrsStok
    My.SQL.Add "fs_kd_barang", xKodeBarang
    My.SQL.Add "fs_kd_layanan", xKodeLayanan
    My.SQL.Add "fn_hpp", xHPP
    My.SQL.Add "fn_qty", xQty
    My.SQL.Add "fn_qty_in", xQty
    My.SQL.Add "fs_kd_po", xKodePO
    My.SQL.Add "fs_kd_do", xKodeDO
    My.SQL.Add "fs_kd_mutasi", xKodeTrs
    My.SQL.Add "fd_tgl_mutasi", My.TglYMD(xTglTrs)
    My.SQL.Add "fs_jam_mutasi", xJamTrs
    My.SQL.Add "fd_tgl_ed", My.TglYMD(xTglED)
    My.SQL.Add "fs_no_batch", xNoBatch
    My.SQL.Add "fs_kd_satuan", xKodeSatuan
    
    sSQL = My.SQLGenerator("tb_stok", iseeInsert)
    oConn.Execute sSQL

End Sub

'---remove stok lama, ngga dipake
'   karena pasti 1 row itu 1 record di tb_stok
'   --> ngga juga, ada kemungkinan stok balikan karena void atau retur
'       jadi yang tadinya 1 record, jadi 2 record
Private Sub RemoveStok(ByVal xKodeBarang As String, _
                       ByVal xKodeLayanan As String, _
                       ByVal xQty As Double, _
                       ByVal xKodeSatuan As String, _
                       ByRef xNilaiHPP As Double, _
                       ByVal xTglED As String, _
                       ByVal xNoBatch As String, _
                       ByVal xJenisMutasi As String, _
                       ByVal xKodeTrs As String, _
                       ByVal xTglTrs As String, _
                       ByVal xJamTrs As String, _
                       ByRef xKodePO As String, _
                       ByRef xKodeDO As String, _
                       ByVal xVoidDelete As Boolean, _
                       Optional ByVal xKodeDO2 As String = "")

    '---jude_2009-08-17
    '   tandai jika barang pembelian bebas, karna ada perlakuan khusus di harga jual
    
    Dim sSQL As String
    sSQL = " SELECT         fs_kd_trs, aa.fs_kd_barang, fn_qty, fn_hpp, " & vbCrLf _
         & "                fs_kd_po, fs_kd_do, fd_tgl_ed, fs_no_batch, fn_qty_in, fs_kd_satuan " & vbCrLf _
         & " FROM           tb_stok aa " & vbCrLf _
         & " WHERE          aa.fs_kd_barang = '" & xKodeBarang & "' " & vbCrLf _
         & "    AND         fs_kd_layanan = '" & xKodeLayanan & "' " & vbCrLf _
         & "    AND         fd_tgl_ed = '" & My.TglYMD(xTglED) & "' " & vbCrLf

    '---bali [07-08-2009]
    If Trim$(xKodeDO2) <> "" Then
        '---jude_2010-08-11@HAJ
        If Left(xKodeTrs, 2) = TRS_RETUR_BELI Or xJenisMutasi = MUTASI_DO_VOID Then
            sSQL = sSQL & " AND fs_kd_do = '" & xKodeDO2 & "' "
        Else
            sSQL = sSQL & " AND fs_kd_mutasi = '" & xKodeDO2 & "' "
        End If
    End If
    
    '---balik order by tgl
    '   order FIFO .::. erkoc@Bali 2009-07-30
    sSQL = sSQL _
         & " ORDER BY   fd_tgl_mutasi, fs_jam_mutasi "
    
    Dim oRS As adodb.Recordset: Set oRS = New adodb.Recordset
    oRS.Open sSQL, oConn

    If Not oRS.BOF Then
        Dim xSisa As Double
        xSisa = xQty
        xKodePO = oRS("fs_kd_po").Value
        xKodeDO = oRS("fs_kd_do").Value
        xNilaiHPP = oRS("fn_hpp").Value
        
        Do While (Not oRS.EOF) And (xSisa <> 0)

            '---remove stok
            Dim xStokOut As Double
            If xSisa > oRS("fn_qty").Value Then
                xStokOut = oRS("fn_qty").Value
                sSQL = " DELETE tb_stok " _
                     & " WHERE  fs_kd_trs = '" & oRS("fs_kd_trs").Value & "' "
                oConn.Execute sSQL
            Else
                xStokOut = xSisa
                If oRS("fn_qty").Value - xStokOut <> 0 Then
                    sSQL = " UPDATE tb_stok  " _
                         & " SET    fn_qty = " & oRS("fn_qty").Value - xStokOut _
                         & " WHERE  fs_kd_trs = '" & oRS("fs_kd_trs").Value & "' "
                    oConn.Execute sSQL
                Else
                    sSQL = " DELETE tb_stok  " _
                         & " WHERE  fs_kd_trs = '" & oRS("fs_kd_trs").Value & "' "
                    oConn.Execute sSQL
                End If
            End If

            '---insert buku
            If xVoidDelete Then
                '-- erkoc@Bali 2009-07-30
                '   tambahkan filter layanan
                sSQL = " DELETE     tb_buku " _
                     & " WHERE      fs_kd_mutasi = '" & xKodeTrs & "' " _
                     & "        AND fs_kd_barang = '" & xKodeBarang & "' " _
                     & "        AND fs_kd_layanan = '" & xKodeLayanan & "' " _
                     & "        AND fd_tgl_ed = '" & My.TglYMD(xTglED) & "' "
                oConn.Execute sSQL
                'sSQL = " DELETE     tb_buku " _
                     & " WHERE      fs_kd_mutasi = '" & xKodeTrs & "' " _
                     & "        AND fs_kd_barang = '" & xKodeBarang & "' " _
                     & "        AND fd_tgl_ed = '" & My.TglYMD(xTglED) & "' "
                'oConn.Execute sSQL
            Else
                Dim xKodeTrsBuku As String
                xKodeTrsBuku = GenNewIDBuku
                My.SQL.Add "fs_kd_trs", xKodeTrsBuku
                My.SQL.Add "fs_kd_jenis_mutasi", xJenisMutasi
                My.SQL.Add "fs_kd_barang", xKodeBarang
                My.SQL.Add "fs_kd_layanan", xKodeLayanan
                My.SQL.Add "fn_hpp", oRS("fn_hpp").Value
                My.SQL.Add "fn_stok_in", 0
                My.SQL.Add "fn_stok_out", xStokOut
                My.SQL.Add "fs_kd_po", oRS("fs_kd_po").Value
                My.SQL.Add "fs_kd_do", oRS("fs_kd_do").Value
                My.SQL.Add "fs_kd_mutasi", xKodeTrs
                My.SQL.Add "fd_tgl_mutasi", My.TglYMD(xTglTrs)
                My.SQL.Add "fs_jam_mutasi", xJamTrs
                My.SQL.Add "fd_tgl_ed", oRS("fd_tgl_ed").Value
                My.SQL.Add "fs_no_batch", oRS("fs_no_batch").Value
                My.SQL.Add "fs_kd_satuan", oRS("fs_kd_satuan").Value
                
                sSQL = My.SQLGenerator("tb_buku", iseeInsert)
                oConn.Execute sSQL
            End If

            xSisa = xSisa - xStokOut
            oRS.MoveNext
        Loop
        
        If oRS.EOF And xSisa <> 0 Then
            Dim oBrg As clbBrg: Set oBrg = New clbBrg
            Err.Raise vbError + 1, "RemoveStok()", "Stok " & oBrg(xKodeBarang).Nama & " tidak cukup." & vbCrLf _
                                 & "Remove Stok dibatalkan."
            Set oBrg = Nothing
        End If
    Else
        Set oBrg = New clbBrg
        Err.Raise vbError + 2, "RemoveStok()", "Stok " & oBrg(xKodeBarang).Nama & " tidak ada." & vbCrLf _
                             & "Remove Stok dibatalkan."
        Set oBrg = Nothing
    End If
    oRS.Close
    Set oRS = Nothing
End Sub
