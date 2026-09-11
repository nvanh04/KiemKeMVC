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
        public string SapCode { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Workshop { get; set; }
        public string Machine { get; set; }
        public decimal Quantity { get; set; }
        public int Tubes { get; set; }
        public string Note { get; set; }
    }

    public class SaveSheetRequest
    {
        public string Workshop { get; set; }
        public string DeviceId { get; set; }
        public string Date { get; set; }
        public string CreatedBy { get; set; }
        public List<SheetItemDto> Items { get; set; }
    }

    public class EditSheetRequest
    {
        public string SheetId { get; set; }
        public string Workshop { get; set; }
        public string Date { get; set; }
        public string CreatedBy { get; set; }
        public List<SheetItemDto> Items { get; set; }
    }

    public class MaterialDto
    {
        public string OriginalSapCode { get; set; }
        public string SapCode { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Workshop { get; set; }
        public string Aliases { get; set; }
    }

    public class BatchExcelCategoryDto
    {
        public string Workshop { get; set; }
        public string Name { get; set; }
    }

    public class BatchExcelMachineDto
    {
        public string Workshop { get; set; }
        public string Name { get; set; }
    }

    public class ImportExcelCategoriesRequest
    {
        public List<BatchExcelCategoryDto> Categories { get; set; }
    }

    public class ImportExcelMachinesRequest
    {
        public List<BatchExcelMachineDto> Machines { get; set; }
    }

    public class BatchMaterialRequest
    {
        public List<MaterialDto> Materials { get; set; }
    }

    public class DeleteBatchSheetsRequest
    {
        public List<string> SheetIds { get; set; }
    }

    public class BatchCategoryDto
    {
        public string Workshop { get; set; }
        public string Names { get; set; }
    }

    public class BatchMachineDto
    {
        public string Workshop { get; set; }
        public string Names { get; set; }
    }
}
