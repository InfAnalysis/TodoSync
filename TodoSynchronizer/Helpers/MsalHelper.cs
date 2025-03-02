using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
//using Microsoft.Identity.Client.Desktop;
using Microsoft.Identity.Client.Extensions.Msal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using TodoSynchronizer.Core.Models;

namespace TodoSynchronizer.Helpers
{
    public class MsalHelper
    {
        public MsalHelper()
        {
            CreateApplication();
        }

        public void CreateApplication()
        {
            // Check if we're using client credentials flow (with client secret)
            if (!string.IsNullOrEmpty(ClientSecret))
            {
                // Create a confidential client application (for apps with client secret)
                _confidentialClientApp = ConfidentialClientApplicationBuilder.Create(ClientId)
                                                       .WithAuthority($"{Instance}{Tenant}")
                                                       .WithClientSecret(ClientSecret)
                                                       .WithRedirectUri("http://localhost")
                                                       .Build();
                
                // Set up the token cache for the confidential client
                var storageProperties =
                     new StorageCreationPropertiesBuilder(CacheFileName, CacheDir)
                     .Build();

                var cacheHelper = MsalCacheHelper.CreateAsync(storageProperties).Result;
                cacheHelper.RegisterCache(_confidentialClientApp.UserTokenCache);
            }
            else
            {
                // Create a public client application (for desktop/mobile apps without client secret)
                _clientApp = PublicClientApplicationBuilder.Create(ClientId)
                                                        .WithAuthority($"{Instance}{Tenant}")
                                                        .WithDefaultRedirectUri()
                                                        //.WithBrokerPreview(true)
                                                        .Build();
                
                // Set up the token cache for the public client
                var storageProperties =
                     new StorageCreationPropertiesBuilder(CacheFileName, CacheDir)
                     .Build();

                var cacheHelper = MsalCacheHelper.CreateAsync(storageProperties).Result;
                cacheHelper.RegisterCache(_clientApp.UserTokenCache);
            }
        }

        public async Task<CommonResult> GetToken(Window host)
        {
            // Use client credentials flow if we have a client secret
            if (_confidentialClientApp != null)
            {
                try
                {
                    // Acquire token for application (client credentials flow)
                    var result = await _confidentialClientApp.AcquireTokenForClient(scopes)
                        .ExecuteAsync();

                    return new CommonResult(true, result.AccessToken);
                }
                catch (MsalException ex)
                {
                    return new CommonResult(false, $"Error Acquiring Token:{System.Environment.NewLine}{ex}");
                }
                catch (Exception ex)
                {
                    return new CommonResult(false, $"Error:{System.Environment.NewLine}{ex}");
                }
            }
            
            // Continue with the existing interactive authorization code flow
            AuthenticationResult authResult = null;
            var app = PublicClientApp;
            IAccount firstAccount;

            var accounts = await app.GetAccountsAsync().ConfigureAwait(false);
            firstAccount = accounts.FirstOrDefault();

            try
            {
                authResult = await app.AcquireTokenSilent(scopes, firstAccount)
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                // A MsalUiRequiredException happened on AcquireTokenSilent. 
                // This indicates you need to call AcquireTokenInteractive to acquire a token
                System.Diagnostics.Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");

                try
                {
                    authResult = await app.AcquireTokenInteractive(scopes)
                        .WithAccount(firstAccount)
                        .WithParentActivityOrWindow(new WindowInteropHelper(host).Handle)
                        .WithPrompt(Microsoft.Identity.Client.Prompt.SelectAccount)
                        .ExecuteAsync();
                }
                catch (MsalException msalex)
                {
                    return new CommonResult(false, $"Error Acquiring Token:{System.Environment.NewLine}{msalex}");
                }
            }
            catch (Exception ex)
            {
                return new CommonResult(false, $"Error Acquiring Token Silently:{System.Environment.NewLine}{ex}");
            }

            if (authResult != null)
            {
                return new CommonResult(true, authResult.AccessToken);
            }
            else
            {
                return new CommonResult(false, "未知错误");
            }
        }

        // Add the client secret property
        private static string ClientSecret = "ryU8Q~6NewY1xHB0Pr41AAiKfq4xY_yDmuBr5a4w"; // Replace with your actual client secret

        private static string ClientId = "c133bd3b-da0e-4ec5-90e9-1cb173dcd60e";

        private static string Tenant = "consumers";
        private static string Instance = "https://login.microsoftonline.com/";
        private IPublicClientApplication _clientApp;
        private IConfidentialClientApplication _confidentialClientApp; // For client credentials flow

        public IPublicClientApplication PublicClientApp { get { return _clientApp; } }
        public IConfidentialClientApplication ConfidentialClientApp { get { return _confidentialClientApp; } }

        private static readonly string s_cacheFilePath =
                   Path.Combine(MsalCacheHelper.UserRootDirectory, "msal.contoso.cache");

        public static readonly string CacheFileName = Path.GetFileName(s_cacheFilePath);
        public static readonly string CacheDir = Path.GetDirectoryName(s_cacheFilePath);

        public static string graphAPIEndpoint = "https://graph.microsoft.com/v1.0/me";

        public static string[] scopes = new string[] { "Tasks.ReadWrite", "offline_access", "User.Read" };
    }
}