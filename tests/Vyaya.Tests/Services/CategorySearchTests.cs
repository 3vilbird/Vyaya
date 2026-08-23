using Vyaya.Data;
using Vyaya.Models;
using Vyaya.Services;
using Xunit;

namespace Vyaya.Tests.Services;

public class CategorySearchTests
{
    private readonly CategoryService _categoryService;
    private readonly MockCategoryRepository _repo;

    public CategorySearchTests()
    {
        _repo = new MockCategoryRepository();
        _categoryService = new CategoryService(_repo);
    }

    [Fact]
    public async Task SearchCategories_WithTrav_ReturnsTravel()
    {
        var results = await _categoryService.SearchCategoriesAsync("trav");
        Assert.NotEmpty(results);
        Assert.Contains(results, c => c.Name == "Travel");
    }

    [Fact]
    public async Task SearchCategories_CaseInsensitive_ReturnsMatches()
    {
        var results = await _categoryService.SearchCategoriesAsync("FOOD");
        Assert.NotEmpty(results);
        Assert.Contains(results, c => c.Name == "Food");
    }

    [Fact]
    public async Task GetDefaultCategory_ReturnsOtherOrDefault()
    {
        var defaultCat = await _categoryService.GetDefaultCategoryAsync();
        Assert.NotNull(defaultCat);
        Assert.Equal("Other", defaultCat.Name);
    }
}
