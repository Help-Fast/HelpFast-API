using System.Threading;
using System.Threading.Tasks;

namespace ApiHelpFast.Services;

public interface IGoogleDriveService
{
    Task<string> LerDocumentoComoStringAsync(string fileId, CancellationToken cancellationToken = default);
}

