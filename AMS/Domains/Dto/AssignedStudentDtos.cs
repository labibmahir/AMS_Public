namespace AMS.Domains.Dto;

public sealed class AssignedStudentListDto
{
    public Guid Oid { get; set; }
    public Guid StudentId { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string StudentIdString { get; set; } = "";
}

public sealed class AssignedStudentCreateDto
{
    public Guid StudentId { get; set; }
    public int ClassId { get; set; }
}
