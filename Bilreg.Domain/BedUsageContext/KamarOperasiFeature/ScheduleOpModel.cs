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
    
    #region CREATION
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
    public static ScheduleOpModel Default 
        => new ScheduleOpModel("-", AuditTrailType.Default, 
            OrderOpModel.Default.ToReff(), PasienModel.Default.ToReff(), 
            UrgencyLevelEnum.Elective, 0, 
            new DateTime(3000,1,1), KamarType.Default.ToReff(), 
            RegModel.Default.ToReff(), PpaType.Default.ToReff(), []);
    public static IScheduleOpKey Key(string id) 
        => new ScheduleOpModel(id, AuditTrailType.Default, 
            OrderOpModel.Default.ToReff(), PasienModel.Default.ToReff(), 
            UrgencyLevelEnum.Elective, 0, 
            new DateTime(3000,1,1), KamarType.Default.ToReff(), 
            RegModel.Default.ToReff(), PpaType.Default.ToReff(), []);
    
    public static ScheduleOpModel CreateFromOrder(OrderOpModel orderOp, string userId,
        KamarType kamar, PpaType teamLeader, DateTime tglOp)
    {
        var newId = Ulid.NewUlid().ToString();
        var audit = new AuditTrailType(new AuditInfoType(userId, DateTime.Now),
            AuditInfoType.Default, AuditInfoType.Default);
        var result = new ScheduleOpModel(newId, audit, orderOp.ToReff(),
            orderOp.Pasien, orderOp.UrgencyLevel, orderOp.EstimasiDurasiInMinutes,
            tglOp, kamar.ToReff(), orderOp.Reg, PpaType.Default.ToReff(), []);
        result.AddPpa(teamLeader, userId);
        result.AssignLeader(teamLeader, userId);
        return result;
    }
    
    #endregion
    
    #region PROPERTIES
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
    public void SetSchedule(DateTime tglOp, KamarReff kamarOp, string userId)
    {
        TglOp = tglOp;
        KamarOp = kamarOp;
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void AddPpa(PpaType ppa, string userId)
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
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void RemovePpa(PpaType ppa, string userId)
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
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void AssignLeader(PpaType ppa, string userId)
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
        AuditTrail.Modif(userId, DateTime.Now);
    }
    #endregion
}

public interface IScheduleOpKey
{
    string ScheduleOpId { get; }
}