using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace SiteMonitor
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ServiceConfigurations _serviceConfigurations;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;

            _serviceConfigurations = new ServiceConfigurations();
            new ConfigureFromConfigurationOptions<ServiceConfigurations>(
                configuration.GetSection("ServiceConfigurations"))
                    .Configure(_serviceConfigurations);
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Serviço iniciado em: {DateTimeOffset.Now}");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Serviço parado em: {DateTimeOffset.Now}");
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Worker executando em: {DateTimeOffset.Now}");

                foreach (string host in _serviceConfigurations.Hosts)
                {
                    _logger.LogInformation($"Verificando a disponibilidade do host {host}");

                    var resultado = new ResultMonitor();
                    resultado.Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    resultado.Host = host;

                    // Verifica a disponibilidade efetuando um ping
                    // no host que foi configurado em appsettings.json
                    try
                    {
                        using (Ping p = new Ping())
                        {
                            var resposta = p.Send(host);
                            resultado.Status = resposta.Status.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        resultado.Status = "Exception";
                        resultado.Exception = ex;
                    }

                    string jsonResultado = JsonConvert.SerializeObject(resultado);
                    if (resultado.Exception == null)
                        _logger.LogInformation(jsonResultado);
                    else
                        _logger.LogError(jsonResultado);
                }

                await Task.Delay(_serviceConfigurations.Intervalo, stoppingToken);
            }
        }
    }
}
