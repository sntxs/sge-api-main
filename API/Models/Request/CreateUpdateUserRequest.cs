namespace API.Models.Request
{
    public class CreateUpdateUserRequest
    {
        public required string Name { get; set; }
        public string? Email { get; set; } = null;
        public string? PhoneNumber { get; set; } = null;
        public required string Cpf { get; set; }
        public required string Username { get; set; }
        public string? Password { get; set; } = null;
        public bool IsAdmin { get; set; }
        public required Guid SectorId { get; set; }
    }
}