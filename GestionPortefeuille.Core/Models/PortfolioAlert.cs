namespace GestionPortefeuille.Core.Models;

public enum AlertSeverity
{
    Info,
    Warning,
    Danger
}

public class PortfolioAlert
{
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
