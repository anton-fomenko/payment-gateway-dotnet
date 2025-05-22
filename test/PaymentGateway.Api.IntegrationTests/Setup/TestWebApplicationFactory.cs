using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using PaymentGateway.Services;

namespace PaymentGateway.Api.IntegrationTests.Setup
{
    public class TestWebApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
    {
        public Mock<IPaymentsService> PaymentsServiceMock { get; } = new Mock<IPaymentsService>();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing IPaymentsService registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IPaymentsService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add the mock IPaymentsService
                services.AddSingleton(PaymentsServiceMock.Object);
            });
        }
    }
}
