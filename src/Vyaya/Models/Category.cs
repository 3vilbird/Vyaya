using SQLite;

namespace Vyaya.Models;

[Table("Categories")]
public class Category
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed, NotNull]
    public string Name { get; set; } = string.Empty;

    public string Icon { get; set; } = "📁";

    public string ColorHex { get; set; } = "#0A84FF";

    public bool IsDefault { get; set; }

    public string? Keywords { get; set; }
}
