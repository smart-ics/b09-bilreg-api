using System.Transactions;
using Bilreg.Application.Helpers;
using Bilreg.Application.PasienContext.ParamContext.ParamSistemAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using CommunityToolkit.Diagnostics;
using MediatR;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public record PasienCreateCommand(
    string PasienName,
    string TempatLahir,
    string TglLahir,
    string NickName,
    string Gender,
    string IbuKandung,
    string GolDarah) : IRequest<PasienCreateResponse>;

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
        Guard.IsNotEmpty(request.TglLahir);
        Guard.IsTrue(request.TglLahir.IsValidTgl(FORMAT_TGL_YMD));
        
        //  BUILD
        var pasienId = NewPasienId();
        var gender = new GenderType(request.Gender);
        var pasien = new PasienModel(pasienId, request.PasienName,
            request.TglLahir.ToDate(), gender);
        pasien.SetPersonalInfo(request.NickName, request.TempatLahir, 
            request.IbuKandung, new GolDarahType(request.GolDarah));
        
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

