using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Settings;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Settings;

public sealed class SampleDataCleanupService(TopstarDbContext db) : ISampleDataCleanupService
{
    public Task<SampleDataCleanupPreview> PreviewAsync(Guid actorUserId, CancellationToken ct = default)
        => RunAsync(false, actorUserId, ct);

    public async Task<SampleDataCleanupResult> DeleteAsync(Guid actorUserId, CancellationToken ct = default)
    {
        var preview = await RunAsync(true, actorUserId, ct);
        var message = preview.HasSampleData
            ? $"Đã xóa {preview.TotalAccounts:N0} tài khoản mẫu và {preview.RelatedRecords:N0} bản ghi liên quan."
            : "Không còn dữ liệu mẫu để xóa.";
        return new(true, message, preview);
    }

    private async Task<SampleDataCleanupPreview> RunAsync(bool execute, Guid actorUserId, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "dbo.usp_AdminCleanupSampleAccounts";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(new SqlParameter("@Execute", SqlDbType.Bit) { Value = execute });
        command.Parameters.Add(new SqlParameter("@ActorUserId", SqlDbType.UniqueIdentifier) { Value = actorUserId });
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return new(0, 0, 0, 0, 0, 0);
        return new(
            reader.GetInt32(reader.GetOrdinal("MemberAccounts")),
            reader.GetInt32(reader.GetOrdinal("BrandAccounts")),
            reader.GetInt32(reader.GetOrdinal("KocProfiles")),
            reader.GetInt32(reader.GetOrdinal("BrandProfiles")),
            reader.GetInt32(reader.GetOrdinal("Campaigns")),
            reader.GetInt32(reader.GetOrdinal("RelatedRecords")));
    }
}
