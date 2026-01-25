using ClassicApi.Application.DTOs;

namespace ClassicApi.Application.Interfaces
{
    public interface IClientService
    {
        Task<ClientDto> GetClientByIdAsync(int id);

        Task<IEnumerable<ClientDto>> GetClientsByNameAsync(string name);

        Task<IEnumerable<ClientDto>> GetAllClientsAsync();

        Task<ClientDto> CreateClientAsync(BaseClientDto BaseClientDto);

        Task<ClientDto> UpdateClientByIdAsync(int id, BaseClientDto BaseClientDto);

        Task<bool> DeleteClientByIdAsync(int id);
    }
}
