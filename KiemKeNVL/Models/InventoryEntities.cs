using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace KiemKeNVL.Models
{
    [Table("Workshops")]
    public class Workshop
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; }
        [Required, MaxLength(50)]
        public string Password { get; set; }
    }

    [Table("Categories")]
    public class Category
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; }
        [Required, MaxLength(100)]
        public string Workshop { get; set; }
    }

    [Table("Machines")]
    public class Machine
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; }
        [Required, MaxLength(100)]
        public string Workshop { get; set; }
    }

    [Table("Materials")]
    public class Material
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(50)]
        public string SapCode { get; set; }
        [Required, MaxLength(250)]
        public string Name { get; set; }
        [MaxLength(250)]
        public string Description { get; set; }
        [Required, MaxLength(100)]
        public string Category { get; set; }
        [Required, MaxLength(100)]
        public string Workshop { get; set; }
        [MaxLength(500)]
        public string Aliases { get; set; }
    }

    [Table("InventorySheets")]
    public class InventorySheet
    {
        [Key]
        public string Id { get; set; }
        [Required, MaxLength(100)]
        public string Workshop { get; set; }
        [MaxLength(100)]
        public string DeviceId { get; set; }
        [Required, MaxLength(20)]
        public string Date { get; set; }
        [MaxLength(100)]
        public string Machine { get; set; }
        [Required, MaxLength(100)]
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<InventorySheetItem> Items { get; set; } = new List<InventorySheetItem>();
    }

    [Table("InventorySheetItems")]
    public class InventorySheetItem
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string SheetId { get; set; }
        [Required, MaxLength(50)]
        public string SapCode { get; set; }
        [Required, MaxLength(250)]
        public string Name { get; set; }
        [MaxLength(100)]
        public string Category { get; set; }
        [MaxLength(100)]
        public string Workshop { get; set; }
        [MaxLength(100)]
        public string Machine { get; set; }
        public decimal Quantity { get; set; }
        public int Tubes { get; set; }
        [MaxLength(250)]
        public string Note { get; set; }

        [ForeignKey("SheetId")]
        public virtual InventorySheet Sheet { get; set; }
    }

    public class KiemKeDbContext : DbContext
    {
        public KiemKeDbContext() : base("name=KiemKeDbContext") { }

        public DbSet<Workshop> Workshops { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Machine> Machines { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<InventorySheet> InventorySheets { get; set; }
        public DbSet<InventorySheetItem> InventorySheetItems { get; set; }
    }

    public class SheetItemDto
    {
        public string sapCode { get; set; }
        public string name { get; set; }
        public string category { get; set; }
        public string workshop { get; set; }
        public string machine { get; set; }
        public decimal quantity { get; set; }
        public int tubes { get; set; }
        public string note { get; set; }
    }

    public class SaveSheetRequest
    {
        public string workshop { get; set; }
        public string deviceId { get; set; }
        public string date { get; set; }
        public string createdBy { get; set; }
        public List<SheetItemDto> items { get; set; }
    }
}