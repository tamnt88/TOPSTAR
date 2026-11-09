using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Web.Services;

public sealed record TrackingConfiguration(bool Enabled, string? HeadCode, string? BodyCode);

public sealed class TrackingConfigurationService(TopstarDbContext db)
{
    public async Task<TrackingConfiguration?> ReadAsync(CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        var close = connection.State != ConnectionState.Open;
        try
        {
            if (close) await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT IsEnabled, HeadCode, BodyCode FROM dbo.WebsiteTrackingConfiguration WHERE Id = 1";
            await using var reader = await command.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? new(reader.GetBoolean(0), reader.IsDBNull(1) ? null : reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2)) : null;
        }
        catch (SqlException ex) when (ex.Number == 208) { return null; }
        finally { if (close) await connection.CloseAsync(); }
    }

    public async Task SaveAsync(TrackingConfiguration value, Guid actor, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE dbo.WebsiteTrackingConfiguration WITH (UPDLOCK, SERIALIZABLE)
SET IsEnabled={value.Enabled}, HeadCode={value.HeadCode}, BodyCode={value.BodyCode}, UpdatedAtUtc=SYSUTCDATETIME(), UpdatedByUserId={actor} WHERE Id=1;
IF @@ROWCOUNT=0 INSERT dbo.WebsiteTrackingConfiguration(Id,IsEnabled,HeadCode,BodyCode,UpdatedAtUtc,UpdatedByUserId)
VALUES(1,{value.Enabled},{value.HeadCode},{value.BodyCode},SYSUTCDATETIME(),{actor});
INSERT dbo.WebsiteTrackingHistory(IsEnabled,HeadCode,BodyCode,ActorUserId,CreatedAtUtc)
VALUES({value.Enabled},{value.HeadCode},{value.BodyCode},{actor},SYSUTCDATETIME());", ct);
        await transaction.CommitAsync(ct);
    }
}
