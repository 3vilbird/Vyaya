using SQLite;
using Vyaya.Models;

namespace Vyaya.Data;

public class AppDatabase
{
    private readonly SQLiteAsyncConnection _database;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isInitialized;

    public AppDatabase(string? dbPath = null)
    {
        var path = dbPath ?? Path.Combine(FileSystem.AppDataDirectory, "vyaya_expenses.db3");
        
        // Ensure directory exists
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _database = new SQLiteAsyncConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
    }

    public SQLiteAsyncConnection Connection => _database;

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized)
                return;

            await _database.CreateTableAsync<Expense>();
            await _database.CreateTableAsync<Category>();

            // Create helpful indices
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_expense_date ON Expenses(ExpenseDateUtc);");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_expense_category ON Expenses(Category);");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_expense_status ON Expenses(Status);");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_expense_method ON Expenses(PaymentMethod);");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_category_name ON Categories(Name);");

            await SeedDefaultCategoriesAsync();

            _isInitialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task SeedDefaultCategoriesAsync()
    {
        var count = await _database.Table<Category>().CountAsync();
        if (count > 0)
            return;

        var defaultCategories = new List<Category>
        {
            new() { Name = "Food", Icon = "🍔", ColorHex = "#FF9500", IsDefault = true, Keywords = "food lunch dinner breakfast snacks canteen" },
            new() { Name = "Vegetables", Icon = "🥦", ColorHex = "#34C759", IsDefault = true, Keywords = "vegetables veggies sabzi fruit mandi market" },
            new() { Name = "Groceries", Icon = "🛒", ColorHex = "#30B0C7", IsDefault = true, Keywords = "groceries supermarket provisions milk bread eggs ration" },
            new() { Name = "Restaurants", Icon = "🍽️", ColorHex = "#FF2D55", IsDefault = true, Keywords = "restaurants dining cafe zomato swiggy eat out" },
            new() { Name = "Travel", Icon = "✈️", ColorHex = "#007AFF", IsDefault = true, Keywords = "travel flight train uber ola taxi auto metro bus ride" },
            new() { Name = "Fuel", Icon = "⛽", ColorHex = "#FF9500", IsDefault = true, Keywords = "fuel petrol diesel cng gas station" },
            new() { Name = "Shopping", Icon = "🛍️", ColorHex = "#AF52DE", IsDefault = true, Keywords = "shopping clothes amazon flipkart shoes mall retail" },
            new() { Name = "Bills", Icon = "🧾", ColorHex = "#5856D6", IsDefault = true, Keywords = "bills recharge mobile dth postpaid broadband wifi" },
            new() { Name = "Utilities", Icon = "💡", ColorHex = "#FFCC00", IsDefault = true, Keywords = "utilities electricity water cylinder gas maintenance" },
            new() { Name = "Entertainment", Icon = "🎬", ColorHex = "#FF3B30", IsDefault = true, Keywords = "entertainment movies cinema games netflix prime hotstar party" },
            new() { Name = "Health", Icon = "💊", ColorHex = "#34C759", IsDefault = true, Keywords = "health medicine doctor pharmacy hospital dental clinic" },
            new() { Name = "Education", Icon = "📚", ColorHex = "#5856D6", IsDefault = true, Keywords = "education books tuition courses school college fees" },
            new() { Name = "Rent", Icon = "🏠", ColorHex = "#8E8E93", IsDefault = true, Keywords = "rent house pg flat accommodation lease" },
            new() { Name = "Subscriptions", Icon = "📱", ColorHex = "#007AFF", IsDefault = true, Keywords = "subscriptions icloud spotify youtube prime software" },
            new() { Name = "Other", Icon = "📦", ColorHex = "#8E8E93", IsDefault = true, Keywords = "other miscellaneous general cash expense" }
        };

        await _database.InsertAllAsync(defaultCategories);
    }
}
