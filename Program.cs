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

            //
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
                Console.WriteLine("\nStored procedure 'CalculateBonuses' checked/created successfully.");

            // Compare EF Core and Dapper for fetching financial reports
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
        Console.WriteLine("\n🛑Comparing EF Core and Dapper for financial reports...🛑");

        // 1. Entity Framework Core
        var stopwatch = Stopwatch.StartNew();

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
        Console.WriteLine($"EF Core Execution Time: {stopwatch.ElapsedMilliseconds} ms");

        foreach (var report in efCoreResults)
        {
            Console.WriteLine($"Department: {report.DepartmentName}, Total Salary: {report.TotalSalary}, Project: {report.ProjectName}, Budget: {report.ProjectBudget}");
        }

        // 2. Dapper
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
            Console.WriteLine($"Dapper Execution Time: {stopwatch.ElapsedMilliseconds} ms");

            foreach (var report in dapperResults)
            {
                Console.WriteLine($"Department: {report.DepartmentName}, Total Salary: {report.TotalSalary}, Project: {report.ProjectName}, Budget: {report.ProjectBudget}");
            }
        }
    }
}
