using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisitorManagement.Application.DTOs.Visitors;

public sealed class UpdateVisitorRequest
{
    public string VisitorName { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string MobileNumber { get; set; } = string.Empty;

    public string? Email { get; set; }

    public DateTime VisitDate { get; set; }

    public int HostId { get; set; }

    public int PurposeId { get; set; }

    public string? Remarks { get; set; }

    public string Status { get; set; } = "Scheduled";

    public string RowVersion { get; set; } = string.Empty;
}