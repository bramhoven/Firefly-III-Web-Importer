﻿using System;
using System.Linq;
using FireflyImporter.BusinessLayer.Firefly.Models;
using FireflyImporter.BusinessLayer.Nordigen.Models;

namespace FireflyImporter.BusinessLayer.Converters
{
    internal class INGConverter : ConverterBase
    {
        #region Fields

        private const string _descriptionFieldName = "Omschrijving";
        
        private const string _nameFieldName = "Naam";

        #endregion

        #region Methods

        /// <inheritdoc />
        protected override FireflyTransaction FillTransactions(FireflyTransaction fireflyTransaction, Transaction transaction)
        {
            fireflyTransaction.ExternalId = transaction.TransactionId;

            var (description, notes) = GetTextFields(transaction.RemittanceInformationUnstructured, transaction.CreditorName, transaction.DebtorName);
            fireflyTransaction.Description = description;
            fireflyTransaction.Notes = notes;

            return fireflyTransaction;
        }

        #region Static Methods

        private static (string, string) GetTextFields(string description, string creditorName, string debtorName)
        {
            var splitDescription = description.Split("<br>");
            var descriptionText = splitDescription.FirstOrDefault(d => d.Contains(_descriptionFieldName))?.Replace($"{_descriptionFieldName}:", "").Trim();
            
            if (string.IsNullOrWhiteSpace(descriptionText))
                descriptionText = splitDescription[0].Replace($"{_nameFieldName}:", "").Trim();
            
            if (string.IsNullOrWhiteSpace(descriptionText))
                descriptionText = creditorName;
            
            if (string.IsNullOrWhiteSpace(descriptionText))
                descriptionText = debtorName;
            
            var notesText = string.Join(Environment.NewLine, splitDescription);
            return (descriptionText, notesText);
        }

        #endregion

        #endregion
    }
}