namespace API.Models.Response
{
    public class AuthResponse
    {
        public string Token { get; set; }
        public string TokenExpires { get; set; }
        public Guid Id { get; set; }
    }
}
