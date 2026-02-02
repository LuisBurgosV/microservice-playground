using ClassicApi.Application.DTOs;
using ClassicApi.Application.Interfaces;
using ClassicApi.Domain.Entities;
using ClassicApi.Infrastructure.Persistence;

namespace ClassicApi.Infrastructure.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly AppDbContext _context;

        public ClientRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<ClientDto> CreateClientAsync(BaseClientDto BaseClientDto)
        {
            // TODO: Map BaseClientDto to Client entity using mappings
            Client client = new()
            {
                Name = BaseClientDto.Name,
                Surname = BaseClientDto.Surname,
                Email = BaseClientDto.Email,
                Password = BaseClientDto.Password,
                PhoneNumber = BaseClientDto.PhoneNumber,
                Address = BaseClientDto.Address,
                DateOfBirth = BaseClientDto.DateOfBirth,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            _context.Clients.Add(client);

            var result = _context.SaveChanges();

            if (result <= 0)
            {
                return Task.FromResult<ClientDto>(null);
            }

            // TODO: Map Client entity to ClientDto using mappings
            return Task.FromResult(new ClientDto
            {
                Id = _context.Clients.Max(c => c.Id),
                Name = client.Name,
                Surname = client.Surname,
                Email = client.Email,
                Password = client.Password,
                PhoneNumber = client.PhoneNumber,
                Address = client.Address,
                DateOfBirth = client.DateOfBirth
            });
        }

        public Task<bool> DeleteClientByIdAsync(int clientId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ClientDto>> GetAllClientsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<ClientDto> GetClientByIdAsync(int clientId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ClientDto>> GetClientsByNameAsync(string clientName)
        {
            throw new NotImplementedException();
        }

        public Task<ClientDto> UpdateClientByIdAsync(int clientId, BaseClientDto BaseClientDto)
        {
            throw new NotImplementedException();
        }
    }
}
