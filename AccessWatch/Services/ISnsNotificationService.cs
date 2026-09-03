using AccessWatch.Models;

namespace AccessWatch.Services
{
    public interface ISnsNotificationService
    {
        Task<bool> PublishReportStatusChangedAsync(
            AccessibilityReport report,
            ReportStatus previousStatus,
            ReportStatus newStatus,
            CancellationToken cancellationToken = default);
    }
}
