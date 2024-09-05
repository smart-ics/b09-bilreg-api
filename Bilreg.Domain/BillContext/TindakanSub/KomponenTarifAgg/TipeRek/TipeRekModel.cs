﻿namespace Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg.TipeRek
{
    public class TipeRekModel(string tipeRekId, string tipeRekName, bool isNeraca, decimal noUrut, string debetKredit) : ITipeRekKey
    {
        public string TipeRekId { get; protected set; } = tipeRekId;
        public string TipeRekName { get; protected set; } = tipeRekName;
        public bool IsNeraca { get; protected set; } = isNeraca;
        public decimal NoUrut { get; protected set; } = noUrut;
        public string DebetKredit { get; protected set; } = debetKredit;

        public void Set(TipeRekModel model)
        {
            TipeRekId = model.TipeRekId;
            TipeRekName = model.TipeRekName;
            IsNeraca = model.IsNeraca;
            NoUrut = model.NoUrut;
            DebetKredit = model.DebetKredit;

        }

    }

    public interface ITipeRekKey
    {
        public string TipeRekId { get; }

    }

}
