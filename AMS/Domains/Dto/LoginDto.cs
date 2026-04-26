using System.ComponentModel.DataAnnotations;

namespace AMS.Domains.Dto;

public class LoginDto
{
    [Required]
    [StringLength(100)]
    public string Username { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Password { get; set; }
}