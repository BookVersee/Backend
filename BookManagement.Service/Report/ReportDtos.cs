using System;
using System.Text.Json.Serialization;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.Report
{
    public class CreateReportRequestDto
    {
        public Guid TargetId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ReportType ReportType { get; set; }

        public string Reason { get; set; } = null!;
    }

    public class ResolveReportRequestDto
    {
        public bool Approve { get; set; }
        public string? AdminNote { get; set; }
    }

    public class ReportResponseDto
    {
        public Guid Id { get; set; }
        public Guid ReporterId { get; set; }
        public string ReporterName { get; set; } = null!;
        public Guid TargetId { get; set; }
        public string ReportType { get; set; } = null!;
        public string Reason { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? AdminNote { get; set; }
        public Guid? ResolvedByAdminId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
