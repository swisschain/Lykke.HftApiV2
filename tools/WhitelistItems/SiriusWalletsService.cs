using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Google.Protobuf.WellKnownTypes;
using Lykke.Common.Log;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Swisschain.Sirius.Api.ApiClient;
using Swisschain.Sirius.Api.ApiContract.Account;
using Swisschain.Sirius.Api.ApiContract.User;
using Swisschain.Sirius.Api.ApiContract.WhitelistItems;

namespace WhitelistItems
{
    public class SiriusWalletsService
    {
        private readonly ILog _log;
        private readonly IApiClient _siriusApiClient;
        private readonly long _brokerAccountId;
        private readonly IEnumerable<TimeSpan> _delay;

        public SiriusWalletsService(AppSettings settings, ILog log)
        {
            _log = log;
            _siriusApiClient = new ApiClient(settings.SiriusApiGrpcUrl, settings.SiriusApiKey);
            _brokerAccountId = settings.BrokerAccountId;
            _delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromMilliseconds(100), retryCount: 7, fastFirst: true);
        }

        public async Task CreateWalletAsync(string clientId, string walletId)
        {
            var accountSearchResponse = await SearchAccountAsync(clientId, walletId);

            if (accountSearchResponse == null)
            {
                var message = "Error getting account from sirius.";

                _log.Warning(message, context: new { clientId, walletId });

                throw new Exception(message);
            }

            if (accountSearchResponse.Body.Items.Count == 0)
            {
                string accountRequestId = $"{_brokerAccountId}_{walletId}_account";
                string userRequestId = $"{clientId}_user";

                var userCreateResponse = await CreateUserAsync(clientId, userRequestId);

                if (userCreateResponse == null)
                {
                    var message = "Error creating user in sirius.";
                    _log.Warning(message, context: new { clientId, requestId = userRequestId });
                    throw new Exception(message);
                }

                var createResponse = await CreateAccountAsync(walletId, userCreateResponse.User.Id, accountRequestId);

                if (createResponse == null)
                {
                    var message = "Error creating account in sirius.";
                    _log.Warning(message, context: new { clientId, requestId = accountRequestId });
                    throw new Exception(message);
                }

                var whitelistItemCreateResponse = await CreateWhitelistItemAsync(clientId, walletId, createResponse.Body.Account.Id);

                if (whitelistItemCreateResponse == null)
                {
                    var message = "Error creating whitelist item.";
                    _log.Warning(message, context: new { clientId, walletId });
                    throw new Exception(message);
                }
            }
            else
            {
                var hftDepositWhitelistItems = await GetDepostiWhitelistItemsAsync(accountSearchResponse.Body.Items.First().Id, walletId);

                if (hftDepositWhitelistItems == null)
                {
                    var message = "Error getting whitelist items.";
                    _log.Warning(message, context: new { accountId = accountSearchResponse.Body.Items.First().Id, walletId });
                    throw new Exception(message);
                }

                if (hftDepositWhitelistItems.WhitelistItems.Items.Count == 0)
                {
                    var whitelistItemCreateResponse = await CreateWhitelistItemAsync(clientId, walletId, accountSearchResponse.Body.Items.First().Id);

                    if (whitelistItemCreateResponse == null)
                    {
                        var message = "Error creating whitelist item.";
                        _log.Warning(message, context: new { clientId, walletId });
                        throw new Exception(message);
                    }
                }
            }
        }

