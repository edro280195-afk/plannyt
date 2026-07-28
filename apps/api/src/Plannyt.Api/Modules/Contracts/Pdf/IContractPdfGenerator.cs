using Plannyt.Api.Modules.Contracts.Application;

namespace Plannyt.Api.Modules.Contracts.Pdf;

public interface IContractPdfGenerator
{
    byte[] GeneratePublished(ContractPdfModel model);

    byte[] GenerateFinal(
        ContractPdfModel model,
        IReadOnlyList<SignatureEvidenceSummaryResponse> evidence);
}
