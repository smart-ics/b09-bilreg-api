using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public class ScheduleOpModel : IScheduleOpKey
{
    #region PROPERTIES
    public string ScheduleOpId { get; }
    public OrderOpReff OrderOp { get; }
    public KamarReff KamarOp { get; }
    public PasienReff Pasien { get; }
    public int Durasi { get; }
    public UrgencyLevelEnum UrgencyLevel { get; }
    public PpaReff TeamLead { get; }
    public IEnumerable<OpTeamMemberType> TeamMember{ get; set; }
    #endregion

    #region BEHAVIOUR
    public void SetJadwal()
    {

    }

    public void AddPpa()
    {

    }

    public void RemovePpa()
    {

    }

    public void AssignLeader(string OrderOpId, string PpaId)
    {

    }
    #endregion
}

public interface IScheduleOpKey
{
    string ScheduleOpId { get; }
}