        private async Task<AccountSearchResponse> SearchAccountAsync(string clientId, string walletId = null)
        {
            var retryPolicy = Policy
                .Handle<Exception>(ex =>
                {
                    _log.Warning($"Retry on Exception: {ex.Message}.", ex, new { clientId });
                    return true;
                })
                .OrResult<AccountSearchResponse>(response =>
                    {
                        if (response.ResultCase == AccountSearchResponse.ResultOneofCase.Error)
                        {
                            _log.Warning("Response from sirius.", context: response.ToJson());
                        }

                        return response.ResultCase == AccountSearchResponse.ResultOneofCase.Error;
                    }
                )
                .WaitAndRetryAsync(_delay);

            try
            {
                var result = await retryPolicy.ExecuteAsync(async () => await _siriusApiClient.Accounts.SearchAsync(new AccountSearchRequest
                {
                    BrokerAccountId = _brokerAccountId, UserNativeId = clientId, ReferenceId = walletId ?? clientId
                }));

                if (result.ResultCase != AccountSearchResponse.ResultOneofCase.Error)
                    return result;

                _log.Error(message: $"Error getting account from sirius: {result.Error.ErrorMessage}.", context: new { clientId, walletId });
                return null;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error getting account from sirius.", new { clientId });
                return null;
            }
        }

        private async Task<CreateUserResponse> CreateUserAsync(string clientId, string userRequestId)
        {
            var retryPolicy = Policy
                .Handle<Exception>(ex =>
                {
                    _log.Warning($"Retry on Exception: {ex.Message}.", ex, new { clientId, requestId = userRequestId });
                    return true;
                })
                .OrResult<CreateUserResponse>(response =>
                    {
                        if (response.BodyCase == CreateUserResponse.BodyOneofCase.Error)
                        {
                            _log.Warning("Response from sirius.", context: response.ToJson());
                        }

                        return response.BodyCase == CreateUserResponse.BodyOneofCase.Error;
                    }
                )
                .WaitAndRetryAsync(_delay);

            try
            {
                _log.Info("Creating user in sirius.", new { clientId, requestId = userRequestId });

                var result = await retryPolicy.ExecuteAsync(async () => await _siriusApiClient.Users.CreateAsync(
                    new CreateUserRequest { RequestId = userRequestId, NativeId = clientId }
                ));

                if (result.BodyCase != CreateUserResponse.BodyOneofCase.Error)
                    return result;

                _log.Error(message: $"Error creating user in sirius: {result.Error.ErrorMessage}.", context: new { clientId, requestId = userRequestId });
                return null;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error creating user in sirius.", new { clientId, requestId = userRequestId });
                return null;
            }
        }

        private async Task<AccountCreateResponse> CreateAccountAsync(string walletId, long userId, string accountRequestId)
        {
            var retryPolicy = Policy
                .Handle<Exception>(ex =>
                {
                    _log.Warning($"Retry on Exception: {ex.Message}.", ex, new { walletId, userId, requestId = accountRequestId });
                    return true;
                })
                .OrResult<AccountCreateResponse>(response =>
                    {
                        if (response.ResultCase == AccountCreateResponse.ResultOneofCase.Error)
                        {
                            _log.Warning("Response from sirius.", context: response.ToJson());
                        }

                        return response.ResultCase == AccountCreateResponse.ResultOneofCase.Error;
                    }
                )
                .WaitAndRetryAsync(_delay);

            try
            {
                _log.Info("Creating account in sirius.", new { clientId = walletId, requestId = accountRequestId });

                var result = await retryPolicy.ExecuteAsync(async () => await _siriusApiClient.Accounts.CreateAsync(new AccountCreateRequest
                    {
                        RequestId = accountRequestId, BrokerAccountId = _brokerAccountId, UserId = userId, ReferenceId = walletId
                    }
                ));

                if (result.ResultCase == AccountCreateResponse.ResultOneofCase.Error)
                {
                    _log.Error(message: $"Error creating account in sirius: {result.Error.ErrorMessage}.", context: new { clientId = walletId, userId, requestId = accountRequestId });
                    return null;
                }

                _log.Info("Account created in sirius.", new { account = result.Body.Account,
                    clientId = walletId, requestId = accountRequestId });

                return result;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error creating account in sirius.", new { clientId = walletId, requestId = accountRequestId });
                return null;
            }
        }

