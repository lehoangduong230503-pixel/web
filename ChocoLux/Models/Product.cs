using System;
using System.Collections.Generic;

namespace ChocoLux.Models;

public partial class Product
{
    public int Id { get; set; }

    public string Sku { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? DescriptionVi { get; set; }

    public decimal Price { get; set; }

    public string? MainImage { get; set; }

    public int? Weight { get; set; }

    public int SoldCount { get; set; }

    public int TotalStock { get; set; }

    public int? CacaoPercent { get; set; }

    public string? StorageCondition { get; set; }

    public string? StorageConditionVi { get; set; }

    public int CategoryId { get; set; }

    public int OriginId { get; set; }

    public bool IsActive { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Origin Origin { get; set; } = null!;
}
