using System;
using System.Collections.Generic;

namespace ChocoLux.Models;

public partial class Payment
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string PaymentStatus { get; set; } = null!;

    public DateTime? PaidAt { get; set; }

    public bool CodReceived { get; set; }

    public DateTime? CodReceivedAt { get; set; }

    public virtual Order Order { get; set; } = null!;
}
