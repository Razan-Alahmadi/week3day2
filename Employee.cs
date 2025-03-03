public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int DepartmentId { get; set; }
    public Department Department { get; set; }
    public decimal Salary { get; set; }  
    public double PerformanceRating { get; set; }
    public List<EmployeeProject> EmployeeProjects { get; set; } = new List<EmployeeProject>();
}
