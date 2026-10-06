using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WebBanHang.Models;

public partial class PCStoreContext : DbContext
{
    public PCStoreContext()
    {
    }

    public PCStoreContext(DbContextOptions<PCStoreContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Catalog> Catalogs { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<Product> Products { get; set; }
    public DbSet<Brand> Brands { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS01;Database=PCStore_update;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Account__3214EC07F2DB7574");

            entity.ToTable("Account");

            entity.HasIndex(e => e.Username, "UQ__Account__536C85E4DD6BC3B7").IsUnique();

            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Catalog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Catalog__3214EC07B0CDED45");

            entity.ToTable("Catalog");

            entity.HasIndex(e => e.CatalogCode, "UQ__Catalog__5BD51BA6B1867618").IsUnique();

            entity.Property(e => e.CatalogCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CatalogName).HasMaxLength(100);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Customer__3214EC07E2D2727A");

            entity.ToTable("Customer");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Account).WithMany(p => p.Customers)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK__Customer__Accoun__4D94879B");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Order__3214EC079EB14870");

            entity.ToTable("Order");

            entity.Property(e => e.CustomerPhone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ShippingAddress).HasMaxLength(255);

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK__Order__CustomerI__571DF1D5");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderDet__3214EC078926FA5F");

            entity.ToTable("OrderDetail");

            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__OrderDeta__Order__5AEE82B9");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__OrderDeta__Produ__5BE2A6F2");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Product__3214EC079F37CE69");

            entity.ToTable("Product");

            entity.HasIndex(e => e.ProductCode, "UQ__Product__2F4E024F35741CA8").IsUnique();

            entity.Property(e => e.Picture).HasMaxLength(255);
            entity.Property(e => e.ProductCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ProductName).HasMaxLength(150);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Catalog).WithMany(p => p.Products)
                .HasForeignKey(d => d.CatalogId)
                .HasConstraintName("FK__Product__Catalog__5441852A");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
