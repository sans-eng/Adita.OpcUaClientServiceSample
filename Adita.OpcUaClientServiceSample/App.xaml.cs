using Adita.OpcUaClientServiceSample.Services;
using Adita.OpcUaClientServiceSample.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Adita.OpcUaClientServiceSample
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Constructors
        public App()
        {
            //Prepare configuration.
            Configuration = ConfigureApplicationConfiguration();

            //Manage services.
            ServiceProvider = ConfigureServices();
        }
        #endregion Constructors

        #region Public properties
        public new static App Current => (App)Application.Current;
        #endregion Public properties

        #region Containers
        public IServiceProvider ServiceProvider { get; }
        public IConfiguration Configuration { get; }
        #endregion Containers

        #region Override methods
        protected override async void OnStartup(StartupEventArgs e)
        {
            bool initializeResult = await InitializeOpcUaClientAsync();
            //in here need to implement notification if connection failed!!!!!!!!!!!!!!!!!!!!!!!!!!!!.

            //base.OnStartup(e);
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        #endregion Override methods

        #region Builder methods
        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            //configure configurations
            services.Configure<UaOptions>
                    (Configuration.GetSection(nameof(UaOptions)));

            services.AddSingleton<IUaClientService, UaClientService>();

            services.AddTransient(typeof(MainViewModel));

            return services.BuildServiceProvider();
        }
        private static IConfiguration ConfigureApplicationConfiguration()
        {
            var builder = new ConfigurationBuilder()
         .SetBasePath(Directory.GetCurrentDirectory())
         .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "Settings//AppSettings.json"), optional: false, reloadOnChange: true);

            return builder.Build();
        }
        #endregion Builder methods

        #region Opc Ua Initializer
        private async Task<bool> InitializeOpcUaClientAsync()
        {
            var client = ServiceProvider.GetRequiredService<IUaClientService>();

            var settings = Configuration.GetSection(nameof(UaOptions)).Get<UaOptions>();

            if (client != null && settings != null)
            {
                client.ServerUrl = settings.ServerUrl;
                client.UseSecurity = settings.UseSecurity;

                bool result = await client.ConnectAsync();
                if (result)
                    client.Subscribe(new MonitoredItems(), 500);

                return result;
            }
            else
            {
                return false;
            }
        }
        #endregion Opc Ua Initializer
    }
}
