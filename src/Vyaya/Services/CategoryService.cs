using Vyaya.Data;
using Vyaya.Models;

namespace Vyaya.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;
    private List<Category>? _cachedCategories;

    public CategoryService(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        if (_cachedCategories == null || _cachedCategories.Count == 0)
        {
            _cachedCategories = await _repository.GetAllAsync();
        }
        return _cachedCategories;
    }

    public async Task<List<Category>> SearchCategoriesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetCategoriesAsync();
        }

        return await _repository.SearchAsync(query);
    }

    public async Task<Category?> GetCategoryByNameAsync(string name)
    {
        var all = await GetCategoriesAsync();
        return all.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<Category> GetDefaultCategoryAsync()
    {
        var all = await GetCategoriesAsync();
        return all.FirstOrDefault(c => c.Name.Equals("Other", StringComparison.OrdinalIgnoreCase)) 
               ?? all.FirstOrDefault() 
               ?? new Category { Name = "Other", Icon = "📦", ColorHex = "#8E8E93" };
    }

    public async Task<Category> AddCustomCategoryAsync(string name, string icon, string colorHex)
    {
        var category = new Category
        {
            Name = name.Trim(),
            Icon = string.IsNullOrWhiteSpace(icon) ? "📁" : icon,
            ColorHex = string.IsNullOrWhiteSpace(colorHex) ? "#007AFF" : colorHex,
            IsDefault = false,
            Keywords = name.Trim().ToLowerInvariant()
        };

        await _repository.InsertAsync(category);
        _cachedCategories = null; // Invalidate cache
        return category;
    }
}
