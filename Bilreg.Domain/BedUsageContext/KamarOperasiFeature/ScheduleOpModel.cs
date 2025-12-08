using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public class ScheduleOpModel : IScheduleOpKey
{
    private readonly List<ScheduleOpPpaType> _listPpa;

    #region PROPERTIES

    //  identitas
    public ScheduleOpModel(string scheduleOpId, AuditTrailType auditTrail, 
        OrderOpReff orderOp, PasienReff pasien, UrgencyLevelEnum urgencyLevel, int durasi, 
        DateTime tglOp, KamarReff kamarOp, RegReff reg, PpaReff teamLead, 
        IEnumerable<ScheduleOpPpaType> listPpa)
    {
        ScheduleOpId = scheduleOpId;
        AuditTrail = auditTrail;
        OrderOp = orderOp;
        Pasien = pasien;
        UrgencyLevel = urgencyLevel;
        Durasi = durasi;
        TglOp = tglOp;
        KamarOp = kamarOp;
        Reg = reg;
        TeamLead = teamLead;
        _listPpa = listPpa.ToList() ?? [];
    }

    public string ScheduleOpId { get; init; }
    public AuditTrailType AuditTrail { get; init; }
    //  order
    public OrderOpReff OrderOp { get; init;}
    public PasienReff Pasien { get; init;}
    public UrgencyLevelEnum UrgencyLevel { get; init;}
    public int Durasi { get; private set;}
    //  schedule
    public DateTime TglOp { get; private set;}
    public KamarReff KamarOp { get; private set;}
    public RegReff Reg { get; private set; }    
    public PpaReff TeamLead { get; private set;}
    public IEnumerable<ScheduleOpPpaType> ListPpa => _listPpa;
    #endregion

    
    #region BEHAVIOUR
    public void SetSchedule(DateTime tglOp, KamarReff kamarOp)
    {
        TglOp = tglOp;
        KamarOp = kamarOp;
    }

    public void AddPpa(PpaType ppa)
    {
        var profesi = ppa.ListSatTugas.FirstOrDefault()?.SatTugas?.Profesi;
        if (profesi is null)
            throw new ArgumentException("Profesi PPA tidak valid");
        var existing = _listPpa.FirstOrDefault(x => x.Ppa.PpaId == ppa.PpaId);
        if (existing is not null)
            throw new ArgumentException("PPA sudah ada dalam schedule");
        var noUrut = _listPpa.DefaultIfEmpty(ScheduleOpPpaType.Default).Max(x => x.NoUrut);
        noUrut++;
        var newPpaMember = new ScheduleOpPpaType(noUrut, ppa.ToReff(), profesi, GroupSpesialisType.Default);
        _listPpa.Add(newPpaMember);
    }

    public void RemovePpa(PpaType ppa)
    {
        _listPpa.RemoveAll(x => x.Ppa.PpaId == ppa.PpaId);
        var newList = new List<ScheduleOpPpaType>();
        var i = 0;
        foreach (var item in _listPpa)
        {
            i++;
            newList.Add(item with { NoUrut = i });
        }
        _listPpa.Clear();
        _listPpa.AddRange(newList);
    }

    public void AssignLeader(PpaType ppa)
    {
        // harus dokter
        var profesi = ppa.ListSatTugas.FirstOrDefault()?.SatTugas?.Profesi;
        if (profesi is null)
            throw new ArgumentException("Profesi PPA tidak valid");
        if (profesi != ProfesiType.Dokter)
            throw new ArgumentException("PPA harus dokter");
        //  harus terdaftar di listPpa
        var existing = _listPpa.FirstOrDefault(x => x.Ppa.PpaId == ppa.PpaId);
        if (existing is null)
            throw new ArgumentException("PPA harus terdaftar di member");
        TeamLead = ppa.ToReff();
    }
    #endregion
}

public interface IScheduleOpKey
{
    string ScheduleOpId { get; }
}