using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using AMS.Domains.Data;

namespace AMS.Domains.Entities;

public class UserAccount
{
    [Key]
    public Guid Oid { get; set; }
    
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
    
    [JsonIgnore]
    public virtual IEnumerable<Class>Classes { get; set; }
}