namespace VisitorManagement.Web.Models.Visitors;

public sealed class CreateVisitorRequest
{
    public string VisitorName { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string MobileNumber { get; set; } = string.Empty;

    public string? Email { get; set; }

    public DateTime VisitDate { get; set; }

    public int HostId { get; set; }

    public int PurposeId { get; set; }

    public string? Remarks { get; set; }
}