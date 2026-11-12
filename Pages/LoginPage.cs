using Microsoft.Playwright;
using PlaywrightXUnitv3Learning.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlaywrightXUnitv3Learning.Pages
{
    public class LoginPage : BasePageSimple, ILoginPage
    {
        public LoginPage(IPage page) : base(page)
        {
            PageUrl = "";
        }

        //Locators (protected, following your rules)
        protected ILocator UsernameInput => Page.Locator("[data-test='username']");
        protected ILocator PasswordInput => Page.Locator("[data-test='password']");
        protected ILocator LoginButton => Page.Locator("[data-test='login-button']");

        public async Task EnterUsername(string username)
        {
            await UsernameInput.FillAsync(username);
        }

        public async Task EnterPassword(string password)
        {
            await PasswordInput.FillAsync(password);
        }

        public async Task ClickLoginButton()
        {
            await LoginButton.ClickAsync();
        }
    }
}
