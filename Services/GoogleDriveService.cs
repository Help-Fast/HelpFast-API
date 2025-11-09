using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Options;
using ApiHelpFast.Models;

namespace ApiHelpFast.Services;

public class GoogleDriveService : IGoogleDriveService, IDisposable
{
    private readonly GoogleDriveOptions _options;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private DriveService? _driveService;
    private bool _disposed;

    public GoogleDriveService(IOptions<GoogleDriveOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<string> LerDocumentoComoStringAsync(string fileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            throw new ArgumentException("O identificador do arquivo do Google Drive é obrigatório.", nameof(fileId));
        }

        var driveService = await ObterDriveServiceAsync(cancellationToken);

        var metadataRequest = driveService.Files.Get(fileId);
        metadataRequest.Fields = "id, name, mimeType";

        var file = await metadataRequest.ExecuteAsync(cancellationToken)
                  ?? throw new InvalidOperationException($"Não foi possível localizar o arquivo com ID '{fileId}' no Google Drive.");

        return file.MimeType switch
        {
            "application/vnd.google-apps.document" => await ExportarGoogleDocComoTextoAsync(driveService, fileId, cancellationToken),
            _ => await BaixarArquivoTextoAsync(driveService, fileId, file.MimeType, cancellationToken)
        };
    }

    private async Task<DriveService> ObterDriveServiceAsync(CancellationToken cancellationToken)
    {
        if (_driveService != null)
        {
            return _driveService;
        }

        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_driveService != null)
            {
                return _driveService;
            }

            _driveService = await CriarDriveServiceAsync(cancellationToken);
            return _driveService;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private async Task<DriveService> CriarDriveServiceAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.CredenciaisJsonPath))
        {
            throw new InvalidOperationException("Configure o caminho do arquivo de credenciais do Google Drive em 'GoogleDrive:CredenciaisJsonPath'.");
        }

        if (!File.Exists(_options.CredenciaisJsonPath))
        {
            throw new FileNotFoundException("O arquivo de credenciais do Google Drive não foi encontrado.", _options.CredenciaisJsonPath);
        }

        GoogleCredential credential;
        await using (var stream = new FileStream(_options.CredenciaisJsonPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            credential = GoogleCredential.FromStream(stream);
        }

        var scopes = (_options.Escopos?.Length ?? 0) > 0
            ? _options.Escopos
            : new[] { DriveService.Scope.DriveReadonly };

        if (credential.IsCreateScopedRequired)
        {
            credential = credential.CreateScoped(scopes);
        }

        var initializer = new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = string.IsNullOrWhiteSpace(_options.ApplicationName)
                ? "HelpFastDesktop"
                : _options.ApplicationName
        };

        return new DriveService(initializer);
    }

    private async Task<string> ExportarGoogleDocComoTextoAsync(DriveService driveService, string fileId, CancellationToken cancellationToken)
    {
        var exportMimeType = string.IsNullOrWhiteSpace(_options.ExportMimeTypePadrao)
            ? "text/plain"
            : _options.ExportMimeTypePadrao;

        var exportRequest = driveService.Files.Export(fileId, exportMimeType);
        await using var stream = new MemoryStream();
        await exportRequest.DownloadAsync(stream, cancellationToken);
        return ConverterBytesParaString(stream.ToArray(), exportMimeType);
    }

    private async Task<string> BaixarArquivoTextoAsync(DriveService driveService, string fileId, string? mimeType, CancellationToken cancellationToken)
    {
        var getRequest = driveService.Files.Get(fileId);
        await using var stream = new MemoryStream();
        await getRequest.DownloadAsync(stream, cancellationToken);
        return ConverterBytesParaString(stream.ToArray(), mimeType);
    }

    private static string ConverterBytesParaString(byte[] dados, string? mimeType)
    {
        if (dados.Length == 0)
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(mimeType) ||
            mimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mimeType, "application/json", StringComparison.OrdinalIgnoreCase))
        {
            return Encoding.UTF8.GetString(dados);
        }

        if (string.Equals(mimeType, "text/html", StringComparison.OrdinalIgnoreCase))
        {
            return Encoding.UTF8.GetString(dados);
        }

        throw new NotSupportedException($"O tipo de arquivo '{mimeType}' não é suportado para conversão direta em texto. Considere exportar o arquivo para um formato texto no Google Drive.");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _driveService?.Dispose();
        _initializationLock.Dispose();
        GC.SuppressFinalize(this);
    }
}

