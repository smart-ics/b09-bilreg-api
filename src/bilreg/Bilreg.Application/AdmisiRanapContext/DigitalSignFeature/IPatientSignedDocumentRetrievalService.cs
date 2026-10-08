namespace Bilreg.Application.AdmisiRanapContext.DigitalSignFeature;

public interface IPatientSignedDocumentRetrievalService
{
    Task<byte[]> RetrievePatientSignedPdfAsync(
        string signingRequestId,
        string signedDocUrl,
        CancellationToken cancellationToken = default);
}
