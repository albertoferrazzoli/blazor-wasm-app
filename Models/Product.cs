using System.ComponentModel.DataAnnotations;

namespace blazor_wasm_app.Models;

public class Product : AuditableEntity<Guid>
{
    [Required(ErrorMessage = "Il nome è obbligatorio")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Il nome deve essere tra 3 e 100 caratteri")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [Range(0.01, 999999.99, ErrorMessage = "Il prezzo deve essere tra 0.01 e 999999.99")]
    public decimal Price { get; set; }

    [Required]
    public ProductCategory Category { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;

    public List<string> Tags { get; set; } = new();

    public Dictionary<string, object> Metadata { get; set; } = new();

    public override string GetDisplayName() => $"{Name} ({Category})";
}

public enum ProductCategory
{
    Electronics,
    Clothing,
    Food,
    Books,
    HomeAndGarden,
    Sports,
    Toys,
    Other
}
