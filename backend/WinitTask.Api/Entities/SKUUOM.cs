namespace WinitTask.Api.Entities;

/// <summary>
/// Units of measure available for a SKU: EA (Each), OU (Outer), CT (Carton).
/// Table: SKUUOM
/// </summary>
public class SKUUOM
{
    public int Id { get; set; }

    public string UID { get; set; } = null!;

    /// <summary>FK to SKU.Code.</summary>
    public string SKUCode { get; set; } = null!;

    /// <summary>Stored on LoadTemplateLine.UOMCode.</summary>
    public string Code { get; set; } = null!;

    /// <summary>Shown in the UOM dropdown.</summary>
    public string Name { get; set; } = null!;

    public SKU? SKU { get; set; }
}
