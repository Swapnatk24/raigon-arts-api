using System.Text;
using Microsoft.AspNetCore.Mvc;
using RaigonArts.Api.Common;
using RaigonArts.Api.DTOs;
using RaigonArts.Api.Services;

namespace RaigonArts.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("cards")]
    public async Task<ActionResult<ApiResponse<ReportCardsDto>>> GetReportCards()
    {
        var result = await _reportService.GetReportCardsAsync();
        return Ok(ApiResponse<ReportCardsDto>.Ok(result));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<ReportCardsDto>>> GetReportSummary()
    {
        var result = await _reportService.GetReportCardsAsync();
        return Ok(ApiResponse<ReportCardsDto>.Ok(result));
    }

    [HttpGet("financials")]
    public async Task<ActionResult<ApiResponse<FinancialReportDto>>> GetFinancials()
    {
        var result = await _reportService.GetFinancialReportAsync();
        return Ok(ApiResponse<FinancialReportDto>.Ok(result));
    }

    [HttpGet("production-breakdown")]
    public async Task<ActionResult<ApiResponse<List<ProductionBreakdownItemDto>>>> GetProductionBreakdown()
    {
        var result = await _reportService.GetProductionBreakdownAsync();
        return Ok(ApiResponse<List<ProductionBreakdownItemDto>>.Ok(result));
    }

    [HttpGet("top-frame-sizes")]
    public async Task<ActionResult<ApiResponse<List<FrameSizeSalesBreakdownDto>>>> GetTopFrameSizes([FromQuery] int? limit)
    {
        var result = await _reportService.GetTopFrameSizesSoldAsync(limit);
        return Ok(ApiResponse<List<FrameSizeSalesBreakdownDto>>.Ok(result));
    }

    [HttpGet("popular-materials")]
    public async Task<ActionResult<ApiResponse<List<MaterialSalesBreakdownDto>>>> GetPopularMaterials([FromQuery] int? limit)
    {
        var result = await _reportService.GetPopularMaterialsAsync(limit);
        return Ok(ApiResponse<List<MaterialSalesBreakdownDto>>.Ok(result));
    }

    [HttpGet("production-analytics")]
    [HttpGet("breakdown")]
    public async Task<ActionResult<ApiResponse<ProductionAnalyticsDto>>> GetProductionAnalytics()
    {
        var result = await _reportService.GetProductionAnalyticsAsync();
        return Ok(ApiResponse<ProductionAnalyticsDto>.Ok(result));
    }

    [HttpGet("export-csv")]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportCsv()
    {
        var csvContent = await _reportService.GenerateOrdersCsvAsync();
        var bytes = Encoding.UTF8.GetBytes(csvContent);
        var fileName = $"raigon_orders_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}
