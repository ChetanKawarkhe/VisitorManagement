using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisitorManagement.Application.DTOs.Visitors;

public sealed class VisitorResponse
{
    public int Id { get; set; }

    public string VisitorName { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string MobileNumber { get; set; } = string.Empty;

    public string? Email { get; set; }

    public DateTime VisitDate { get; set; }

    public int HostId { get; set; }

    public string HostName { get; set; } = string.Empty;

    public int PurposeId { get; set; }

    public string PurposeName { get; set; } = string.Empty;

    public string? Remarks { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string RowVersion { get; set; } = string.Empty;
}