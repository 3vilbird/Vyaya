using Vyaya.Models;

namespace Vyaya.Data;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync();
    Task<List<Category>> SearchAsync(string query);
    Task<Category?> GetByNameAsync(string name);
    Task<Category?> GetByIdAsync(int id);
    Task<int> InsertAsync(Category category);
    Task<int> UpdateAsync(Category category);
}
