using Plannyt.Api.Modules.Proposals.Application;

namespace Plannyt.Api.Modules.Proposals.Pdf;

public interface IProposalPdfGenerator
{
    byte[] Generate(ProposalPublicResponse proposal);
}
