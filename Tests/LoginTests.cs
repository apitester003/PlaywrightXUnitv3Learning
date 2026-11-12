using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using PlaywrightXUnitv3Learning.Core;
using PlaywrightXUnitv3Learning.Flows;
using PlaywrightXUnitv3Learning.Pages;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlaywrightXUnitv3Learning.Tests
{
    public class LoginTests : BaseTest
    {
        private ILoginFlow _loginFlow = null!;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();

            ServiceCollection services = new ServiceCollection();
            services.AddScoped(sp => Page);
            services.AddScoped<ILoginPage, LoginPage>();
            services.AddScoped<ILoginFlow, LoginFlow>();

            ServiceProvider serviceProvider = services.BuildServiceProvider();
            _loginFlow = serviceProvider.GetRequiredService<ILoginFlow>();
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldSucceed()
        {
            //Act
            await _loginFlow.LoginWithCredentials("standard_user", "secret_sauce");

            //Assert - Check if we're on the inventory page
            await Page.WaitForURLAsync("**/inventory.html");
            await Assertions.Expect(Page.Locator(".inventory_list")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 15000 });
        }
    }
}
