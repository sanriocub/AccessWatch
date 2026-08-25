using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using AccessWatch.Models;

namespace AccessWatch.Services
{
    public class SnsNotificationService : ISnsNotificationService
    {
        private readonly IAmazonSimpleNotificationService _snsClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SnsNotificationService> _logger;

        public SnsNotificationService(
            IAmazonSimpleNotificationService snsClient,
            IConfiguration configuration,
            ILogger<SnsNotificationService> logger)
        {
            _snsClient = snsClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> PublishReportStatusChangedAsync(
            AccessibilityReport report,
            ReportStatus previousStatus,
            ReportStatus newStatus,
            CancellationToken cancellationToken = default)
        {
            var topicArn = _configuration["AWS:SnsTopicArn"];

            if (string.IsNullOrWhiteSpace(topicArn))
            {
                _logger.LogWarning(
                    "SNS notification was skipped for report {ReportId} because AWS:SnsTopicArn is not configured.",
                    report.ReportId);
                return false;
            }

            var facilityName = report.Facility?.Name ?? "Unknown facility";
            var categoryName = report.Category?.Name ?? "Uncategorized";
            var message =
                $"AccessWatch report #{report.ReportId} status changed from {previousStatus} to {newStatus}.\n" +
                $"Facility: {facilityName}\n" +
                $"Category: {categoryName}\n" +
                $"Updated at: {DateTime.UtcNow:O}";

            var request = new PublishRequest
            {
                TopicArn = topicArn,
                Subject = $"AccessWatch report #{report.ReportId}: {newStatus}",
                Message = message
            };

            try
            {
                var response = await _snsClient.PublishAsync(request, cancellationToken);

                _logger.LogInformation(
                    "Published SNS status-change notification for report {ReportId}. MessageId: {MessageId}",
                    report.ReportId,
                    response.MessageId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to publish SNS status-change notification for report {ReportId}.",
                    report.ReportId);
                return false;
            }
        }
    }
}
