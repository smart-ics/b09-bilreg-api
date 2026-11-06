namespace Bilreg.Application.AccountingContext.JurnalFeature;

public interface IJurnalDal
{
    void UpdateTimestamp(string jurnalId, DateTime tglJurnal);
}
