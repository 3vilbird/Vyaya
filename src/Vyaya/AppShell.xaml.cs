using Vyaya.Views;

namespace Vyaya;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("addExpense", typeof(AddExpensePage));
        Routing.RegisterRoute("expenseDetail", typeof(ExpenseDetailPage));
    }
}
