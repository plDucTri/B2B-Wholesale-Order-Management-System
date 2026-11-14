using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace B2BWholesale.Portal.Models;

[Table("Users")]
public class User
{
    [Key]
    public int UserID { get; set; }

    [Required, MaxLength(50)]
    public string UserName { get; set; } = "";

    [Required, MaxLength(150)]
    public string Email { get; set; } = "";

    /// <summary>PBKDF2 hash in the format PBKDF2$iterations$salt$hash (shared with the WinForms app).</summary>
    [Required, MaxLength(255)]
    public string Password { get; set; } = "";

    public bool IsLocked { get; set; }

    [Required, MaxLength(20)]
    public string Role { get; set; } = "";

    public int? AgentID { get; set; }
    public Agent? Agent { get; set; }
}
