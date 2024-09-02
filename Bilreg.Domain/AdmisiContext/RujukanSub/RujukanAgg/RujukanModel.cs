using Bilreg.Domain.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.KelasRujukanAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.TipeRujukanAgg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg.RujukanModel;

namespace Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg
{
    public class RujukanModel : IRujukanKey
    {
        // PROPERTIES
        public string RujukanId { get; protected set; }
        public string RujukanName { get; protected set; }
        public string Alamat { get; protected set; }
        public string Alamat2 { get; protected set; }
        public string Kota { get; protected set; }
        public string NoTelp { get; protected set; }
        public bool IsAktif { get; protected set; }
        public string TipeRujukanId { get; protected set; }
        public string TipeRujukanName { get; protected set; }
        public string KelasRujukanId { get; protected set; }
        public string KelasRujukanName { get; protected set; }
        public string CaraMasukDkId { get; protected set; }
        public string CaraMasukDkName { get; protected set; }

        // CONSTRUCTOR
        public RujukanModel(string id, string name)
        {
            RujukanId = id;
            RujukanName = name;
            Alamat = string.Empty;
            Alamat2 = string.Empty;
            Kota = string.Empty;
            NoTelp = string.Empty;
            IsAktif = false;
            TipeRujukanId = string.Empty;
            TipeRujukanName = string.Empty;
            KelasRujukanId = string.Empty;
            KelasRujukanName = string.Empty;
            CaraMasukDkId = string.Empty;
            CaraMasukDkName = string.Empty;
        }

        // FACTORY METHOD
        public static RujukanModel Create(string id, string name) => new RujukanModel(id, name);

        // METHODS
        public void SetName(string rujukanName) => RujukanName = rujukanName;
        public void SetAktif() => IsAktif = true;
        public void UnSetAktif() => IsAktif = false;
        public void SetAlamat(string alamat, string alamat2, string kota)
        {
            Alamat = alamat;
            Alamat2 = alamat2;
            Kota = kota;
        }
        public void SetNoTelp(string notelp) 
        {
            NoTelp = notelp;
        }
        public void SetTipeRujukan(TipeRujukanModel tipeRujukan)
        {
            TipeRujukanId = tipeRujukan.TipeRujukanId;
            TipeRujukanName = tipeRujukan.TipeRujukanName;
        }
        public void SetKelasRujukan(KelasRujukanModel kelasRujukan)
        {
            KelasRujukanId = kelasRujukan.KelasRujukanId;
            KelasRujukanName = kelasRujukan.KelasRujukanName;
        }
        public void SetCaraMasukDk(CaraMasukDkModel caraMasukDk)
        {
            CaraMasukDkId = caraMasukDk.CaraMasukDkId;
            CaraMasukDkName = caraMasukDk.CaraMasukDkName;
        }
    }

    public interface IRujukanKey
    {
        string RujukanId { get; }
    }
}
