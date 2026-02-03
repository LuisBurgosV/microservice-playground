using ClassicApi.Application.DTOs;
using ClassicApi.Application.Interfaces;
using ClassicApi.Infrastructure.Persistence;
using ClassicApi.Common.Mapping;
using Microsoft.EntityFrameworkCore;
using ClassicApi.Domain.Entities;

namespace ClassicApi.Infrastructure.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private const int DefaultPageSize = 10;
        private const int MaxPageSize = 100;

        public ClientRepository(AppDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<ClientDto> CreateClientAsync(BaseClientDto baseClientDto)
        {
            try
            {
                var client = baseClientDto.ToEntity();
                client.CreatedAt = DateTime.UtcNow;
                
                // Hash the password before storing
                client.Password = _passwordHasher.HashPassword(client.Password);

                _context.Clients.Add(client);
                var result = await _context.SaveChangesAsync();

                return result > 0 ? client.ToDto() : null;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error creating client in the database.", ex);
            }
        }

        public async Task<IEnumerable<ClientDto>> GetClientsByNameAsync(string clientName)
        {
            try
            {
                var clients = await _context.Clients
                    .Where(c => c.Name.Contains(clientName))
                    .AsNoTracking()
                    .ToListAsync();

                return clients.Select(c => c.ToDto());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving clients by name '{clientName}'.", ex);
            }
        }

        public async Task<bool> DeleteClientByIdAsync(int clientId)
        {
            try
            {
                var client = await _context.Clients.FindAsync(clientId);

                if (client == null)
                {
                    return false;
                }

                _context.Clients.Remove(client);
                var result = await _context.SaveChangesAsync();

                return result > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error deleting client with id {clientId}.", ex);
            }
        }

        public async Task<IEnumerable<ClientDto>> GetAllClientsAsync(int pageNumber = 1, int pageSize = DefaultPageSize)
        {
            try
            {
                var validPageNumber = pageNumber > 0 ? pageNumber : 1;
                var validPageSize = pageSize > 0 && pageSize <= MaxPageSize ? pageSize : DefaultPageSize;

                var clients = await _context.Clients
                    .AsNoTracking()
                    .Skip((validPageNumber - 1) * validPageSize)
                    .Take(validPageSize)
                    .ToListAsync();

                return clients.Select(c => c.ToDto());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error retrieving all clients.", ex);
            }
        }

        public async Task<ClientDto> GetClientByIdAsync(int clientId)
        {
            try
            {
                var client = await _context.Clients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == clientId);

                return client?.ToDto();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving client with id {clientId}.", ex);
            }
        }

        public async Task<ClientDto> UpdateClientByIdAsync(int clientId, BaseClientDto baseClientDto)
        {
            try
            {
                var existingClient = await _context.Clients.FindAsync(clientId);

                if (existingClient == null)
                {
                    return null;
                }

                existingClient.Name = baseClientDto.Name;
                existingClient.Surname = baseClientDto.Surname;
                existingClient.Email = baseClientDto.Email;
                existingClient.Password = _passwordHasher.HashPassword(baseClientDto.Password);
                existingClient.PhoneNumber = baseClientDto.PhoneNumber;
                existingClient.Address = baseClientDto.Address;
                existingClient.DateOfBirth = baseClientDto.DateOfBirth;
                existingClient.UpdatedAt = DateTime.UtcNow;

                _context.Clients.Update(existingClient);
                var result = await _context.SaveChangesAsync();

                return result > 0 ? existingClient.ToDto() : null;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error updating client with id {clientId}.", ex);
            }
        }
    }
}


