namespace Topstar.Infrastructure.Entities;

public sealed class UserAccountAdminNoteEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
