namespace VisitorManagement.Web.Models.Visitors;

public sealed class VisitorSearchRequest
{
    public string? Search { get; set; }

    public string SortBy { get; set; } = "VisitDate";

    public bool Descending { get; set; } = true;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}