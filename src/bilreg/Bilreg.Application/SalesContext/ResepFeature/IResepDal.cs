namespace Bilreg.Application.SalesContext.ResepFeature;

public interface IResepDal
{
    void LockPrescription(string resepId);
}
