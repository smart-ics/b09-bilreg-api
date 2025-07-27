namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public class AntrianModel
{
    public AntrianModel(string antrianId, 
        DateTime antrianDate, 
        TimeSpan startTime, TimeSpan endTime)
    {
        AntrianId = antrianId;
        AntrianDate = antrianDate;
        StartTime = startTime;
        EndTime = endTime;
    }
    public string AntrianId { get; init; }
    public DateTime AntrianDate { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public ServicePointType ServicePoint { get; init; }
}