namespace WinitTask.Api.Entities;

/// <summary>
/// Product master.
/// Table: SKU
/// </summary>
public class SKU
{
    public int Id { get; set; }

    public string UID { get; set; } = null!;

    /// <summary>Business product code shown in the UI and stored on template lines.</summary>
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public ICollection<SKUUOM> UOMs { get; set; } = new List<SKUUOM>();
}
