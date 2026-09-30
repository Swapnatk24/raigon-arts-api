using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaigonArts.Api.Common;
using RaigonArts.Api.DTOs;
using RaigonArts.Api.Services;

namespace RaigonArts.Api.Controllers;

[ApiController]
[Route("api/v1/backup")]
[Authorize(Roles = "ADMIN")]
public class BackupController : ControllerBase
{
    private readonly IBackupService _backupService;

    public BackupController(IBackupService backupService)
    {
        _backupService = backupService;
    }

    [HttpGet("export")]
    public async Task<ActionResult<BackupExportDto>> ExportDatabase()
    {
        var backup = await _backupService.ExportBackupAsync();
        return Ok(backup);
    }

    [HttpPost("restore")]
    public async Task<ActionResult<ApiResponse<BackupRestoreResponseData>>> RestoreDatabase([FromBody] BackupRestoreRequest request)
    {
        var result = await _backupService.RestoreBackupAsync(request);
        return Ok(ApiResponse<BackupRestoreResponseData>.Ok(result, "Database successfully restored from JSON backup."));
    }
}
