using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.HftApi.Domain.Entities;
using Lykke.HftApi.Domain.Entities.DepositWallets;
using Lykke.HftApi.Domain.Services;
using Swisschain.Sirius.Api.ApiClient;
using Swisschain.Sirius.Api.ApiContract.Account;
using Swisschain.Sirius.Api.ApiContract.User;

namespace Lykke.HftApi.Services
{
    public class SiriusWalletsService : ISiriusWalletsService
    {
        private readonly long _brokerAccountId;
        private readonly IApiClient _siriusApiClient;
        private readonly ILog _log;

        public SiriusWalletsService(
            long brokerAccountId,
            IApiClient siriusApiClient,
            ILogFactory logFactory)
        {
            _brokerAccountId = brokerAccountId;
            _siriusApiClient = siriusApiClient;
            _log = logFactory.CreateLog(this);
        }

        public async Task CreateWalletAsync(string clientId, string walletId)
        {
            var accountSearchResponse = await _siriusApiClient.Accounts.SearchAsync(new AccountSearchRequest
            {
                BrokerAccountId = _brokerAccountId,
                UserNativeId = clientId,
                ReferenceId = walletId
            });

            if (accountSearchResponse.ResultCase == AccountSearchResponse.ResultOneofCase.Error)
            {
                var message = "Error fetching Sirius Account";
                _log.Warning(nameof(CreateWalletAsync),
                    message,
                    context: new
                    {
                        error = accountSearchResponse.Error,
                        walletId,
                        clientId
                    });
                throw new Exception(message);
            }

            if (!accountSearchResponse.Body.Items.Any())
            {
                string accountRequestId = $"{_brokerAccountId}_{walletId}_account";
                string userRequestId = $"{clientId}_user";

                var userCreateResponse = await _siriusApiClient.Users.CreateAsync(new CreateUserRequest
                {
                    RequestId = userRequestId,
                    NativeId = clientId
                });

                if (userCreateResponse.BodyCase == CreateUserResponse.BodyOneofCase.Error)
                {
                    var message = "Error creating User in Sirius";
                    _log.Warning(nameof(CreateWalletAsync),
                    message,
                    context: new
                    {
                        error = userCreateResponse.Error,
                        clientId,
                        requestId = userRequestId
                    });
                    throw new Exception(message);
                }

                var createResponse = await _siriusApiClient.Accounts.CreateAsync(new AccountCreateRequest
                {
                    RequestId = accountRequestId,
                    BrokerAccountId = _brokerAccountId,
                    UserId = userCreateResponse.User.Id,
                    ReferenceId = walletId
                });

                if (createResponse.ResultCase == AccountCreateResponse.ResultOneofCase.Error)
                {
                    var message = "Error creating user in sirius";
                    _log.Warning(nameof(CreateWalletAsync),
                    message,
                    context: new
                    {
                        error = createResponse.Error,
                        clientId,
                        requestId = accountRequestId
                    });
                    throw new Exception(message);
                }
            }
        }

        public async Task<DepositWallet> GetWalletAddressAsync(string clientId, string walletId, long siriusAssetId)
        {
            var accountSearchResponse = await _siriusApiClient.Accounts.SearchAsync(new AccountSearchRequest
            {
                BrokerAccountId = _brokerAccountId,
                UserNativeId = clientId,
                ReferenceId = walletId
            });

            if (accountSearchResponse.ResultCase == AccountSearchResponse.ResultOneofCase.Error)
            {
                var message = "Error fetching Sirius Account";
                _log.Warning(nameof(GetWalletAddressAsync),
                    message,
                    context: new
                    {
                        error = accountSearchResponse.Error,
                        walletId,
                        clientId
                    });
                throw new Exception(message);
            }

            if(!accountSearchResponse.Body.Items.Any())
            {
                return new DepositWallet {State = DepositWalletState.NotFound};
            }

            var account = accountSearchResponse.Body.Items.Single();

            if (account.State == AccountStateModel.Creating)
            {
                return new DepositWallet {State = DepositWalletState.Creating};
            }
            else if(account.State == AccountStateModel.Blocked)
            {
                return new DepositWallet {State = DepositWalletState.Blocked};
            } else if (account.State == AccountStateModel.Active)
            {
                var accountDetailsResponse = await _siriusApiClient.Accounts.GetDetailsAsync(new AccountGetDetailsRequest
                {
                    AccountId = account.Id,
                    AssetId = siriusAssetId
                });

                if (accountDetailsResponse.ResultCase == AccountGetDetailsResponse.ResultOneofCase.Error)
                {
                    var message = "Error fetching Sirius Account details";
                    _log.Warning(nameof(GetWalletAddressAsync),
                        message,
                        context: new
                        {
                            error = accountSearchResponse.Error,
                            walletId,
                            clientId
                        });
                    throw new Exception(message);
                }

                return new DepositWallet
                {
                    State = DepositWalletState.Active,
                    Address =
                        string.IsNullOrEmpty(accountDetailsResponse.Body.AccountDetail.Tag)
                            ? accountDetailsResponse.Body.AccountDetail.Address
                            : $"{accountDetailsResponse.Body.AccountDetail.Address}+{accountDetailsResponse.Body.AccountDetail.Tag}",
                    BaseAddress = accountDetailsResponse.Body.AccountDetail.Address,
                    AddressExtension = accountDetailsResponse.Body.AccountDetail.Tag
                };
            }
            else
            {
                var message = $"Unknown State for Account {account.Id}: {account.State.ToString()}";
                _log.Warning(nameof(GetWalletAddressAsync),
                    message,
                    context: new
                    {
                        error = accountSearchResponse.Error,
                        walletId,
                        clientId
                    });
                throw new Exception(message);
            }
        }
    }
}
