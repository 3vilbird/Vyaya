using Vyaya.Models;

namespace Vyaya.Services;

public interface ICategoryService
{
    Task<List<Category>> GetCategoriesAsync();
    Task<List<Category>> SearchCategoriesAsync(string query);
    Task<Category?> GetCategoryByNameAsync(string name);
    Task<Category> GetDefaultCategoryAsync();
    Task<Category> AddCustomCategoryAsync(string name, string icon, string colorHex);
}
