﻿using System.Collections.Generic;
using System.Threading.Tasks;
using FireflyWebImporter.BusinessLayer.Firefly.Enums;
using FireflyWebImporter.BusinessLayer.Firefly.Helpers;
using FireflyWebImporter.BusinessLayer.Firefly.Models;
using FireflyWebImporter.BusinessLayer.Firefly.Models.Requests;
using FireflyWebImporter.BusinessLayer.Firefly.Models.Responses.Accounts;
using FireflyWebImporter.BusinessLayer.Firefly.Models.Responses.Transactions;
using FireflyWebImporter.BusinessLayer.Firefly.Models.Shared;
using Flurl.Http;
using Microsoft.Extensions.Logging;

namespace FireflyWebImporter.BusinessLayer.Firefly.Stores
{
    public class FireflyStore : IFireflyStore
    {
        #region Fields

        private readonly string _fireflyAccessToken;

        private readonly string _fireflyBaseUrl;

        private readonly ILogger<FireflyStore> _logger;

        #endregion

        #region Constructors

        public FireflyStore(string fireflyBaseUrl, string fireflyAccessToken, ILogger<FireflyStore> logger)
        {
            _fireflyBaseUrl = fireflyBaseUrl;
            _fireflyAccessToken = fireflyAccessToken;
            _logger = logger;
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        public async Task AddNewTransactions(IEnumerable<FireflyTransaction> transactions)
        {
            foreach (var transaction in transactions)
            {
                var request = GetCreateTransactionRequest(transaction);
                try
                {
                    var response = await FireflyRoutes
                          .Transactions(_fireflyBaseUrl)
                          .WithOAuthBearerToken(_fireflyAccessToken)
                          .WithHeader("Accept", "application/json")
                          .PostJsonAsync(request)
                          .ConfigureAwait(false);

                    _logger.LogInformation($"Imported transaction {transaction.Description}");
                }
                catch (FlurlHttpException e)
                {
                    if (e.StatusCode == 422)
                        _logger.LogError(await e.Call.Response.GetStringAsync());
                }
            }
        }

        /// <inheritdoc />
        public async Task<ICollection<FireflyAccount>> GetAccounts(AccountType accountType)
        {
            var response = await FireflyRoutes
                                 .Accounts(_fireflyBaseUrl)
                                 .WithOAuthBearerToken(_fireflyAccessToken)
                                 .WithHeader("Accept", "application/json")
                                 .SetQueryParam("type", MapToType(accountType))
                                 .GetJsonAsync<FireflyAccountResponse>()
                                 .ConfigureAwait(false);
            return response.Data.MapToFireflyAccountCollection();
        }

        /// <inheritdoc />
        public async Task<ICollection<FireflyAccount>> GetAccounts()
        {
            var accounts = new List<FireflyAccount>();
            FireflyAccountResponse response;
            int page = 0;

            do
            {
                response = await FireflyRoutes
                                 .Accounts(_fireflyBaseUrl)
                                 .WithOAuthBearerToken(_fireflyAccessToken)
                                 .WithHeader("Accept", "application/json")
                                 .SetQueryParam("page", ++page)
                                 .GetJsonAsync<FireflyAccountResponse>()
                                 .ConfigureAwait(false);
                accounts.AddRange(response.Data.MapToFireflyAccountCollection());
            } while (response.Meta.Pagination.CurrentPage < response.Meta.Pagination.TotalPages);

            return accounts;
        }

        /// <inheritdoc />
        public async Task<ICollection<FireflyTransaction>> GetTransactions()
        {
            var transactionResponse = await FireflyRoutes
                                            .Transactions(_fireflyBaseUrl)
                                            .WithOAuthBearerToken(_fireflyAccessToken)
                                            .GetJsonAsync<FireflyTransactionResponse>();
            return transactionResponse.Data.MapToFireflyTransactionCollection();
        }

        #region Static Methods

        private static FireflyStoreTransactionsRequest GetCreateTransactionRequest(FireflyTransaction transaction)
        {
            return new FireflyStoreTransactionsRequest
            {
                ApplyRules = true,
                FireWebhooks = true,
                GroupTitle = transaction.Description,
                ErrorIfDuplicateHash = false,
                Transactions = new List<FireflyApiTransaction> { transaction.MapToFireflyApiTransaction() }
            };
        }

        private static string MapToType(AccountType accountType)
        {
            switch (accountType)
            {
                case AccountType.Asset:
                    return "asset";
                case AccountType.Expense:
                    return "expense";
                case AccountType.Revenue:
                    return "revenue";
                case AccountType.Liability:
                    return "liability";
            }

            return string.Empty;
        }

        #endregion

        #endregion
    }
}