using API.Models.Request;
using API.Models.Response;
using MySql.Data.MySqlClient;
using System.Data;

namespace API.Services
{
    public class UserService
    {
        private readonly IConfiguration _configuration;

        public UserService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<List<GetUserResponse>> Get()
        {
            var users = new List<GetUserResponse>();

            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    
                    connection.Open();

                    string query = "SELECT a.*, b.Name AS SectorName, b.CreatedAt AS SectorCreatedAt FROM User a, Sector b WHERE a.SectorId = b.Id";

                    MySqlCommand cmd = new MySqlCommand(query, connection);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (await reader.ReadAsync())
                        {
                            var user = new GetUserResponse
                            {
                                Id = reader.GetGuid("Id"),
                                Name = reader.GetString("Name"),
                                Email = reader.GetString("Email"),
                                PhoneNumber = reader.GetString("PhoneNumber"),
                                Cpf = reader.GetString("Cpf"),
                                Username = reader.GetString("Username"),
                                IsAdmin = reader.GetBoolean("IsAdmin"),
                                Sector = new GetSectorReponse()
                                {
                                    Id = reader.GetGuid("SectorId"),
                                    Name = reader.GetString("SectorName"),
                                    CreatedAt = reader.GetDateTime("SectorCreatedAt")
                                },
                            };

                            users.Add(user);
                        }
                    }

                    return users;
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }

        public async Task<GetUserResponse> GetById(Guid id)
        {
            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT a.*, b.Name AS SectorName, b.CreatedAt AS SectorCreatedAt FROM User a, Sector b WHERE a.SectorId = b.Id AND a.Id = @Id";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", id);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (await reader.ReadAsync())
                        {
                            var user = new GetUserResponse
                            {
                                Id = reader.GetGuid("Id"),
                                Name = reader.GetString("Name"),
                                Email = reader.GetString("Email"),
                                PhoneNumber = reader.GetString("PhoneNumber"),
                                Cpf = reader.GetString("Cpf"),
                                Username = reader.GetString("Username"),
                                IsAdmin = reader.GetBoolean("IsAdmin"),
                                Sector = new GetSectorReponse()
                                {
                                    Id = reader.GetGuid("SectorId"),
                                    Name = reader.GetString("SectorName"),
                                    CreatedAt = reader.GetDateTime("SectorCreatedAt")
                                },
                            };

                            return user;
                        }

