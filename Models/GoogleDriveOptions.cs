namespace ApiHelpFast.Models;

public class GoogleDriveOptions
{
    public string CredenciaisJsonPath { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = "HelpFastDesktop";
    public string[] Escopos { get; set; } = new[] { "https://www.googleapis.com/auth/drive.readonly" };
    public string? ExportMimeTypePadrao { get; set; } = "text/plain";
}

