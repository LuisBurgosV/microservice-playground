using ClassicApi.Application.DTOs;

namespace ClassicApi.Application.Interfaces
{
    public interface IClientRepository
    {
        // GET
        Task<ClientDto> GetClientByIdAsync(int clientId);

        Task<IEnumerable<ClientDto>> GetClientsByNameAsync(string clientName);

        Task<IEnumerable<ClientDto>> GetAllClientsAsync();

        // POST
        Task<ClientDto> CreateClientAsync(ClientCreateDto clientCreateDto);

        // PUT
        Task<ClientDto> UpdateClientByIdAsync(int clientId, ClientUpdateDto clientUpdateDto);

        // DELETE
        Task<bool> DeleteClientByIdAsync(int clientId);
    }
}
