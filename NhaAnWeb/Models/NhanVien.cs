using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NhaAnWeb.Models;

[Table("NhanVien")]
public partial class NhanVien
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [StringLength(255)]
    public string? Ten { get; set; }

    [StringLength(255)]
    public string? TenDangNhap { get; set; }

    [StringLength(255)]
    public string? MatKhau { get; set; }

    public bool? TrangThai { get; set; }

    public int? ChucVu { get; set; }
}