                        throw new Exception("Não foi possível encontrar o usuário.");
                    }
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }

        public async Task Create(CreateUpdateUserRequest request)
        {
            if (request.Email is not null && !Validator.IsValidEmail(request.Email))
                throw new Exception("E-mail inválido.");

            if (request.PhoneNumber is not null && (request.PhoneNumber.Length != 11 || Validator.ContainsLetter(request.PhoneNumber) || !Validator.IsValidPhoneNumber(request.PhoneNumber)))
                throw new Exception("Número de telefone inválido. Utilize somente números.");

            if (request.Cpf is null || request.Cpf.Length != 11 || !Validator.IsValidCpf(request.Cpf))
                throw new Exception("Número do CPF inválido. Utilize somente números.");

            if (request.Password is null || !Validator.IsValidPassword(request.Password))
                throw new Exception("Senha inválida.");

            //validar username (máximo de caracteres, letras, números)
            //validar nome (mínimo de tamanho)
            //pegar usuário que está CADASTRANDO e ver se é admin, caso o cadastrado for admin

            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();

                    // Verificar se o setor existe
                    string query = "SELECT 1 FROM Sector WHERE Id = @sectorId";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@sectorId", request.SectorId);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!await reader.ReadAsync())
                            throw new Exception("Setor não encontrado.");
                    }

                    // Inserir usuário com SectorId
                    query = "INSERT INTO User (Id, Name, Email, PhoneNumber, Cpf, Username, Password, IsAdmin, SectorId) VALUES " +
                            "(UUID(), @name, @email, @phoneNumber, @cpf, @username, @hashedPassword, @isAdmin, @sectorId)";
                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@name", request.Name);
                    cmd.Parameters.AddWithValue("@email", request.Email);
                    cmd.Parameters.AddWithValue("@phoneNumber", request.PhoneNumber);
                    cmd.Parameters.AddWithValue("@cpf", request.Cpf);
                    cmd.Parameters.AddWithValue("@username", request.Username);
                    cmd.Parameters.AddWithValue("@hashedPassword", BCrypt.Net.BCrypt.HashPassword(request.Password));
                    cmd.Parameters.AddWithValue("@isAdmin", request.IsAdmin);
                    cmd.Parameters.AddWithValue("@sectorId", request.SectorId);

                    await cmd.ExecuteNonQueryAsync();
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }

        public async Task Update(Guid id, CreateUpdateUserRequest request)
        {
            if (request.Email is not null && !Validator.IsValidEmail(request.Email))
                throw new Exception("E-mail inválido.");

            if (request.PhoneNumber is not null && (request.PhoneNumber.Length != 11 || Validator.ContainsLetter(request.PhoneNumber) || !Validator.IsValidPhoneNumber(request.PhoneNumber)))
                throw new Exception("Número de telefone inválido. Utilize somente números.");

            if (request.Cpf is null || request.Cpf.Length != 11 || !Validator.IsValidCpf(request.Cpf))
                throw new Exception("Número do CPF inválido. Utilize somente números.");

            //validar username (máximo de caracteres, letras, números)
            //validar nome (mínimo de tamanho)
            //pegar usuário que está EDITANDO e ver se é admin, caso o editado for admin

            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        string query = "SELECT 1 FROM User WHERE Username = @username AND Id <> @id";
                        using (var cmd = new MySqlCommand(query, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@username", request.Username);
                            cmd.Parameters.AddWithValue("@id", id);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                    throw new Exception("Nome de usuário já está sendo usado.");
                            }
                        }

                        query = "SELECT 1 FROM User WHERE Cpf = @cpf AND Id <> @id";
                        using (var cmd = new MySqlCommand(query, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@cpf", request.Cpf);
                            cmd.Parameters.AddWithValue("@id", id);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                    throw new Exception("Número de CPF já está sendo usado.");
                            }
                        }

                        if (request.Email is not null)
                        {
                            query = "SELECT 1 FROM User WHERE Email = @email AND Id <> @id";
                            using (var cmd = new MySqlCommand(query, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@email", request.Email);
                                cmd.Parameters.AddWithValue("@id", id);
                                using (var reader = await cmd.ExecuteReaderAsync())
                                {
                                    if (await reader.ReadAsync())
                                        throw new Exception("Este e-mail já está sendo usado.");
                                }
                            }
                        }

                        if (request.PhoneNumber is not null)
                        {
                            query = "SELECT 1 FROM User WHERE PhoneNumber = @phoneNumber AND Id <> @id";
                            using (var cmd = new MySqlCommand(query, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@phoneNumber", request.PhoneNumber);
                                cmd.Parameters.AddWithValue("@id", id);
                                using (var reader = await cmd.ExecuteReaderAsync())
                                {
                                    if (await reader.ReadAsync())
                                        throw new Exception("Este telefone já está sendo usado.");
                                }
                            }
                        }

                        query = "UPDATE User SET Name = @name, Email = @email, PhoneNumber = @phoneNumber, Cpf = @cpf, Username = @username, IsAdmin = @isAdmin, SectorId = @sectorId " +
                                "WHERE Id = @id";
                        using (var cmd = new MySqlCommand(query, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@name", request.Name);
                            cmd.Parameters.AddWithValue("@email", request.Email);
                            cmd.Parameters.AddWithValue("@phoneNumber", request.PhoneNumber);
                            cmd.Parameters.AddWithValue("@cpf", request.Cpf);
                            cmd.Parameters.AddWithValue("@username", request.Username);
                            cmd.Parameters.AddWithValue("@isAdmin", request.IsAdmin);
                            cmd.Parameters.AddWithValue("@sectorId", request.SectorId);
                            cmd.Parameters.AddWithValue("@id", id);

                            await cmd.ExecuteNonQueryAsync();
                        }

                        await transaction.CommitAsync();
                    }
                    catch (MySqlException ex)
                    {
                        await transaction.RollbackAsync();
                        throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                    }
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

                    string query = "DELETE FROM User WHERE Id = @id";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", id);

                    var result = await cmd.ExecuteNonQueryAsync();

                    if (result != 1)
                        throw new Exception("Não foi possível excluir o usuário. Caso o problema persistir, contate o administrador.");
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }

        public async Task ChangePassword(ChangePasswordRequest request)
        {
            if (request.NewPassword is null || !Validator.IsValidPassword(request.NewPassword))
                throw new Exception("Nova senha inválida.");

            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT Password FROM User WHERE Id = @userId";
                    
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@userId", request.UserId);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (await reader.ReadAsync())
                        {
                            string storedHash = reader.GetString("Password");
                            
                            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, storedHash))
                                throw new Exception("Senha atual incorreta.");
                        }
                        else
                            throw new Exception("Usuário não encontrado.");
                    }

                    query = "UPDATE User SET Password = @newPassword WHERE Id = @userId";
                    
                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@newPassword", BCrypt.Net.BCrypt.HashPassword(request.NewPassword));
                    cmd.Parameters.AddWithValue("@userId", request.UserId);

                    int result = await cmd.ExecuteNonQueryAsync();

                    if (result != 1)
                        throw new Exception("Não foi possível atualizar a senha. Caso o problema persistir, contate o administrador.");
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }
    }
}
