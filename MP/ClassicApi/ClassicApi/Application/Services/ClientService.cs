using ClassicApi.Application.DTOs;
using ClassicApi.Application.Interfaces;
using ClassicApi.Common.Exceptions;

namespace ClassicApi.Application.Services
{
    public class ClientService : IClientService
    {
        private readonly IClientRepository _clientRepository;
        
        public ClientService(IClientRepository clientRepository)
        {
            _clientRepository = clientRepository;
        }

        public async Task<ClientDto> GetClientByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                {
                    throw new ValidationException("Client id must be greater than zero.");
                }

                var client = await _clientRepository.GetClientByIdAsync(id);
                if (client == null)
                {
                    throw new NotFoundException($"Client with id {id} not found.");
                }
                return client;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException("An error occurred while retrieving the client.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An unexpected error occurred while retrieving the client.", ex);
            }
        }

        public async Task<IEnumerable<ClientDto>> GetClientsByNameAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ValidationException("Client name cannot be empty.");
                }

                var clients = await _clientRepository.GetClientsByNameAsync(name);
                if (clients == null || !clients.Any())
                {
                    throw new NotFoundException($"No clients found with name '{name}'.");
                }
                return clients;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException($"An error occurred while retrieving clients by name '{name}'.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An unexpected error occurred while retrieving clients.", ex);
            }
        }

        public async Task<IEnumerable<ClientDto>> GetAllClientsAsync(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                if (pageNumber <= 0)
                {
                    throw new ValidationException("Page number must be greater than zero.");
                }

                if (pageSize <= 0 || pageSize > 100)
                {
                    throw new ValidationException("Page size must be between 1 and 100.");
                }

                var clients = await _clientRepository.GetAllClientsAsync(pageNumber, pageSize);

                if (clients == null || !clients.Any())
                {
                    throw new NotFoundException($"No clients found on page {pageNumber}.");
                }

                return clients;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException("An error occurred while retrieving all clients.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An unexpected error occurred while retrieving all clients.", ex);
            }
        }

        public async Task<ClientDto> CreateClientAsync(BaseClientDto clientCreateDto)
        {
            try
            {
                if (clientCreateDto == null)
                {
                    throw new ValidationException("Client data cannot be null.");
                }

                ValidateClientData(clientCreateDto);

                var createdClient = await _clientRepository.CreateClientAsync(clientCreateDto);

                if (createdClient == null)
                {
                    throw new ValidationException("Client could not be created.");
                }

                return createdClient;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException("An error occurred while creating the client.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An unexpected error occurred while creating the client.", ex);
            }
        }

        public async Task<ClientDto> UpdateClientByIdAsync(int id, BaseClientDto clientUpdateDto)
        {
            try
            {
                if (id <= 0)
                {
                    throw new ValidationException("Client id must be greater than zero.");
                }

                if (clientUpdateDto == null)
                {
                    throw new ValidationException("Client data cannot be null.");
                }

                ValidateClientData(clientUpdateDto);

                var updatedClient = await _clientRepository.UpdateClientByIdAsync(id, clientUpdateDto);
                
                if (updatedClient == null)
                {
                    throw new NotFoundException($"Client with id {id} not found.");
                }
                
                return updatedClient;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException($"An error occurred while updating client with id {id}.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An unexpected error occurred while updating the client.", ex);
            }
        }

        public async Task<bool> DeleteClientByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                {
                    throw new ValidationException("Client id must be greater than zero.");
                }

                var isDeleted = await _clientRepository.DeleteClientByIdAsync(id);
                
                if (!isDeleted)
                {
                    throw new NotFoundException($"Client with id {id} not found.");
                }
                
                return true;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException($"An error occurred while deleting client with id {id}.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An unexpected error occurred while deleting the client.", ex);
            }
        }

        private static void ValidateClientData(BaseClientDto clientData)
        {
            if (string.IsNullOrWhiteSpace(clientData.Name))
            {
                throw new ValidationException("Client name cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(clientData.Surname))
            {
                throw new ValidationException("Client surname cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(clientData.Email))
            {
                throw new ValidationException("Client email cannot be empty.");
            }

            if (!IsValidEmail(clientData.Email))
            {
                throw new ValidationException("Client email format is invalid.");
            }

            if (string.IsNullOrWhiteSpace(clientData.Password))
            {
                throw new ValidationException("Client password cannot be empty.");
            }

            if (clientData.Password.Length < 6)
            {
                throw new ValidationException("Client password must be at least 6 characters long.");
            }

            if (clientData.DateOfBirth >= DateTime.Now)
            {
                throw new ValidationException("Client date of birth must be in the past.");
            }
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
