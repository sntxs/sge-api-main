namespace API.Models.Response
{
    public class GetUserResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Cpf { get; set; }
        public string Username { get; set; }
        public bool IsAdmin { get; set; }
        public GetSectorReponse Sector { get; set; }
    }
}
