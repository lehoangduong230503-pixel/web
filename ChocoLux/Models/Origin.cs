using System;
using System.Collections.Generic;

namespace ChocoLux.Models;

public partial class Origin
{
    public int Id { get; set; }

    public string CountryName { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
