namespace FirstLend.Application.Dtos.Response
{
    public class ServiceResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Code { get; set; } = "";
        public T? Data { get; set; }
        public object? Errors { get; set; }
        public int? TotalCount { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }
}
