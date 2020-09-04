using Core;
using EnvioOcorrenciasOmni.BLL.EnvioOcorrenciasWS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace SimularOcorrenciasOmni
{
    public class EnvioOcorrencias
    {
        public EnvioOcorrencias(IConfiguration configuration, ILogger logger, Spinner spinner)
        {
            Configuration = configuration;
            _logger = logger;
            _spinner = spinner;
        }

        public IConfiguration Configuration { get; }
        public ILogger _logger { get; }
        public Spinner _spinner { get; }

        public void ProcessarEnvio(bool dataAtual = false)
        {
            var regOcoore = new EnvioOcorrenciasWS(Configuration, _logger, _spinner);
            _spinner.Start();
            _spinner.SetCursorPosition();
            _logger.LogInformation($"Iniciado o Envio das Ocorrencias Em Data:{DateTime.Now.ToShortDateString()} Hora: { DateTime.Now:HH:mm:ss}");
            _spinner.SetCursorPosition();

            regOcoore.RegistrarOcorrencia(371);

            var mensagem = $"\nFinalizado o Envio da Ocorrências Em Data:{DateTime.Now.ToShortDateString()} Hora: { DateTime.Now:HH:mm:ss}";

            _logger.LogInformation(mensagem);
            _spinner.SetCursorPosition();
        }

    }
}
