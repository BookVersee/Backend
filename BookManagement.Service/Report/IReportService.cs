using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookManagement.Service.Report
{
    public interface IReportService
    {
        Task<ReportResponseDto> CreateReportAsync(Guid reporterId, CreateReportRequestDto dto);
        Task<IEnumerable<ReportResponseDto>> GetReportsAsync(string? status = null, string? type = null);
        Task<ReportResponseDto> GetReportByIdAsync(Guid reportId);
        Task ResolveReportAsync(Guid adminId, Guid reportId, ResolveReportRequestDto dto);
    }
}
