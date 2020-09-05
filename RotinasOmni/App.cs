using Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RotinasOmni
{
    public class App
    {
        public IConfiguration Configuration { get; }
        private readonly ILogger _logger;
        private readonly Spinner _spinner;
        private readonly string[] _args;

        public App(string[] args, IConfiguration configuration, ILogger<App> logger, Spinner spinner)
        {
            this.Configuration = configuration;
            _logger = logger;
            _spinner = spinner;
            _args = args;
        }

        /// <summary>
        /// Start Application 
        /// </summary>
        public void Run()
        {
            try
            {
                if (_args.ToArray().Contains("-h")) Help();
                else if (_args.Length == 0 || (_args.ToArray().Contains("-h") && _args.Length == 1))
                {
                    Help();
                    Console.ReadKey();
                }

                //Processa envio de ocorrencias via WebService Omni
                if (_args[0] == typeof(EnvioOcorrenciasOmni.EnvioOcorrencias).Name) new EnvioOcorrenciasOmni.EnvioOcorrencias(this.Configuration, _logger, _spinner).ProcessarEnvio(_args.ToArray().Contains("-a"));
                if (_args[0] == typeof(SimularOcorrenciasOmni.SimularOcorrencias).Name) new SimularOcorrenciasOmni.SimularOcorrencias(this.Configuration, _logger, _spinner).ProcessarEnvio();


            }
            catch (Exception ex)
            {
                _logger.LogDebug($"\n Ocorreu um erro: {ex.Message} Data:{DateTime.Now.Date} Hora:{DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}");
            }
            finally
            {
                _spinner.Stop();
                _spinner.Dispose();
            }
        }

        private void Help()
        {
            _logger.LogInformation("Definir a aplicação a ser executada via linha de comando\n\n");
            _logger.LogInformation("-- ---- ----                 Opções disponíveis            ---- ---- --");

            _logger.LogInformation($"{typeof(EnvioOcorrenciasOmni.EnvioOcorrencias).Name} [-a]");
            _logger.LogInformation($"Descrição \n Nome da Aplicação : {typeof(EnvioOcorrenciasOmni.EnvioOcorrencias).Name} \n Parâmetros: \n [-a] Opcional, informa se a data deve ser definida automaticamente como a data do dia atual.");

            _logger.LogInformation($"{typeof(SimularOcorrenciasOmni.SimularOcorrencias).Name} ");
            _logger.LogInformation($"Descrição \n Nome da Aplicação : {typeof(SimularOcorrenciasOmni.SimularOcorrencias).Name}");
        }
    }
}
