using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NhaAnWeb.Models;

[Keyless]
[Table("attLog")]
public partial class AttLog
{
    [Column("employeeID")]
    [StringLength(100)]
    public string? EmployeeId { get; set; }

    [Column("authDateTime", TypeName = "datetime")]
    public DateTime? AuthDateTime { get; set; }

    [Column("authDate")]
    public DateOnly? AuthDate { get; set; }

    [Column("authTime")]
    public TimeOnly? AuthTime { get; set; }

    [Column("direction")]
    [StringLength(100)]
    public string? Direction { get; set; }

    [Column("deviceName")]
    [StringLength(100)]
    public string? DeviceName { get; set; }

    [Column("deviceSerialNo")]
    [StringLength(100)]
    public string? DeviceSerialNo { get; set; }

    [Column("personName")]
    [StringLength(100)]
    public string? PersonName { get; set; }

    [Column("cardNo")]
    [StringLength(100)]
    public string? CardNo { get; set; }
}
