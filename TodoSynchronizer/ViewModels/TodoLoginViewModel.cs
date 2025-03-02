using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TodoSynchronizer.Helpers;
using TodoSynchronizer.Core.Models;
using TodoSynchronizer.Mvvm;
using TodoSynchronizer.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TodoSynchronizer.Models;

namespace TodoSynchronizer.ViewModels
{
    public partial class TodoLoginViewModel : ObservableObject
    {
        [ObservableProperty]
        private LoginInfoModel loginInfo = new LoginInfoModel();
        
        [ObservableProperty]
        private string clientSecret = ""; // Property to store the client secret

        public AsyncRelayCommand LoginCommand { get; set; }
        public RelayCommand LogoutCommand { get; set; }
        public RelayCommand SwitchCommand { get; set; }

        public TodoLoginViewModel()
        {
            // Try to load client secret from settings or environment
            // This is just a placeholder - you might load it from somewhere secure
            ClientSecret = IniHelper.GetKeyValue("graph", "clientSecret", "");
            
            LoginCommand = new AsyncRelayCommand(Login);
            LogoutCommand = new RelayCommand(Logout);
            SwitchCommand = new RelayCommand(Switch);
        }

        public async Task Login()
        {
            // Create an MSAL helper with our client secret
            MsalHelper helper = new MsalHelper();
            
            // You might need to set the client secret on the helper here
            // if your MsalHelper class provides a way to set it
            
            CommonResult res = await helper.GetToken(Application.Current.MainWindow);
            
            if (!res.success)
            {
                MessageBox.Show(res.result);
                return;
            }
            TodoService.Token = res.result;
            var userinfo = TodoService.GetUserInfo();
            var logininfo = new LoginInfoModel();

            logininfo.UserName = userinfo.DisplayName;
            logininfo.UserEmail = userinfo.UserPrincipalName;
            logininfo.UserAvatar = BitmapHelper.GetBitmapSource(TodoService.GetUserAvatar());
            logininfo.IsLogin = true;
            LoginInfo = logininfo;
            
            // Save client secret if it was successful
            if (!string.IsNullOrEmpty(ClientSecret))
            {
                IniHelper.SetKeyValue("graph", "clientSecret", ClientSecret);
            }
        }
        
        public void Logout()
        {
            LoginInfo = new LoginInfoModel();
        }
        
        public void Switch()
        {
            // Implement account switching logic
        }
    }
}