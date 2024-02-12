using Expence_App.Dto;
using Expence_App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;


namespace Expence_App.Controllers
{
    public class DashboardController : Controller
    {

        private readonly ApplicationDbContext _context;
        public DashboardController(ApplicationDbContext context)
        {
            _context= context;
        }
        public async Task<ActionResult> Index()
        {
            //Last 7 Days
            DateTime StartDate = DateTime.Today.AddDays(-6);
            DateTime EndDate = DateTime.Today;

            List<Transaction> SelectedTransaction = await _context.Transactions
                .Include(x=>x.Category).Where(y=>y.Date >= StartDate &&y.Date<=EndDate).ToListAsync();
           


            //Total Income 


            int TotalIncome = SelectedTransaction.Where(i=>i.Category.Type =="Income").Sum(j=>j.Amount);
            ViewBag.TotalIncome = TotalIncome.ToString("C0");

            //Total Expense
            int TotalExpense = SelectedTransaction.Where(i => i.Category.Type == "Expense").Sum(j => j.Amount);
            ViewBag.TotalExpense = TotalExpense.ToString("C0");

            //Balance

            int Balance = TotalIncome - TotalExpense;

            CultureInfo culture= CultureInfo.CreateSpecificCulture("en-IN");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            ViewBag.Balance = String.Format(culture,"{0:C0}",Balance);


            //Doughtnut Chart For Expense

            ViewBag.DoughtnutChartData = SelectedTransaction.Where(i => i.Category.Type == "Expense")
                .GroupBy(i => i.Category.CategoryId)
                .Select(k=>
                new
                {   categoryTitleWithIcon= k.First().Category.Icon+" " + k.First().Category.Title,
                    amount = k.Sum(j=>j.Amount),
                    formattedAmount= k.Sum(j => j.Amount).ToString("C0"),

                }).OrderByDescending(l=>l.amount)
                .ToList();


            //SplineChart Expense vs Income
            //Income


            List<SplineChartData> IncomeSummary = SelectedTransaction
                .Where(i => i.Category.Type == "Income")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day =(string) $"{k.First().Date:dd-MMM}",
                    income = k.Sum(i => i.Amount)

                }).ToList();

            //Expense
            List<SplineChartData> ExpenseSummary = SelectedTransaction
               .Where(i => i.Category.Type == "Expense")
               .GroupBy(j => j.Date)
               .Select(k => new SplineChartData()
               {
                   day =(string) $"{k.First().Date:dd-MMM}",
                   expense = k.Sum(i => i.Amount)

               }).ToList();


            //Combine Income and Expense
            string[] Last7Days = Enumerable.Range(0, 7)
                                .Select(i => StartDate.AddDays(i).ToString("dd-MMM"))
                                .ToArray();

            ViewBag.SplineChart = from day in Last7Days
                                  join income in IncomeSummary on day equals income.day
                                 into dayIncomeJoined
                                  from income in dayIncomeJoined.DefaultIfEmpty()
                                  join expense in ExpenseSummary
                                 on day equals expense.day into expenseJoined
                                  from expense in dayIncomeJoined.DefaultIfEmpty()
                                  select new
                                  {
                                      day = day,
                                      income = income == null ? 0 : income.income,
                                      expense = expense == null ? 0 : expense.expense,

                                  };
            ViewBag.RecentTransactions = await _context.Transactions
               .Include(i => i.Category)
               .OrderByDescending(j => j.Date)
               .Take(5)
               .ToListAsync();



            ;
            return View();

        }
    }
}

