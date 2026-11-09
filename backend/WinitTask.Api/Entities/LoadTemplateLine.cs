namespace WinitTask.Api.Entities;

/// <summary>
/// One product line inside a load template, holding the model quantity per weekday.
/// Table: LoadTemplateLine
/// </summary>
public class LoadTemplateLine
{
    public int Id { get; set; }

    public string UID { get; set; } = null!;

    /// <summary>FK to LoadTemplate.UID.</summary>
    public string LoadTemplateUID { get; set; } = null!;

    /// <summary>FK to SKU.Code.</summary>
    public string SKUCode { get; set; } = null!;

    /// <summary>SKUUOM.Code (EA / OU / CT) — the Code is stored, the Name is displayed.</summary>
    public string UOMCode { get; set; } = null!;

    public decimal MonQty { get; set; }
    public decimal TueQty { get; set; }
    public decimal WedQty { get; set; }
    public decimal ThuQty { get; set; }
    public decimal FriQty { get; set; }
    public decimal SatQty { get; set; }
    public decimal SunQty { get; set; }

    public LoadTemplate? LoadTemplate { get; set; }
    public SKU? SKU { get; set; }
}
