﻿/*
 * Copyright (c) 2013, salesforce.com, inc.
 * All rights reserved.
 * Redistribution and use of this software in source and binary forms, with or
 * without modification, are permitted provided that the following conditions
 * are met:
 * - Redistributions of source code must retain the above copyright notice, this
 * list of conditions and the following disclaimer.
 * - Redistributions in binary form must reproduce the above copyright notice,
 * this list of conditions and the following disclaimer in the documentation
 * and/or other materials provided with the distribution.
 * - Neither the name of salesforce.com, inc. nor the names of its contributors
 * may be used to endorse or promote products derived from this software without
 * specific prior written permission of salesforce.com, inc.
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Data.Json;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Adaptation;
using Salesforce.SDK.Source.Security;
using Salesforce.SDK.Source.Settings;
using Windows.Foundation.Diagnostics;

namespace Salesforce.SDK.Auth
{
    /// <summary>
    ///     Store specific implementation if IAuthStorageHelper
    /// </summary>
    ///
    public sealed class AuthStorageHelper
    {
        private const string PasswordVaultAccounts = "Salesforce Accounts";
        private const string PasswordVaultCurrentAccount = "Salesforce Account";
        private const string PasswordVaultSecuredData = "Salesforce Secure";
        private const string PasswordVaultEncryptionSettings = "Salesforce Encryption Settings";
        private const string InstallationStatusKey = "InstallationStatus";

        private static readonly Lazy<AuthStorageHelper> Auth = new Lazy<AuthStorageHelper>(() => new AuthStorageHelper());
        private readonly ApplicationDataContainer _persistedData;
        private readonly PasswordVault _vault;
        private readonly object _retrieveCurrentAccountLock = new object();

        private AuthStorageHelper()
        {
            _vault = new PasswordVault();
            _persistedData = ApplicationData.Current.LocalSettings;
            InstallationStatusCheck();
        }

        public static AuthStorageHelper GetAuthStorageHelper()
        {
            return Auth.Value;
        }

        private void InstallationStatusCheck()
        {
            if (!_persistedData.Values.ContainsKey(InstallationStatusKey))
            {
                IReadOnlyList<PasswordCredential> accounts = _vault.RetrieveAll();
                foreach (PasswordCredential next in accounts)
                {
                    _vault.Remove(next);
                }
                _persistedData.Values.Add(InstallationStatusKey, "");
            }
        }

        private IEnumerable<PasswordCredential> SafeRetrieveResource(string resource)
        {
            try
            {
                PlatformAdapter.SendToCustomLogger(
                    string.Format(
                        "AuthStorageHelper.SafeRetrieveResource - Attempting to retrieve resource {0}",
                        resource), LoggingLevel.Verbose);

                var list = _vault.RetrieveAll();
                return (from item in list where resource.Equals(item.Resource) select item);
            }
            catch (Exception ex)
            {
                PlatformAdapter.SendToCustomLogger(
                    string.Format(
                        "AuthStorageHelper.SafeRetrieveResource - Exception occured when retrieving vault data for resource {0}",
                        resource), LoggingLevel.Critical);

                PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Critical);

                Debug.WriteLine("Failed to retrieve vault data for resource " + resource);
            }
            return new List<PasswordCredential>();
        }

        private PasswordCredential SafeRetrieveUser(string resource, string userName)
        {
            try
            {
                PlatformAdapter.SendToCustomLogger(
                    string.Format(
                        "AuthStorageHelper.SafeRetrieveUser - Attempting to retrieve user Resource={0}  UserName={1}",
                        resource, userName), LoggingLevel.Verbose);

                return SafeRetrieveResource(resource).FirstOrDefault(n => userName.Equals(n.UserName));
            }
            catch (Exception ex)
            {
                PlatformAdapter.SendToCustomLogger(
                    string.Format(
                        "AuthStorageHelper.SafeRetrieveUser - Exception occured when retrieving vault data for resource {0}",
                        resource), LoggingLevel.Critical);

                PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Critical);

                Debug.WriteLine("Failed to retrieve vault data for resource " + resource);
            }
            return null;
        }

        private IEnumerable<PasswordCredential> SafeRetrieveUser(string userName)
        {
            try
            {
                PlatformAdapter.SendToCustomLogger(
                    string.Format(
                        "AuthStorageHelper.SafeRetrieveUser - Attempting to retrieve user {0}",
                        userName), LoggingLevel.Verbose);

                return _vault.FindAllByUserName(userName);
            }
            catch (Exception ex)
            {
                PlatformAdapter.SendToCustomLogger(
                    string.Format(
                        "AuthStorageHelper.SafeRetrieveUser - Exception occured when retrieving vault data for user {0}",
                        userName), LoggingLevel.Critical);

                PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Critical);

                Debug.WriteLine("Failed to retrieve vault data for user");
            }
            return new List<PasswordCredential>();
        }

        /// <summary>
        ///     Persist account, and sets account as the current account.
        /// </summary>
        /// <param name="account"></param>
        internal void PersistCredentials(Account account)
        {
            PasswordCredential creds = SafeRetrieveUser(PasswordVaultAccounts, account.UserName);
            if (creds != null)
            {
                PlatformAdapter.SendToCustomLogger("AuthStorageHelper.PersistCredentials - removing existing credential", LoggingLevel.Verbose);
                _vault.Remove(creds);
                IReadOnlyList<PasswordCredential> current = _vault.FindAllByResource(PasswordVaultCurrentAccount);
                if (current != null)
                {
                    foreach (PasswordCredential user in current)
                    {
                        _vault.Remove(user);
                    }
                }
            }
            string serialized = Encryptor.Encrypt(JsonConvert.SerializeObject(account));
            _vault.Add(new PasswordCredential(PasswordVaultAccounts, account.UserName, serialized));
            _vault.Add(new PasswordCredential(PasswordVaultCurrentAccount, account.UserName, serialized));
            var options = new LoginOptions(account.LoginUrl, account.ClientId, account.CallbackUrl,
                LoginOptions.DefaultDisplayType, account.Scopes, LoginOptions.DefaultBrokerOptions, LoginOptions.DefaultUseTwoParamAuthAsyncMethod);
            SalesforceConfig.LoginOptions = options;
            PlatformAdapter.SendToCustomLogger("AuthStorageHelper.PersistCredentials - done adding info to vault", LoggingLevel.Verbose);
        }

        internal Account RetrieveCurrentAccount()
        {
            lock (_retrieveCurrentAccountLock)
            {
                PasswordCredential creds = SafeRetrieveResource(PasswordVaultCurrentAccount).FirstOrDefault();
                if (creds != null)
                {
                    PasswordCredential account = _vault.Retrieve(creds.Resource, creds.UserName);
                    if (String.IsNullOrWhiteSpace(account.Password))
                        _vault.Remove(account);
                    else
                    {
                        try
                        {
                            PlatformAdapter.SendToCustomLogger(
                                "AuthStorageHelper.RetrieveCurrentAccount - getting current account", LoggingLevel.Verbose);
                            return JsonConvert.DeserializeObject<Account>(Encryptor.Decrypt(account.Password));
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("***The exception decrypting account: " + ex.Message);
                            PlatformAdapter.SendToCustomLogger(
                                "AuthStorageHelper.RetrieveCurrentAccount - Exception occured when decrypting account, removing account from vault",
                                LoggingLevel.Warning);

                            PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Warning);

                            // if we can't decrypt remove the account
                            _vault.Remove(account);
                        }
                    }
                }
                return null;
            }
        }

        /// <summary>
        ///     Retrieve an account based on the id of the user.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal Account RetrievePersistedCredential(String id)
        {
            Dictionary<string, Account> accounts = RetrievePersistedCredentials();
            if (accounts.ContainsKey(id))
                return accounts[id];
            return null;
        }

        /// <summary>
        ///     Retrieve persisted account
        /// </summary>
        /// <returns></returns>
        internal Dictionary<string, Account> RetrievePersistedCredentials()
        {
            IEnumerable<PasswordCredential> creds = SafeRetrieveResource(PasswordVaultAccounts);
            var accounts = new Dictionary<string, Account>();
            if (creds != null)
            {
                PlatformAdapter.SendToCustomLogger(
                    "AuthStorageHelper.RetrievePersistedCredentials - attempting to get all credentials",
                    LoggingLevel.Verbose);

                foreach (PasswordCredential next in creds)
                {
                    PasswordCredential account = _vault.Retrieve(next.Resource, next.UserName);
                    if (String.IsNullOrWhiteSpace(account.Password))
                        _vault.Remove(next);
                    else
                    {
                        try
                        {
                            accounts.Add(next.UserName,
                           JsonConvert.DeserializeObject<Account>(Encryptor.Decrypt(account.Password)));
                        }
                        catch (Exception ex)
                        {
                            PlatformAdapter.SendToCustomLogger(
                                "AuthStorageHelper.RetrievePersistedCredentials - Exception occured when decrypting account, removing account from vault",
                                LoggingLevel.Warning);

                            PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Warning);

                            // if we can't decrypt remove the account
                           _vault.Remove(next);
                        }
                       
                    }
                }
            }

            PlatformAdapter.SendToCustomLogger(
                string.Format(
                    "AuthStorageHelper.RetrievePersistedCredentials - Done. Total number of accounts retrieved = {0}",
                    accounts.Count), LoggingLevel.Verbose);

            return accounts;
        }

        /// <summary>
        ///     Delete a persisted account credential based on the user id.
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="id"></param>
        internal void DeletePersistedCredentials(string userName, string id)
        {
            IEnumerable<PasswordCredential> creds = SafeRetrieveUser(userName);
            if (creds != null)
            {
                foreach (PasswordCredential next in creds)
                {
                    PasswordCredential vaultAccount = _vault.Retrieve(next.Resource, next.UserName);
                    try
                    {
                        var account = JsonConvert.DeserializeObject<Account>(Encryptor.Decrypt(vaultAccount.Password));
                        if (id.Equals(account.UserId))
                        {
                            PlatformAdapter.SendToCustomLogger(
                                string.Format(
                                    "AuthStorageHelper.DeletePersistedCredentials - removing entry from vault for UserName={0}  UserID={1}",
                                    userName, id), LoggingLevel.Verbose);

                            _vault.Remove(next);
                        }
                    }
                    catch (Exception ex)
                    {
                        PlatformAdapter.SendToCustomLogger(
                            "AuthStorageHelper.DeletePersistedCredentials - Exception occured when decrypting account, removing account from vault",
                            LoggingLevel.Warning);

                        PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Warning);

                        // if we can't decrypt remove the account
                       _vault.Remove(next);
                    }
                }
            }
        }

        /// <summary>
        ///     Delete all persisted accounts
        /// </summary>
        internal void DeletePersistedCredentials()
        {
            IEnumerable<PasswordCredential> accounts = SafeRetrieveResource(PasswordVaultAccounts);
            IEnumerable<PasswordCredential> current = SafeRetrieveResource(PasswordVaultCurrentAccount);
            if (accounts != null)
            {
                foreach (PasswordCredential next in accounts)
                {
                    _vault.Remove(next);
                }
            }
            if (current != null)
            {
                foreach (PasswordCredential next in current)
                {
                    _vault.Remove(next);
                }
            }

            PlatformAdapter.SendToCustomLogger(
                "AuthStorageHelper.DeletePersistedCredentials - removed all entries from vault", LoggingLevel.Verbose);
        }

        internal void PersistData(bool replace, string key, string data, string nonce = null)
        {
            if (_persistedData.Values.ContainsKey(key))
            {
                if (replace)
                {
                    _persistedData.Values[key] = Encryptor.Encrypt(data, nonce);
                }
            }
            else
            {
                _persistedData.Values.Add(key, Encryptor.Encrypt(data));
            }
        }

        internal string RetrieveData(string key, string nonce = null)
        {
            string data = null;
            if (_persistedData.Values.ContainsKey(key))
            {
                data = Encryptor.Decrypt(_persistedData.Values[key] as string, nonce);
            }
            return data;
        }

        internal void DeleteData(string key)
        {
            if (_persistedData.Values.ContainsKey(key))
            {
                _persistedData.Values.Remove(key);
            }
        }

        internal void PersistEncryptionSettings(string key, string salt)
        {
            DeleteEncryptionSettings();
            var encryptionSettingsObj = new { Key = key, Salt = salt };
            var encrpytionSettings = new PasswordCredential(PasswordVaultSecuredData, PasswordVaultEncryptionSettings,
                JsonConvert.SerializeObject(encryptionSettingsObj));
            _vault.Add(encrpytionSettings);
            PlatformAdapter.SendToCustomLogger("AuthStorageHelper.PersistEncryptionSettings - encryption settings added to vault",
                LoggingLevel.Verbose);
        }

        internal bool TryRetrieveEncryptionSettings(out string key, out string salt)
        {
            key = null;
            salt = null;
            PasswordCredential creds = SafeRetrieveResource(PasswordVaultSecuredData).FirstOrDefault();
            if (creds != null)
            {
                PasswordCredential encrpytionSettings = _vault.Retrieve(PasswordVaultSecuredData, PasswordVaultEncryptionSettings);
                if (String.IsNullOrWhiteSpace(encrpytionSettings.Password))
                {
                    // Failed to deserialize the data, we should clear it out and start over.
                    PlatformAdapter.SendToCustomLogger(
                        "AuthStorageHelper.TryRetrieveEncryptionSettings - Encryption Settings values are corrupt. Assuming bad state and clearing the vault completely",
                        LoggingLevel.Warning);
                    _vault.Remove(encrpytionSettings);
                    DeletePersistedCredentials();
                }
                else
                {
                    try
                    {
                        var encrpytionSettingsObj = JObject.Parse(encrpytionSettings.Password);
                        key = encrpytionSettingsObj.Value<string>("Key");
                        salt = encrpytionSettingsObj.Value<string>("Salt");
                        PlatformAdapter.SendToCustomLogger(
                        "AuthStorageHelper.TryRetrieveEncryptionSettings - Encryption Settings have been retrieved successfully.",
                        LoggingLevel.Verbose);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // Failed to deserialize the data, we should clear it out and start over.
                        PlatformAdapter.SendToCustomLogger(
                            "AuthStorageHelper.TryRetrieveEncryptionSettings - Encryption Settings values can't be deserialized. Assuming bad state and clearing the vault completely",
                            LoggingLevel.Warning);

                        PlatformAdapter.SendToCustomLogger(ex, LoggingLevel.Warning);
                        
                        _vault.Remove(encrpytionSettings);
                        DeletePersistedCredentials();
                    }
                }

            }
            else
            {
                var account = RetrieveCurrentAccount();
                // If  account is stored, but the Encryption Settings values can't be retrieved, then we should assume we are in a bad state and clear the vault.
                if (account != null )
                {
                    PlatformAdapter.SendToCustomLogger(
                        "AuthStorageHelper.TryRetrieveEncryptionSettings - Encryption Settings values can't be retrieved from vault. Assuming bad state and clearing the vault completely",
                        LoggingLevel.Verbose);
                    DeletePersistedCredentials();
                }
            }
            PlatformAdapter.SendToCustomLogger(
                        "AuthStorageHelper.TryRetrieveEncryptionSettings - Encryption Settings have not yet been saved.",
                        LoggingLevel.Verbose);
            return false;
        }

        internal void DeleteEncryptionSettings()
        {
            PasswordCredential encryptionSettings = SafeRetrieveUser(PasswordVaultSecuredData, PasswordVaultEncryptionSettings);
            if (encryptionSettings != null)
            {
                PlatformAdapter.SendToCustomLogger(
                    "AuthStorageHelper.DeleteEncryptionSettings - removed encryption settings from vault",
                    LoggingLevel.Verbose);
                _vault.Remove(encryptionSettings);
            }
        }
    }
}