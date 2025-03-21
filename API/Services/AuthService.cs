using API.Models.Request;
using API.Models.Response;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace API.Services
{
    public class AuthService
    {
        private readonly IConfiguration _configuration;

        public AuthService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<AuthResponse> Auth(AuthRequest request)
        {
            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();

                    string query = @"
                    SELECT Id, Password FROM User WHERE Username = @username";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@username", request.Username);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (await reader.ReadAsync())
                        {
                            string storedHash = reader["Password"].ToString();

                            if (BCrypt.Net.BCrypt.Verify(request.Password, storedHash))
                            {
                                var (token, expiration) = GenerateJwtToken(request.Username);

                                return new AuthResponse
                                {
                                    Token = token,
                                    TokenExpires = expiration.ToString("dd/MM/yyyy HH:mm:ss"),
                                    Id = reader.GetGuid("Id"),
                                };
                            }
                            else
                                throw new Exception("Usuário ou senha inválidos.");
                        }
                        else
                            throw new Exception("Usuário ou senha inválidos.");
                    }
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }

        private (string token, DateTime expiresAt) GenerateJwtToken(string username)
        {
            var key = Encoding.ASCII.GetBytes(_configuration["JwtTokenKey"]);

            var tokenHandler = new JwtSecurityTokenHandler();
            var expiration = DateTime.Now.AddHours(8);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
            new Claim(ClaimTypes.Name, username)
                }),
                Expires = expiration,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return (tokenString, expiration);
        }

    }
}
