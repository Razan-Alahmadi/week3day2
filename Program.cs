using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        using (var context = new AppDbContext())
        {


            Console.WriteLine("1️⃣ Write a LINQ query using Entity Framework Core to find all employees who have worked on more than 3 projects in the last 6 months.");

            // LINQ query using EF Core to find employees with more than 3 projects in the last 6 months
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);

            var employees = context.EmployeeProjects
                .Where(ep => ep.Project.Deadline >= sixMonthsAgo)
                .GroupBy(ep => ep.EmployeeId)
                .Where(g => g.Count() > 3)
                .Select(g => g.Key)
                .ToList();

            var result = context.Employees
                .Where(e => employees.Contains(e.Id))
                .ToList();

            foreach (var emp in result)
            {
                Console.WriteLine($"Employee: {emp.Name}");
            }

            Console.WriteLine("2️⃣  Use Dapper to optimize a query that retrieves all employees along with their assigned projects, fetching only essential columns (EmployeeName, ProjectName,ProjectDeadline).");

            // Dapper Query to fetch employees with project details
            using (IDbConnection db = new SqlConnection(context.Database.GetConnectionString()))
            {
                var sql = @"
                    SELECT e.Name AS EmployeeName, p.Name AS ProjectName, p.Deadline AS ProjectDeadline
                    FROM Employees e
                    JOIN EmployeeProjects ep ON e.Id = ep.EmployeeId
                    JOIN Projects p ON ep.ProjectId = p.Id
                    WHERE p.Deadline >= @SixMonthsAgo";

                var employeesWithProjects = db.Query<EmployeeProjectDto>(sql, new { SixMonthsAgo = DateTime.Now.AddMonths(-6) }).ToList();

                foreach (var item in employeesWithProjects)
                {
                    Console.WriteLine($"Employee: {item.EmployeeName}, Project: {item.ProjectName}, Deadline: {item.ProjectDeadline:yyyy-MM-dd}");
                }
            }


            Console.WriteLine("3️⃣  Implement a stored procedure that calculates employee bonuses based on performance ratings and salary. Use Dapper to execute the stored procedure and return the results in your .NET Core application. ");
            string connectionString = "Server=localhost;Database=SchoolDB;Trusted_Connection=True;";
            // Improved stored procedure execution with Dapper
            using (var connection = new SqlConnection(connectionString))
            {
                string createProcedureQuery = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'CalculateBonuses') AND type in (N'P'))
                    BEGIN
                        EXEC('
                            CREATE PROCEDURE CalculateBonuses
                            AS
                            BEGIN
                                SELECT Id, Name, Salary, PerformanceRating,
                                       (Salary * (PerformanceRating * 0.05)) AS Bonus
                                FROM Employees;
                            END;');
                    END;";

                connection.Execute(createProcedureQuery);
                Console.WriteLine("\n ✅ Stored procedure 'CalculateBonuses' checked/created successfully.");

                // Compare EF Core and Dapper for fetching financial reports
                Console.WriteLine("\n4️⃣ Compare Entity Framework Core and Dapper for fetching financial reports(e.g., total department salaries and project budgets). Implement two versions (one with EF Core and one with Dapper) and analyze which one performs better for large datasets.");
                Console.WriteLine("---------- ");
                PerformanceComparison.CompareEfCoreAndDapper(context);
            }
        }
    }

    // DTO for Dapper query result
    public class EmployeeProjectDto
    {
        public string EmployeeName { get; set; }
        public string ProjectName { get; set; }
        public DateTime ProjectDeadline { get; set; }
    }

    // Financial Report DTO for performance comparison
    public class FinancialReport
    {
        public string DepartmentName { get; set; }
        public decimal TotalSalary { get; set; }
        public string ProjectName { get; set; }
        public decimal ProjectBudget { get; set; }
    }

    public static class PerformanceComparison
    {
        public static void CompareEfCoreAndDapper(AppDbContext context)
{
    var stopwatch = Stopwatch.StartNew();

    // Entity Framework Core execution
    var efCoreResults = context.Departments
        .Select(d => new FinancialReport
        {
            DepartmentName = d.Name,
            TotalSalary = d.Employees.Sum(e => e.Salary),
            ProjectName = d.Employees
                .SelectMany(e => e.EmployeeProjects)
                .Select(ep => ep.Project.Name)
                .FirstOrDefault(),
            ProjectBudget = d.Employees
                .SelectMany(e => e.EmployeeProjects)
                .Select(ep => ep.Project.Budget)
                .FirstOrDefault()
        }).ToList();

    stopwatch.Stop();
    long efCoreTime = stopwatch.ElapsedMilliseconds;
    Console.WriteLine($"1. Entity Framework Core: Execution Time: {efCoreTime} ms");

    foreach (var report in efCoreResults)
    {
        Console.WriteLine($"Department: {report.DepartmentName}, Total Salary: {report.TotalSalary}, Project: {report.ProjectName}, Budget: {report.ProjectBudget}");
    }

    Console.WriteLine("---------- ");

    // Dapper execution
    string connectionString = "Server=localhost;Database=SchoolDB;Trusted_Connection=True;";
    using (IDbConnection db = new SqlConnection(context.Database.GetConnectionString()))
    {
        stopwatch.Restart();

        var sql = @"
        SELECT 
            d.Name AS DepartmentName,
            SUM(e.Salary) AS TotalSalary,
            p.Name AS ProjectName,
            p.Budget AS ProjectBudget
        FROM Departments d
        JOIN Employees e ON d.Id = e.DepartmentId
        JOIN EmployeeProjects ep ON e.Id = ep.EmployeeId
        JOIN Projects p ON ep.ProjectId = p.Id
        GROUP BY d.Name, p.Name, p.Budget";

        var dapperResults = db.Query<FinancialReport>(sql).ToList();

        stopwatch.Stop();
        long dapperTime = stopwatch.ElapsedMilliseconds;
        Console.WriteLine($"2. Dapper: Execution Time: {dapperTime} ms");

        foreach (var report in dapperResults)
        {
            Console.WriteLine($"Department: {report.DepartmentName}, Total Salary: {report.TotalSalary}, Project: {report.ProjectName}, Budget: {report.ProjectBudget}");
        }

        Console.WriteLine("---------- ");

        Console.WriteLine($"Fastest one is: {(efCoreTime < dapperTime ? "Entity Framework Core" : "Dapper")}");
    }
}

    }
}
