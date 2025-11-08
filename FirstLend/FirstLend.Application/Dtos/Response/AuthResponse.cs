namespace FirstLend.Application.Dtos.Response
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Code { get; set; } = "";
        public object? Data { get; set; }
        public List<ValidationError>? Errors { get; set; }
    }

    public class ValidationError
    {
        public string Field { get; set; } = "";
        public string Message { get; set; } = "";
    }
}