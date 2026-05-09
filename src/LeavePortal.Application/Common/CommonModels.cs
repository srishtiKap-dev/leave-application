namespace LeavePortal.Application.Common;

public sealed record ApiResponse<T>(bool Success, T? Data, string Message, IReadOnlyList<string> Errors)
{
    public static ApiResponse<T> Ok(T data, string message = "Success") => new(true, data, message, []);
    public static ApiResponse<T> Fail(string message, params string[] errors) => new(false, default, message, errors);
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)Math.Max(PageSize, 1));
}

public sealed record PageRequest(int Page = 1, int PageSize = 20)
{
    public int Skip => (Math.Max(Page, 1) - 1) * Math.Clamp(PageSize, 1, 100);
    public int Take => Math.Clamp(PageSize, 1, 100);
}
