using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace B2BWholesale.Portal.Models;

[Table("Agent")]
public class Agent
{
    [Key]
    public int AgentID { get; set; }

    [Required, MaxLength(150)]
    public string AgentName { get; set; } = "";

    [Required, MaxLength(255)]
    public string Address { get; set; } = "";

    [Required, MaxLength(20)]
    public string Phone { get; set; } = "";
}
