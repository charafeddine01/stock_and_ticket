using System.Globalization;

namespace StockPortal.Services
{
    public interface ILocalizationService
    {
        string this[string key] { get; }
        string Get(string key);
        string Get(string key, params object[] args);
        bool IsArabic { get; }
        string CurrentCulture { get; }
        string Direction { get; }
        string Status(string? status);
        string Priority(int? priority);
        string TicketType(string? type);
        string TargetTeam(string? team);
        string CustodyType(string? type);
        string SupplierType(int? type);
        string MaintenanceType(int? type);
        string Role(string? role);
    }
}
