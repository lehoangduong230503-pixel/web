using System;
using System.Collections.Generic;

namespace ChocoLux.Models;

public partial class Address
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int CityId { get; set; }

    public string AddressDetail { get; set; } = null!;

    public bool IsDefault { get; set; }

    public virtual ShippingZone City { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
