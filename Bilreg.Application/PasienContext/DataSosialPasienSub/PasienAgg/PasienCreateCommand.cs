using System.Transactions;
using Bilreg.Application.Helpers;
using Bilreg.Application.PasienContext.ParamContext.ParamSistemAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using CommunityToolkit.Diagnostics;
using MediatR;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using Xunit.Sdk;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public record PasienCreateCommand(
    string PasienName,
    string TempatLahir,
    string TglLahir,
    string NickName,
    string Gender,
    string IbuKandung,
    string GolDarah,
    string UserId) : IRequest<PasienCreateResponse>;

public record PasienCreateResponse(string PasienId);

public class PasienCreateHandler : IRequestHandler<PasienCreateCommand, PasienCreateResponse>
{
    private readonly IParamSistemDal _paramSistemDal;
    private readonly INunaCounterBL _counter;
    private readonly IPasienWriter _writer;
    private readonly ITglJamProvider _dateTime;

    private const string KODE_RS_PARAM_KEY = "RS__XXXXXX_KODE";
    private const string NO_MR_PARAM_KEY = "NOMR";
    private const string FORMAT_TGL_YMD = "yyyy-MM-dd";
    private const string GENDER_LIST = "LPWMF10";
    private const string ACTIVITY_NAME = "PasienCreate";

    public PasienCreateHandler(IParamSistemDal paramSistemDal, 
        INunaCounterBL counter, 
        IPasienWriter writer,
        ITglJamProvider dateTime)
    {
        _paramSistemDal = paramSistemDal;
        _counter = counter;
        _writer = writer;
        _dateTime = dateTime;
    }

    public Task<PasienCreateResponse> Handle(PasienCreateCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        Guard.IsNotNull(request);
        Guard.IsNotEmpty(request.PasienName);
        Guard.IsNotEmpty(request.TglLahir);
        Guard.IsTrue(request.TglLahir.IsValidTgl(FORMAT_TGL_YMD));
        Guard.IsTrue(request.Gender.Length == 1);
        Guard.IsTrue(request.Gender.IsValidA(x => GENDER_LIST.Contains(x)));
        Guard.IsNotEmpty(request.IbuKandung);
        Guard.IsNotEmpty(request.GolDarah);
        
        //  BUILD
        var pasienId = NewPasienId();
        var pasien = new PasienModel(pasienId, request.PasienName);
        pasien.SetPersonalInfo(request.TempatLahir,
            request.TglLahir.ToDate(DateFormatEnum.YMD),
            request.NickName, request.Gender, 
            request.IbuKandung, request.GolDarah);
        pasien.SetTglMedrec(_dateTime.Now);
        pasien.RemoveNull();
        
        var changes = PropertyChangeHelper.GetChanges(new PasienModelSerializable(), pasien);
        var pasienLog = new PasienLogModel(pasienId, ACTIVITY_NAME, request.UserId);
        pasienLog.SetChangeLog(changes);
        pasien.Add(pasienLog);
        
        //  WRITE
        var pasienResult = _writer.Save(pasien);
        var result = new PasienCreateResponse(pasienResult.PasienId);
        return Task.FromResult(result);
    }

    private string NewPasienId()
    {
        var kodeRsEncrypted = _paramSistemDal.GetData(KODE_RS_PARAM_KEY)?.Value ?? string.Empty;
        var kodeRs = X1EncryptionHelper.DecodingNeo(kodeRsEncrypted);
        
        using var trans = TransHelper.NewScope(IsolationLevel.Serializable);
        var newId = _counter.GenerateDec(NO_MR_PARAM_KEY, kodeRs, 15, string.Empty);
        trans.Complete();
        
        return newId;
    }
}

