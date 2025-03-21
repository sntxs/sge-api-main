using API.Models.Request;
using API.Models.Response;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace API.Services
{
    public class SectorService
    {
        private readonly IConfiguration _configuration;

        public SectorService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task Create(CreateUpdateSectorRequest request)
        {
            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT 1 FROM Sector WHERE Name = @name";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@name", request.Name);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (await reader.ReadAsync())
                            throw new Exception("Nome de Setor já está sendo utilizado.");
                    }

                    query = "INSERT INTO Sector (Id, Name, CreatedAt) VALUES (UUID(), @name, NOW())";
                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@name", request.Name);

                    await cmd.ExecuteNonQueryAsync();
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }

        public async Task<List<GetSectorReponse>> Get()
        {
            var items = new List<GetSectorReponse>();

            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT * FROM Sector";

                    MySqlCommand cmd = new MySqlCommand(query, connection);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = new GetSectorReponse
                            {
                                Id = reader.GetGuid("Id"),
                                Name = reader.GetString("Name"),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                            };

                            items.Add(item);
                        }
                    }
                    return items;
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }
        
        public async Task<GetSectorReponse> GetById(Guid id)
        {
            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT * FROM Sector WHERE id = @id";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", id);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (await reader.ReadAsync())
                        {
                            var product = new GetSectorReponse
                            {
                                Id = reader.GetGuid("Id"),
                                Name = reader.GetString("Name"),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                            };

                            return product;
                        }

                        throw new Exception("Não foi possível encontrar o Setor.");
                    }
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }

        public async Task Update(Guid id, CreateUpdateSectorRequest request)
        {
            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT 1 FROM Sector WHERE Name = @name AND Id <> @id";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@name", request.Name);
                    cmd.Parameters.AddWithValue("@id", id);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (await reader.ReadAsync())
                            throw new Exception("Nome de Setor já está sendo usado.");
                    }

                    query = "UPDATE Sector SET Name = @name WHERE Id = @id";

                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@name", request.Name);
                    cmd.Parameters.AddWithValue("@id", id);

                    int result = await cmd.ExecuteNonQueryAsync();

                    if (result != 1)
                        throw new Exception("Não foi possível atualizar o Setor. Caso o problema persistir, contate o administrador.");
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }

        public async Task Delete(Guid id)
        {
            //pegar usuário que está DELETANDO e ver se é admin

            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();

                    string query = "DELETE FROM Sector WHERE Id = @id";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", id);

                    var result = await cmd.ExecuteNonQueryAsync();

                    if (result != 1)
                        throw new Exception("Não foi possível excluir o Setor. Caso o problema persistir, contate o administrador.");
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }

    }
}
