using System.ComponentModel.DataAnnotations;
using WinitTask.Api.DTOs;
using WinitTask.Api.Exceptions;

namespace WinitTask.Api.Tests.Dtos;

public class QueryAndValidationContractTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    [InlineData(1, 1)]
    [InlineData(4, 4)]
    public void LoadTemplatePage_BelowOne_BecomesOne(int input, int expected)
    {
        var query = new LoadTemplateQueryParameters { Page = input };

        Assert.Equal(expected, query.Page);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 1)]
    [InlineData(10, 10)]
    [InlineData(200, 200)]
    [InlineData(201, 200)]
    public void LoadTemplatePageSize_IsClampedBetween1And200(int input, int expected)
    {
        var query = new LoadTemplateQueryParameters { PageSize = input };

        Assert.Equal(expected, query.PageSize);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-2, 1)]
    [InlineData(3, 3)]
    public void SkuPage_BelowOne_BecomesOne(int input, int expected)
    {
        var query = new SkuQueryParameters { Page = input };

        Assert.Equal(expected, query.Page);
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(-8, 25)]
    [InlineData(25, 25)]
    [InlineData(500, 500)]
    [InlineData(501, 500)]
    public void SkuPageSize_IsClampedBetween1And500(int input, int expected)
    {
        var query = new SkuQueryParameters { PageSize = input };

        Assert.Equal(expected, query.PageSize);
    }

    [Fact]
    public void ExcludedCodeList_SplitsTrimsAndDropsBlanks()
    {
        var query = new SkuQueryParameters { ExcludeCodes = " MILK, BRD ,, JUICE " };

        Assert.Equal(new[] { "MILK", "BRD", "JUICE" }, query.ExcludedCodeList);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ExcludedCodeList_Blank_IsEmpty(string? excludeCodes)
    {
        var query = new SkuQueryParameters { ExcludeCodes = excludeCodes };

        Assert.Empty(query.ExcludedCodeList);
    }

    [Fact]
    public void PagedResult_ComputesPageFlags()
    {
        var middle = new PagedResult<int> { TotalCount = 21, Page = 2, PageSize = 10 };
        Assert.Equal(3, middle.TotalPages);
        Assert.True(middle.HasPrevious);
        Assert.True(middle.HasNext);

        var last = new PagedResult<int> { TotalCount = 21, Page = 3, PageSize = 10 };
        Assert.False(last.HasNext);

        var empty = new PagedResult<int> { TotalCount = 0, Page = 1, PageSize = 10 };
        Assert.Equal(0, empty.TotalPages);
        Assert.False(empty.HasPrevious);
        Assert.False(empty.HasNext);

        var unbounded = new PagedResult<int> { TotalCount = 5, Page = 1, PageSize = 0 };
        Assert.Equal(0, unbounded.TotalPages);
    }

    [Fact]
    public void SaveRequest_RejectsAMissingName_AnOverlongName_AndANegativeQuantity()
    {
        var request = new SaveLoadTemplateRequest
        {
            TemplateName = "",
            TemplateDescription = new string('d', 251),
            Lines = new List<SaveLoadTemplateLineRequest>
            {
                new() { SkuCode = "MILK", UomCode = "EA", MonQty = -1 }
            }
        };

        var results = Validate(request);
        var lineResults = Validate(request.Lines[0]);

        Assert.Contains(results, result => result.ErrorMessage == "Template name is required.");
        Assert.Contains(results, result => result.ErrorMessage == "Template description cannot exceed 250 characters.");
        Assert.Contains(lineResults, result => result.ErrorMessage == "Monday quantity cannot be negative.");
    }

    [Fact]
    public void SaveRequest_AcceptsAnEmptyProductList_AndA250CharacterName()
    {
        var request = new SaveLoadTemplateRequest
        {
            TemplateName = new string('n', 250),
            TemplateDescription = new string('d', 250),
            Lines = new List<SaveLoadTemplateLineRequest>()
        };

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void NotFoundException_FormatsTheMissingKey()
    {
        var ex = NotFoundException.For("Load template", "tpl-1");

        Assert.Equal("Load template 'tpl-1' was not found.", ex.Message);
    }

    [Fact]
    public void BusinessValidationException_KeepsTheFieldMap()
    {
        var errors = new Dictionary<string, string[]> { ["Lines"] = new[] { "Unknown SKU code(s): NOPE." } };

        var ex = new BusinessValidationException(errors);

        Assert.Equal("One or more validation errors occurred.", ex.Message);
        Assert.Same(errors, ex.Errors);
    }

    private static List<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return results;
    }
}
