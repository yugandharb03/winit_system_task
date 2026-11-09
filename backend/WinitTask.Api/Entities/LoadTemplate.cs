namespace WinitTask.Api.Entities;

/// <summary>
/// Header record of a load request template.
/// Table: LoadTemplate
/// </summary>
public class LoadTemplate
{
    public int Id { get; set; }

    /// <summary>Business key (GUID string) used by the API and referenced by child rows.</summary>
    public string UID { get; set; } = null!;

    public string TemplateName { get; set; } = null!;

    public string? TemplateDescription { get; set; }

    /// <summary>
    /// "No of Items": the total load quantity, i.e. every Mon–Sun quantity across all
    /// lines added together. Recalculated whenever the template is saved.
    /// </summary>
    public int ItemCount { get; set; }

    public ICollection<LoadTemplateLine> Lines { get; set; } = new List<LoadTemplateLine>();
}
