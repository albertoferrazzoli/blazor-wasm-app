namespace blazor_wasm_app.Models;

public abstract class BaseEntity<TKey> where TKey : struct
{
    public TKey Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public virtual string GetDisplayName() => $"{GetType().Name} #{Id}";
}

public abstract class AuditableEntity<TKey> : BaseEntity<TKey> where TKey : struct
{
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }

    public void MarkModified(string user)
    {
        UpdatedAt = DateTime.UtcNow;
        ModifiedBy = user;
    }
}
