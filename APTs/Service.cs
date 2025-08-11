internal partial class Program
{
    class Service(CustomAssemblyLoadContext alc, IHostApplicationLifetime lifetime) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try {
                var type = await alc.LoadAsync(stoppingToken);
                Activator.CreateInstance(type, [stoppingToken]);
            } finally {
                lifetime.StopApplication();
            }
        }
    }
}