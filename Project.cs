public class Project
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime Deadline { get; set; }
    public decimal Budget { get; set; } 
    public List<EmployeeProject> EmployeeProjects { get; set; } = new List<EmployeeProject>();
}
