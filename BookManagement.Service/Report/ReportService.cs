using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Email;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Report
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _db;
        private readonly IEmailService? _emailService;

        public ReportService(AppDbContext db, IEmailService? emailService = null)
        {
            _db = db;
            _emailService = emailService;
        }

        public async Task<ReportResponseDto> CreateReportAsync(Guid reporterId, CreateReportRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new ArgumentException("Lý do báo cáo không được để trống.");

            var reporter = await _db.Users.FirstOrDefaultAsync(u => u.Id == reporterId);
            if (reporter == null)
                throw new KeyNotFoundException("Tài khoản gửi báo cáo không tồn tại.");

            var report = new Repository.Entities.Report
            {
                Id = Guid.NewGuid(),
                ReporterId = reporterId,
                TargetId = dto.TargetId,
                ReportType = dto.ReportType,
                Reason = dto.Reason.Trim(),
                Status = ReportStatus.PENDING,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _db.Reports.AddAsync(report);
            await _db.SaveChangesAsync();

            return MapToDto(report, reporter.FullName ?? reporter.Username);
        }

        public async Task<IEnumerable<ReportResponseDto>> GetReportsAsync(string? status = null, string? type = null)
        {
            var query = _db.Reports
                .AsNoTracking()
                .Include(r => r.Reporter)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ReportStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(r => r.Status == parsedStatus);
            }

            if (!string.IsNullOrEmpty(type) && Enum.TryParse<ReportType>(type, true, out var parsedType))
            {
                query = query.Where(r => r.ReportType == parsedType);
            }

            var reports = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return reports.Select(r => MapToDto(r, r.Reporter?.FullName ?? r.Reporter?.Username ?? "N/A"));
        }

        public async Task<ReportResponseDto> GetReportByIdAsync(Guid reportId)
        {
            var report = await _db.Reports
                .AsNoTracking()
                .Include(r => r.Reporter)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null)
                throw new KeyNotFoundException("Không tìm thấy báo cáo.");

            return MapToDto(report, report.Reporter?.FullName ?? report.Reporter?.Username ?? "N/A");
        }

        public async Task ResolveReportAsync(Guid adminId, Guid reportId, ResolveReportRequestDto dto)
        {
            var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == reportId);
            if (report == null)
                throw new KeyNotFoundException("Không tìm thấy báo cáo.");

            report.Status = dto.Approve ? ReportStatus.RESOLVED : ReportStatus.REJECTED;
            report.AdminNote = dto.AdminNote;
            report.ResolvedByAdminId = adminId;
            report.UpdatedAt = DateTimeOffset.UtcNow;

            // If report is resolved/approved, check for shop violation strikes
            if (dto.Approve)
            {
                Guid? shopId = null;
                if (report.ReportType == ReportType.SHOP || report.ReportType == ReportType.USER)
                {
                    var isShop = await _db.Shops.AnyAsync(s => s.Id == report.TargetId);
                    if (isShop) shopId = report.TargetId;
                }
                else if (report.ReportType == ReportType.FEEDBACK)
                {
                    var feedback = await _db.Feedbacks.FirstOrDefaultAsync(f => f.Id == report.TargetId);
                    if (feedback != null) shopId = feedback.ShopId;
                }
                else if (report.ReportType == ReportType.RESPONSE)
                {
                    var resp = await _db.Responses.FirstOrDefaultAsync(r => r.Id == report.TargetId);
                    if (resp != null) shopId = resp.ShopId;
                }
                else if (report.ReportType == ReportType.CHAT)
                {
                    var chat = await _db.Chats.FirstOrDefaultAsync(c => c.Id == report.TargetId);
                    if (chat != null)
                    {
                        shopId = chat.ShopId;
                    }
                    else
                    {
                        var msg = await _db.Messages.Include(m => m.Chat).FirstOrDefaultAsync(m => m.Id == report.TargetId);
                        if (msg != null) shopId = msg.Chat.ShopId;
                    }
                }

                if (shopId.HasValue)
                {
                    var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Id == shopId.Value);
                    if (shop != null)
                    {
                        shop.ViolationCount += 1;
                        var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30);

                        // Count resolved reports against this shop target in last 30 days
                        var recentReportsCount = await _db.Reports.CountAsync(r =>
                            (r.TargetId == shopId.Value ||
                             (r.ReportType == ReportType.FEEDBACK && _db.Feedbacks.Any(f => f.Id == r.TargetId && f.ShopId == shopId.Value)) ||
                             (r.ReportType == ReportType.RESPONSE && _db.Responses.Any(res => res.Id == r.TargetId && res.ShopId == shopId.Value)) ||
                             (r.ReportType == ReportType.CHAT && (_db.Chats.Any(c => c.Id == r.TargetId && c.ShopId == shopId.Value) || _db.Messages.Any(m => m.Id == r.TargetId && m.Chat.ShopId == shopId.Value))))
                            && r.Status == ReportStatus.RESOLVED
                            && r.CreatedAt >= thirtyDaysAgo);

                        // 3 Strikes Rule -> Lock shop for 1 month
                        if (recentReportsCount >= 3)
                        {
                            shop.Condition = ShopCondition.LOCKED;
                            shop.LockedUntil = DateTimeOffset.UtcNow.AddDays(30);

                            // User status stays ACTIVE so customer features still work!
                            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == shop.Id);
                            if (user != null)
                            {
                                user.Role = UserRole.CUSTOMER;
                            }
                            
                            var lockNoticeContent = $"[THÔNG BÁO TẠM KHÓA CỬA HÀNG (1 THÁNG)] Xin chào {user?.FullName ?? shop.ShopName}, Ban quản trị hệ thống xin thông báo gian hàng '{shop.ShopName}' của bạn đã bị tạm khóa 1 tháng do nhận đủ {recentReportsCount} báo cáo vi phạm (Chat / Feedback / Response) được xác thực trong vòng 30 ngày. Lý do: Vi phạm quy định vận hành / nội dung báo cáo từ người dùng. Thời gian mở khóa dự kiến: {shop.LockedUntil:dd/MM/yyyy HH:mm:ss} UTC. (Ghi chú: Tài khoản người mua hàng Customer của bạn vẫn hoạt động bình thường).";

                            // Send Notification & Email
                            var notification = new BookManagement.Repository.Entities.Notification
                            {
                                Id = Guid.NewGuid(),
                                UserId = shop.Id,
                                Type = NotificationType.SYSTEM,
                                ReferenceId = report.Id,
                                Content = lockNoticeContent,
                                IsRead = false,
                                CreatedAt = DateTimeOffset.UtcNow
                            };
                            await _db.Notifications.AddAsync(notification);

                            if (_emailService != null && user != null && !string.IsNullOrEmpty(user.Email))
                            {
                                string htmlBody = $@"
                                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #E5E7EB; border-radius: 8px;'>
                                        <h2 style='color: #DC2626;'>Thông Báo Tạm Khóa Cửa Hàng (1 Tháng)</h2>
                                        <p>Xin chào <strong>{user.FullName ?? shop.ShopName}</strong>,</p>
                                        <p>Ban quản trị hệ thống xin thông báo gian hàng <strong>{shop.ShopName}</strong> của bạn đã bị tạm khóa <strong>1 tháng</strong> do nhận đủ 3 báo cáo vi phạm được xác thực trong vòng 30 ngày.</p>
                                        <p><strong>Lý do khóa:</strong> Vi phạm quy định vận hành / nội dung báo cáo (Chat / Feedback / Response) từ người dùng.</p>
                                        <p><strong>Thời gian mở khóa dự kiến:</strong> {shop.LockedUntil:dd/MM/yyyy HH:mm:ss} UTC</p>
                                        <p style='color: #059669;'><em>Ghi chú: Tài khoản người mua hàng (Customer) của bạn vẫn duy trì trạng thái HOẠT ĐỘNG bình thường.</em></p>
                                    </div>";
                                try { await _emailService.SendEmailAsync(user.Email, "Thông Báo Tạm Khóa Cửa Hàng - BookManagement", htmlBody); } catch {}
                            }
                        }
                    }
                }
            }

            await _db.SaveChangesAsync();
        }

        private static ReportResponseDto MapToDto(BookManagement.Repository.Entities.Report report, string reporterName) => new ReportResponseDto
        {
            Id = report.Id,
            ReporterId = report.ReporterId,
            ReporterName = reporterName,
            TargetId = report.TargetId,
            ReportType = report.ReportType.ToString(),
            Reason = report.Reason,
            Status = report.Status.ToString(),
            AdminNote = report.AdminNote,
            ResolvedByAdminId = report.ResolvedByAdminId,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt
        };
    }
}
