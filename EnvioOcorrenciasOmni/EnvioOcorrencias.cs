using Core;
using EnvioOcorrenciasOmni.BLL.EnvioOcorrenciasWS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace EnvioOcorrenciasOmni
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

            DateTime dataInicial;
            DateTime dataFinal;

            if (dataAtual)
            {
                dataInicial = DateTime.Now.Date;
                dataFinal = dataInicial.AddDays(1).AddSeconds(-1);
            }
            else
            {
                dataInicial = InteracoesConsole.SoliticarData($"Digite a data inicial, ex : ({DateTime.Now.AddDays(-1).ToShortDateString()})").Date;
                dataFinal = InteracoesConsole.SoliticarData($"Digite a data final, ex :  ({DateTime.Now.ToShortDateString()})");
            }

            //invert datas caso necessário
            if (dataFinal.Date < dataInicial.Date)
            {
                dataInicial.InvertarDatas(ref dataFinal);
            }

            //Todo : colocar a quantidade de dias permitadas no arquivo de configuração
            if ((dataFinal.Date - dataInicial.Date).TotalDays > 7)
            {
                _logger.LogInformation($"Intervalo entre as datas informadas é maior que o permitido : intervalor permitido é de até {7} dias.");
                return;
            }

            _logger.LogInformation($"Periodo Informado de: {dataInicial.ToShortDateString()} até :{dataFinal.ToShortDateString()} \n\n");


            var regOcoore = new EnvioOcorrenciasWS(Configuration, _logger, _spinner);


            _spinner.Start();
            _spinner.SetCursorPosition();

            while (dataFinal.Date >= dataInicial.Date)
            {
                var dataEnvio = dataInicial;
                _logger.LogInformation($"Iniciado o Envio das Ocorrencia do dia: {dataEnvio.ToShortDateString()} - Em Data:{DateTime.Now.ToShortDateString()} Hora: { DateTime.Now:HH:mm:ss}");
                _spinner.SetCursorPosition();

                regOcoore.RegistrarOcorrencia(371, dataEnvio, dataEnvio);

                dataInicial = dataInicial.AddDays(1);

                var mensagem = $"\nFinalizado o Envio da Ocorrência do dia: {dataEnvio.ToShortDateString()} - Em Data:{DateTime.Now.ToShortDateString()} Hora: { DateTime.Now:HH:mm:ss}";

                _logger.LogInformation(mensagem);
                _spinner.SetCursorPosition();
            }
        }
    }
}
