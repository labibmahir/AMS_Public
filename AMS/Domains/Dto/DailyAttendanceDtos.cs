using System.ComponentModel.DataAnnotations;

namespace AMS.Domains.Dto;

public sealed class AttendanceStudentListDto
{
    public Guid StudentId { get; set; }
    public string FullName { get; set; }
    public string StudentIdString { get; set; }
    public string ImagePath { get; set; }
    public bool IsPresent { get; set; }
}

public sealed class ToggleAttendanceDto
{
    public Guid StudentId { get; set; }
    public int ClassId { get; set; }
    public DateTime AttendanceDate { get; set; }
    public bool IsPresent { get; set; }
}
