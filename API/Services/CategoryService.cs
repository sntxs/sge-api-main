using API.Models.Request;
using API.Models.Response;
using MySql.Data.MySqlClient;

namespace API.Services
{
    public class CategoryService
    {
        private readonly IConfiguration _configuration;

        public CategoryService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task Create(CreateUpdateCategoryRequest request)
        {
            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT 1 FROM Category WHERE Name = @name";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@name", request.Name);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (await reader.ReadAsync())
                            throw new Exception("Nome de Categoria já está sendo utilizado.");
                    }

                    query = "INSERT INTO Category (Id, Name, CreatedAt) VALUES (UUID(), @name, NOW())";
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

        public async Task<List<GetCategoryResponse>> Get()
        {
            var items = new List<GetCategoryResponse>();

            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT * FROM Category";

                    MySqlCommand cmd = new MySqlCommand(query, connection);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = new GetCategoryResponse
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
        
        public async Task<GetCategoryResponse> GetById(Guid id)
        {
            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT * FROM Category WHERE id = @id";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", id);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (await reader.ReadAsync())
                        {
                            var category = new GetCategoryResponse
                            {
                                Id = reader.GetGuid("Id"),
                                Name = reader.GetString("Name"),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                            };

                            return category;
                        }

                        throw new Exception("Não foi possível encontrar a Categoria.");
                    }
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }

        public async Task Update(Guid id, CreateUpdateCategoryRequest request)
        {
            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT 1 FROM Category WHERE Name = @name AND Id <> @id";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@name", request.Name);
                    cmd.Parameters.AddWithValue("@id", id);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (await reader.ReadAsync())
                            throw new Exception("Nome de Categoria já está sendo usado.");
                    }

                    query = "UPDATE Category SET Name = @name WHERE Id = @id";

                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@name", request.Name);
                    cmd.Parameters.AddWithValue("@id", id);

                    int result = await cmd.ExecuteNonQueryAsync();

                    if (result != 1)
                        throw new Exception("Não foi possível atualizar a Categoria. Caso o problema persistir, contate o administrador.");
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }

        public async Task Delete(Guid id)
        {
            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();

                    string query = "DELETE FROM Category WHERE Id = @id";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", id);

                    var result = await cmd.ExecuteNonQueryAsync();

                    if (result != 1)
                        throw new Exception("Não foi possível excluir a Categoria. Caso o problema persistir, contate o administrador.");
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }
    }
} 