using PlaywrightXUnitv3Learning.Pages;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlaywrightXUnitv3Learning.Flows
{
    public class LoginFlow : ILoginFlow
    {
        private readonly ILoginPage _loginPage;
        public LoginFlow(ILoginPage loginPage)
        {
            _loginPage = loginPage;
        }
        public async Task LoginWithCredentials(string username, string password)
        {
            await _loginPage.GoTo("https://www.saucedemo.com");
            await _loginPage.EnterUsername(username);
            await _loginPage.EnterPassword(password);
            await _loginPage.ClickLoginButton();
        }
    }
}
