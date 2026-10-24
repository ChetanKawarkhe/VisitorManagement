using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisitorManagement.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }

    public int VisitorId { get; set; }

    public Visitor Visitor { get; set; } = null!;

    public string Action { get; set; } = string.Empty;

    public int? PerformedByUserId { get; set; }

    public User? PerformedByUser { get; set; }

    public string CorrelationId { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
