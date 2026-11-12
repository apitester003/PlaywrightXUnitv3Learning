using PlaywrightXUnitv3Learning.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlaywrightXUnitv3Learning.Pages
{
    public interface ILoginPage : IBasePage
    {
        Task EnterUsername(string username);
        Task EnterPassword(string password);
        Task ClickLoginButton();
    }
}
