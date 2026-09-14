using System;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Repository.Entities
{
    public class Report : BaseEntity<Guid>, IAuditableEntity
    {
        public Guid ReporterId { get; set; }
        public Guid TargetId { get; set; }
        public ReportType ReportType { get; set; }
        public string Reason { get; set; } = null!;
        public ReportStatus Status { get; set; } = ReportStatus.PENDING;
        public string? AdminNote { get; set; }
        public Guid? ResolvedByAdminId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation properties
        public User Reporter { get; set; } = null!;
        public User? ResolvedByAdmin { get; set; }
    }
}
