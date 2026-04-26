using System.ComponentModel.DataAnnotations;
using AMS.Domains.Data;

namespace AMS.Domains.Dto;

public class RegisterDto
{
    [Required]
    [StringLength(250)]
    public string FullName { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Username { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Password { get; set; }
    
    [Required]
    public Enums.UserType UserType { get; set; }
}