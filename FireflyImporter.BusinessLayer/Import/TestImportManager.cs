﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FireflyImporter.BusinessLayer.Configuration.Interfaces;
using FireflyImporter.BusinessLayer.Firefly;
using FireflyImporter.BusinessLayer.Import.Mappers;
using FireflyImporter.BusinessLayer.Logger;
using FireflyImporter.BusinessLayer.Nordigen;
using FireflyImporter.BusinessLayer.Nordigen.Models;

namespace FireflyImporter.BusinessLayer.Import
{
    public class TestImportManager : ImportManagerBase
    {
        #region Constructors

        public TestImportManager(INordigenManager nordigenManager,
                                 IFireflyManager fireflyManager,
                                 IImportConfiguration importConfiguration,
                                 ICompositeLogger<TestImportManager> logger) : base(nordigenManager, fireflyManager, importConfiguration, logger) { }

        #endregion

        #region Methods

        /// <inheritdoc />
        public override async ValueTask StartImport(CancellationToken cancellationToken)
        {
            var existingFireflyTransactions = await GetExistingFireflyTransactions();
            var requisitions = await GetRequisitions();

            await Logger.LogInformation($"Start import for {requisitions.Count} connected banks");

            var firstImport = !existingFireflyTransactions.Any();
            var balances = new Dictionary<string, string>();
            var newTransactions = new List<Transaction>();
            foreach (var requisition in requisitions)
            {
                foreach (var account in requisition.Accounts)
                {
                    newTransactions.AddRange(await GetTransactionForRequisitionAccount(account, requisition));

                    var details = await NordigenManager.GetAccountDetails(account);
                    var balance = await NordigenManager.GetAccountBalance(account);
                    balances.Add(details.Iban, balance.FirstOrDefault()?.BalanceAmount.Amount);
                }
            }

            await Logger.LogInformation($"Retrieved a total of {newTransactions.Count} transactions");

            var accounts = await FireflyManager.GetAccounts();
            var newFireflyTransactions = TransactionMapper.MapTransactionsToFireflyTransactions(newTransactions, accounts, null).ToList();

            newFireflyTransactions = (await RemoveExistingTransactions(newFireflyTransactions, existingFireflyTransactions)).ToList();
            newFireflyTransactions = (await CheckForDuplicateTransfers(newFireflyTransactions, existingFireflyTransactions, balances.Keys)).ToList();

            if (!newFireflyTransactions.Any())
            {
                await Logger.LogInformation("No new transactions to import");
                return;
            }

            await Logger.LogInformation($"Would import {newFireflyTransactions.Count} transactions");

            if (!firstImport)
                return;

            await Logger.LogInformation("Would set asset account opening balances");
        }

        #endregion
    }
}