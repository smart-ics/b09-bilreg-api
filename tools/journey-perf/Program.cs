using System.Diagnostics;
using System.Reflection;
using Bilreg.Application.AdmisiRanapContext.JourneyFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.JourneyFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;

typeof(ConnStringHelper).GetField("_connString", BindingFlags.NonPublic | BindingFlags.Static)!
    .SetValue(null, "");

var dal = new JourneyDal(Options.Create(new DatabaseOptions
{
    ServerName = "dev.smart-ics.com",
    DbName = "HOSPITAL_HPL"
}));

dal.List(new JourneyListFilter(PageSize: 50));

var sw = Stopwatch.StartNew();
var active = dal.List(new JourneyListFilter(JourneyListScope.Active, PageSize: 50));
sw.Stop();
Console.WriteLine($"active_list_ms={sw.ElapsedMilliseconds}; matches={active.TotalMatches}; items={active.Items.Count}");

sw.Restart();
var hist = dal.List(new JourneyListFilter(JourneyListScope.History, PageSize: 50));
sw.Stop();
Console.WriteLine($"history_list_ms={sw.ElapsedMilliseconds}; matches={hist.TotalMatches}; items={hist.Items.Count}");

var id = hist.Items.FirstOrDefault()?.JourneyId ?? active.Items.FirstOrDefault()?.JourneyId;
if (id is not null)
{
    sw.Restart();
    var d = dal.GetByJourneyId(id);
    sw.Stop();
    Console.WriteLine($"detail_ms={sw.ElapsedMilliseconds}; journey={id}; stage={d?.Stage}");
}
