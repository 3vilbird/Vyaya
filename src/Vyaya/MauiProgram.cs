using Microsoft.Extensions.Logging;
using ZXing.Net.Maui.Controls;
using Vyaya.Data;
using Vyaya.Services;
using Vyaya.ViewModels;
using Vyaya.Views;

namespace Vyaya;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Database & Repositories
        builder.Services.AddSingleton<AppDatabase>();
        builder.Services.AddSingleton<ICategoryRepository, CategoryRepository>();
        builder.Services.AddSingleton<IExpenseRepository, ExpenseRepository>();

        // Domain Services
        builder.Services.AddSingleton<IUpiParser, UpiParser>();
        builder.Services.AddSingleton<IUpiPaymentLauncher, UpiPaymentLauncher>();
        builder.Services.AddSingleton<ICategoryService, CategoryService>();
        builder.Services.AddSingleton<IExpenseService, ExpenseService>();
        builder.Services.AddSingleton<IExpenseReportService, ExpenseReportService>();
        builder.Services.AddSingleton<IExportService, ExportService>();

        // ViewModels
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<ScanViewModel>();
        builder.Services.AddTransient<ExpenseDetailsViewModel>();
        builder.Services.AddTransient<AddExpenseViewModel>();
        builder.Services.AddTransient<ExpensesViewModel>();
        builder.Services.AddTransient<ExpenseDetailViewModel>();
        builder.Services.AddTransient<MonthlySummaryViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // Views
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<ScanPage>();
        builder.Services.AddTransient<ExpenseDetailsPage>();
        builder.Services.AddTransient<AddExpensePage>();
        builder.Services.AddTransient<ExpensesPage>();
        builder.Services.AddTransient<ExpenseDetailPage>();
        builder.Services.AddTransient<MonthlySummaryPage>();
        builder.Services.AddTransient<SettingsPage>();

        return builder.Build();
    }
}
