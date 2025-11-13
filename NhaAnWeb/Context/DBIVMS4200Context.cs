using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using NhaAnWeb.Models;

namespace NhaAnWeb.Context;

public partial class DBIVMS4200Context : DbContext
{
    public DBIVMS4200Context()
    {
    }

    public DBIVMS4200Context(DbContextOptions<DBIVMS4200Context> options)
        : base(options)
    {
    }

    public virtual DbSet<AttLog> AttLogs { get; set; }

    public virtual DbSet<GatePf> GatePfs { get; set; }

    public virtual DbSet<NhanVien> NhanViens { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=10.0.193.252;Initial Catalog=IVMS4200;Persist Security Info=True;User ID=sa;Password=Zpvn@2022;Encrypt=True;Trust Server Certificate=True");



    //    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
    //        => optionsBuilder.UseSqlServer("Data Source=WIN-H8S0A6L31JG\\SQLEXPRESS;Initial Catalog=IVMS4200;Persist Security Info=True;User ID=sa;Password=Credible@1357;Encrypt=True;Trust Server Certificate=True");


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GatePf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GatePF__3214EC273610DA26");
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__NhanVien__3214EC277D7FF395");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
