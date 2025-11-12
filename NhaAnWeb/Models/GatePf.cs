using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NhaAnWeb.Models;

[Table("GatePF")]
public partial class GatePf
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("G_Name")]
    [StringLength(100)]
    public string? GName { get; set; }

    [StringLength(255)]
    public string? Detail { get; set; }
}
