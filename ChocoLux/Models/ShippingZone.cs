using System;
using System.Collections.Generic;

namespace ChocoLux.Models;

public partial class ShippingZone
{
    public int Id { get; set; }

    public string CityName { get; set; } = null!;

    public string CityNameVi { get; set; } = null!;

    public decimal Fee { get; set; }

    public string DistanceGroup { get; set; } = null!;

    public string DistanceGroupVi { get; set; } = null!;

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
}
