using WinitTask.Api.DTOs;

namespace WinitTask.Api.Services;

public interface ILoadTemplateService
{
    Task<PagedResult<LoadTemplateListItemDto>> GetPagedAsync(
        LoadTemplateQueryParameters query, CancellationToken cancellationToken = default);

    Task<LoadTemplateDetailDto> GetByUidAsync(string uid, CancellationToken cancellationToken = default);

    Task<LoadTemplateDetailDto> CreateAsync(
        SaveLoadTemplateRequest request, CancellationToken cancellationToken = default);

    Task<LoadTemplateDetailDto> UpdateAsync(
        string uid, SaveLoadTemplateRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(string uid, CancellationToken cancellationToken = default);
}
