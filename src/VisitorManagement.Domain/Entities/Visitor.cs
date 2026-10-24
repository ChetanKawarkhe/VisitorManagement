using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VisitorManagement.Domain.Enums;

namespace VisitorManagement.Domain.Entities;

public class Visitor
{
    public int Id { get; set; }

    public string VisitorName { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string MobileNumber { get; set; } = string.Empty;

    public string? Email { get; set; }

    public DateTime VisitDate { get; set; }

    public int HostId { get; set; }

    public Host Host { get; set; } = null!;

    public int PurposeId { get; set; }

    public VisitPurpose Purpose { get; set; } = null!;

    public string? Remarks { get; set; }

    public VisitorStatus Status { get; set; } = VisitorStatus.Scheduled;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int CreatedByUserId { get; set; }

    public User CreatedByUser { get; set; } = null!;

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedByUserId { get; set; }

    public User? UpdatedByUser { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}