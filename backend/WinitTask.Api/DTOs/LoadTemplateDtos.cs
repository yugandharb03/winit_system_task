using System.ComponentModel.DataAnnotations;

namespace WinitTask.Api.DTOs;

/// <summary>One row of the "Maintain Load Request Template" grid.</summary>
public class LoadTemplateListItemDto
{
    public string Uid { get; init; } = null!;
    public string TemplateName { get; init; } = null!;
    public string? TemplateDescription { get; init; }

    /// <summary>The UI renders 0 as "N/A".</summary>
    public int ItemCount { get; init; }
}

/// <summary>Full template for the Add / Edit page.</summary>
public class LoadTemplateDetailDto
{
    public string Uid { get; init; } = null!;
    public string TemplateName { get; init; } = null!;
    public string? TemplateDescription { get; init; }
    public int ItemCount { get; init; }
    public IReadOnlyList<LoadTemplateLineDto> Lines { get; init; } = Array.Empty<LoadTemplateLineDto>();
}

/// <summary>One product row of the model-quantity grid.</summary>
public class LoadTemplateLineDto
{
    public string Uid { get; init; } = null!;
    public string SkuCode { get; init; } = null!;
    public string SkuName { get; init; } = null!;

    /// <summary>The stored SKUUOM.Code.</summary>
    public string UomCode { get; init; } = null!;

    /// <summary>The SKUUOM.Name matching UomCode — what the dropdown displays.</summary>
    public string UomName { get; init; } = null!;

    /// <summary>Every UOM this SKU can be ordered in, to populate the dropdown.</summary>
    public IReadOnlyList<UomDto> AvailableUoms { get; init; } = Array.Empty<UomDto>();

    public decimal MonQty { get; init; }
    public decimal TueQty { get; init; }
    public decimal WedQty { get; init; }
    public decimal ThuQty { get; init; }
    public decimal FriQty { get; init; }
    public decimal SatQty { get; init; }
    public decimal SunQty { get; init; }
}

/// <summary>Body of POST and PUT /api/load-templates.</summary>
public class SaveLoadTemplateRequest
{
    [Required(ErrorMessage = "Template name is required.")]
    [StringLength(250, ErrorMessage = "Template name cannot exceed 250 characters.")]
    public string TemplateName { get; init; } = null!;

    [StringLength(250, ErrorMessage = "Template description cannot exceed 250 characters.")]
    public string? TemplateDescription { get; init; }

    /// <summary>May be empty — a template with no products is valid.</summary>
    public List<SaveLoadTemplateLineRequest> Lines { get; init; } = new();
}

public class SaveLoadTemplateLineRequest
{
    [Required(ErrorMessage = "SKU code is required.")]
    [StringLength(250)]
    public string SkuCode { get; init; } = null!;

    [Required(ErrorMessage = "UOM code is required.")]
    [StringLength(250)]
    public string UomCode { get; init; } = null!;

    [Range(0, 9999999999999999.99, ErrorMessage = "Monday quantity cannot be negative.")]
    public decimal MonQty { get; init; }

    [Range(0, 9999999999999999.99, ErrorMessage = "Tuesday quantity cannot be negative.")]
    public decimal TueQty { get; init; }

    [Range(0, 9999999999999999.99, ErrorMessage = "Wednesday quantity cannot be negative.")]
    public decimal WedQty { get; init; }

    [Range(0, 9999999999999999.99, ErrorMessage = "Thursday quantity cannot be negative.")]
    public decimal ThuQty { get; init; }

    [Range(0, 9999999999999999.99, ErrorMessage = "Friday quantity cannot be negative.")]
    public decimal FriQty { get; init; }

    [Range(0, 9999999999999999.99, ErrorMessage = "Saturday quantity cannot be negative.")]
    public decimal SatQty { get; init; }

    [Range(0, 9999999999999999.99, ErrorMessage = "Sunday quantity cannot be negative.")]
    public decimal SunQty { get; init; }
}