        private async Task<WhitelistItemCreateResponse> CreateWhitelistItemAsync(string clientId, string walletId, long accountId)
        {
            var requestId = $"lykke:hft:deposit:{clientId}:{walletId}";

            var retryPolicy = Policy
                .Handle<Exception>(ex =>
                {
                    _log.Warning($"Retry on Exception: {ex.Message}.", ex, new { requestId, clientId, walletId, accountId });
                    return true;
                })
                .OrResult<WhitelistItemCreateResponse>(response =>
                    {
                        if (response.BodyCase == WhitelistItemCreateResponse.BodyOneofCase.Error)
                        {
                            _log.Warning("Response from sirius.", context: response.ToJson());
                        }

                        return response.BodyCase == WhitelistItemCreateResponse.BodyOneofCase.Error;
                    }
                )
                .WaitAndRetryAsync(_delay);

            try
            {
                _log.Info("Creating whitelist item in sirius.", context: new { requestId, clientId, walletId, accountId });

                var request = new WhitelistItemCreateRequest
                {
                    Name = "HFT Wallet Deposits Whitelist",
                    Scope = new WhitelistItemScope
                    {
                        BrokerAccountId = _brokerAccountId,
                        AccountId = accountId,
                        UserNativeId = clientId
                    },
                    Details = new WhitelistItemDetails
                    {
                        TransactionType = WhitelistTransactionType.Deposit,
                        TagType = new NullableWhitelistItemTagType { Null = NullValue.NullValue }
                    },
                    Lifespan = new WhitelistItemLifespan { StartsAt = Timestamp.FromDateTime(DateTime.UtcNow) },
                    RequestId = requestId
                };

                var result = await retryPolicy.ExecuteAsync(async () => await _siriusApiClient.WhitelistItems.CreateAsync(request));

                if (result.BodyCase == WhitelistItemCreateResponse.BodyOneofCase.Error)
                {
                    _log.Error(message: $"Error creating whitelist item in sirius: {result.Error.ErrorMessage}", context: new { requestId = request.RequestId, clientId = request.Scope.UserNativeId, accountId = request.Scope.AccountId });
                    return null;
                }

                _log.Info("Whitelist item created in sirius.",
                    context: new { whitelistItemId = result.WhitelistItem.Id, requestId = request.RequestId, clientId = request.Scope.UserNativeId, accountId = request.Scope.AccountId });

                return result;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error creating whitelist item in sirius." , new { requestId, clientId, walletId, accountId });
                return null;
            }
        }

        private async Task<WhitelistItemsSearchResponse> GetDepostiWhitelistItemsAsync(long accountId, string walletId)
        {
            var retryPolicy = Policy
                .Handle<Exception>(ex =>
                {
                    _log.Warning($"Retry on Exception: {ex.Message}.", ex, new { accountId, walletId });
                    return true;
                })
                .OrResult<WhitelistItemsSearchResponse>(response =>
                    {
                        if (response.BodyCase == WhitelistItemsSearchResponse.BodyOneofCase.Error)
                        {
                            _log.Warning("Response from sirius.", context: response.ToJson());
                        }

                        return response.BodyCase == WhitelistItemsSearchResponse.BodyOneofCase.Error;
                    }
                )
                .WaitAndRetryAsync(_delay);

            try
            {
                var result = await retryPolicy.ExecuteAsync(async () => await _siriusApiClient.WhitelistItems.SearchAsync(new WhitelistItemSearchRequest
                {
                    AccountId = accountId,
                    AccountReferenceId = walletId,
                    TransactionType = { WhitelistTransactionType.Deposit },
                    IsRemoved = false
                }));

                if (result.BodyCase == WhitelistItemsSearchResponse.BodyOneofCase.Error)
                {
                    _log.Error(message: $"Error getting whitelist items from sirius: {result.Error.ErrorMessage}", context: new { accountId, walletId });
                    return null;
                }

                return result;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error getting whitelist item from sirius." , new { accountId, walletId });
                return null;
            }
        }
    }
}
