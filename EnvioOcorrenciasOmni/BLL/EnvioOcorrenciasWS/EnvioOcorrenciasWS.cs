using Core;
using EnvioOcorrenciasOmni.DTO.OmniFacil;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WsCobrancaOmni;

namespace EnvioOcorrenciasOmni.BLL.EnvioOcorrenciasWS
{
    public class EnvioOcorrenciasWS
    {
        //Codigos de erros Omni Conhecidos
        private readonly string[] codigosOmni = { "-20006", "-10001", "-10003", "-20023", "-20005" };

        private readonly IConfiguration _config;
        private readonly ILogger _logger;
        private readonly Spinner _spinner;
        private readonly string saveFilePath;
        private readonly dynamic _parametrosServicoOmni;
        private ServicoOmniFacil _clienteServicoOmni;

        public EnvioOcorrenciasWS(IConfiguration config, ILogger logger, Spinner spinner)
        {
            _config = config;
            _logger = logger;
            _spinner = spinner;
            saveFilePath = _config.GetSection("GeneralConfig").GetSection("logFilePath").Value;

            _parametrosServicoOmni = new ExpandoObject();
            _parametrosServicoOmni.CNPJ_SALASOLUTION = _config.GetSection("ServicoOmni").GetSection("CNPJ_PARCEIRO_OMNI").Value;
            _parametrosServicoOmni.CODIGO_PARCEIRO_OMNI = _config.GetSection("ServicoOmni").GetSection("CODIGO_PARCEIRO_OMNI").Value;
            _parametrosServicoOmni.loginExterno = _config.GetSection("ServicoOmni").GetSection("USUARIO_PARCEIRO_OMNI").Value;
            _parametrosServicoOmni.Agente = _config.GetSection("ServicoOmni").GetSection("AGENTE_PARCEIRO_OMNI").Value;
            _parametrosServicoOmni.userProxy = _config.GetSection("GeneralConfig").GetSection("userProxy").Value;
            _parametrosServicoOmni.senhaProxy = _config.GetSection("GeneralConfig").GetSection("senhaProxy").Value;

            _parametrosServicoOmni.quantidadeRegistroPorEtapa = _config.GetSection("GeneralConfig").GetSection("registrosPorEtapa").Value.ToInt();

            _clienteServicoOmni = ServicoOmniFacil.Instancia(_parametrosServicoOmni.CNPJ_SALASOLUTION, _parametrosServicoOmni.CODIGO_PARCEIRO_OMNI, _parametrosServicoOmni.loginExterno, _parametrosServicoOmni.Agente, _parametrosServicoOmni.userProxy, _parametrosServicoOmni.senhaProxy);

        }


        public void RegistrarOcorrencia(int us_id, DateTime dataInicio, DateTime dataFim)
        {
            IEnumerable<AcionamentoIntegracaoOmni> dadosOcorrencia = null;
            var nomeArquivo = "";
            List<Ocorrencia> Ocorrencias = new List<Ocorrencia>();
            string codigoClienteParceiroAtual = string.Empty;

            try
            {
                _spinner.SetCursorSpinner(_logger, "Recuperando ocorrencias a serem enviadas");

                //Recupera dados das ocorrências a serem importadas para a omni.
                using (var dalAndamentoNegocial = new DAL.Acionamentos(_config))
                {
                    dadosOcorrencia = dalAndamentoNegocial.OcorrenciasParaEnvioWs(dataInicio, dataFim).ToList();
                }

                _spinner.SetCursorSpinner(_logger, "Gerando relatório contendo dados coletados para envio");

                GeradorArquivo.ListaParaCsv(dadosOcorrencia, nomeArquivo = $"{saveFilePath}\\{DateTime.Now:yyyyMMddHHmmss}.csv");

                _spinner.SetCursorSpinner(_logger, $"Relatório gerado com sucesso em : {nomeArquivo}");


                if (dadosOcorrencia.Any())
                {
                    _spinner.SetCursorSpinner(_logger, $"Iniciando envio de " + dadosOcorrencia.Count() + " registros");

                    IEnumerable<AcionamentoIntegracaoOmni> dados = null;

                    try
                    {
                        //TODO colocar no arquvo de configuracao a qunatidade de dados a serem enviados
                        while ((dados = GerarLista(dadosOcorrencia, _parametrosServicoOmni.quantidadeRegistroPorEtapa)).Count() > 0)
                        {
                            foreach (var ocorr in dados)
                            {
                                try
                                {
                                    if (_clienteServicoOmni.CodigoParceiro != _parametrosServicoOmni.CODIGO_PARCEIRO_OMNI)
                                    {
                                        codigoClienteParceiroAtual = _parametrosServicoOmni.CODIGO_PARCEIRO_OMNI;
                                        _clienteServicoOmni = _clienteServicoOmni.ObterServico();

                                    }

                                    var ret = _clienteServicoOmni.IncluirOcorrencia(
                                                                          ocorr.contr_contrato
                                                                        , ocorr.at_reu_cod_externo
                                                                        , ocorr.us_login_externo
                                                                        , ocorr.cod_ocorr_omni
                                                                        , ocorr.andn_data_cadastro
                                                                        , ocorr.andn_andamento
                                                                        , ocorr.telefone
                                                                        , ocorr.cod_cliente_omni
                                                                    );

                                    Ocorrencia ocorrencia = GerarOcorrencia(us_id, ocorr, MontarMensagem(ocorr.andn_id, ret), (ret.erro?.Length ?? 0) == 0);
                                    Ocorrencias.Add(ocorrencia);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogDebug(" Ocorreu um erro: " + string.Format("IdOcorrencia: {0} | Erro: {1}", ocorr.andn_id, ex.Message));
                                }
                            }

                            _spinner.SetCursorSpinner(_logger, $"Registros enviados {dados.Count()}, de um total de {dadosOcorrencia.Count() + dados.Count()}, restam {dadosOcorrencia.Count()}");

                        }
                    }
                    finally
                    {

                        try
                        {
                            _spinner.SetCursorSpinner(_logger, "Finalizando o processamento");
                            if (Ocorrencias.Count > 0)
                            {
                                GeradorArquivo.ListaParaCsv(Ocorrencias, nomeArquivo.Replace(".csv", "resultado_processamento.csv"));

                                //Recupera dados das ocorrências a serem importadas para a omni.
                                using (var dalAndamentoNegocial = new DAL.Acionamentos(_config))
                                {
                                    dalAndamentoNegocial.RegistrarHistoricoOcorrencia(Ocorrencias);
                                }
                            }

                            _spinner.SetCursorSpinner(_logger, "Envio finalizado");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug($"Ocorreu um erro ao finalizar : {ex.Message}");
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug($" Ocorreu um erro: {ex.Message}");
                throw;
            }
            finally
            {
                dadosOcorrencia = null;
                Ocorrencias = null;
            }
        }

        public void SimularOcorrencia(int us_id)
        {
            var nomeArquivo = "";
            List<Ocorrencia> Ocorrencias = new List<Ocorrencia>();

            try
            {
                _spinner.SetCursorSpinner(_logger, "Recuperando ocorrencias a serem enviadas");

                int limit = 20000;
                int offset = 0;

                //Recupera dados das ocorrências a serem importadas para a omni.
                using (var dalAndamentoNegocial = new DAL.Acionamentos(_config))
                {
                    IEnumerable<AcionamentoIntegracaoOmni> ocorrencias;
                    while ((ocorrencias = dalAndamentoNegocial.SimulacaoesOcorrenciasParaEnvioWs(limit, offset)).Count() > 0)
                    {
                        _spinner.SetCursorSpinner(_logger, $"Enviando simulações de {offset} à {offset + limit}");

                        IEnumerable<AcionamentoIntegracaoOmni> dados = ocorrencias;
                        dados.AsParallel()
                            .ForAll(ocorr =>
                            {

                                try
                                {
                                    var ret = _clienteServicoOmni.IncluirOcorrencia(
                                                                          ocorr.contr_contrato
                                                                        , ocorr.at_reu_cod_externo
                                                                        , ocorr.us_login_externo
                                                                        , ocorr.cod_ocorr_omni
                                                                        , ocorr.andn_data_cadastro
                                                                        , ocorr.andn_andamento
                                                                        , ocorr.telefone
                                                                        , ocorr.cod_cliente_omni
                                                                    );

                                    Ocorrencia ocorrencia = GerarOcorrencia(us_id, ocorr, MontarMensagem(ocorr.andn_id, ret), (ret.erro?.Length ?? 0) == 0);
                                    Ocorrencias.Add(ocorrencia);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogDebug(" Ocorreu um erro: " + string.Format("IdOcorrencia: {0} | Erro: {1}", ocorr.andn_id, ex.Message));
                                }
                            }
                        );


                        offset += limit;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug($" Ocorreu um erro: {ex.Message}");
                throw;
            }
            finally
            {
                if (Ocorrencias.Count > 0)
                {
                    GeradorArquivo.ListaParaCsv(Ocorrencias, nomeArquivo = $"{saveFilePath}\\Simulacoes_{DateTime.Now:yyyyMMddHHmmss}.csv");

                    using (var dalAndamentoNegocial = new DAL.Acionamentos(_config))
                    {
                        dalAndamentoNegocial.RegistrarHistoricoOcorrencia(
                            new List<Ocorrencia>() {
                                new Ocorrencia() { atualizado_em = DateTime.Now, cliente = "", contrato = "", criado_em = DateTime.Now.Date, id_ocorrencia = 0.ToString(), id_usuario = 371, origem_ocorrencia = "FNWS", retorno = "sucesso", success = true }
                            });
                    }
                }
                Ocorrencias = null;
            }
        }

        private static IEnumerable<AcionamentoIntegracaoOmni> GerarLista(IEnumerable<AcionamentoIntegracaoOmni> dadosOcorrencia, int registroPorEtapa)
        {

            var regPorEtapa = registroPorEtapa > 0 ? registroPorEtapa : 2500;

            IEnumerable<AcionamentoIntegracaoOmni> dados = new List<AcionamentoIntegracaoOmni>(dadosOcorrencia.Count() >= regPorEtapa ? dadosOcorrencia.Take(regPorEtapa) : dadosOcorrencia.Take(dadosOcorrencia.Count()));
            if (dadosOcorrencia.Count() >= regPorEtapa)
            {
                ((List<AcionamentoIntegracaoOmni>)dadosOcorrencia).RemoveRange(0, regPorEtapa);
            }
            else
            {
                ((List<AcionamentoIntegracaoOmni>)dadosOcorrencia).RemoveRange(0, dadosOcorrencia.Count());
            }

            return dados;
        }

        /// <summary>
        /// //Ocorrencias para inserção na base de dados
        /// </summary>
        /// <param name="us_id"></param>
        /// <param name="itemRetorno"></param>
        /// <param name="mensagem"></param>
        /// <returns></returns>
        private static Ocorrencia GerarOcorrencia(int us_id, AcionamentoIntegracaoOmni ocorrenciaRetorno, string mensagem, bool sucesso)
        {
            try
            {
                var ocorrencia = new Ocorrencia
                {
                    cliente = ocorrenciaRetorno.at_reu_cod_externo,
                    contrato = ocorrenciaRetorno.contr_contrato,
                    id_ocorrencia = ocorrenciaRetorno.andn_id,
                    id_usuario = us_id,
                    retorno = System.Text.RegularExpressions.Regex.Replace(mensagem.ToString(), @"\r\n?|\n|;", " "),
                    origem_ocorrencia = ocorrenciaRetorno.origem_acionamento,
                    criado_em = DateTime.Now,
                    success = sucesso
                };

                return ocorrencia;

            }
            catch (Exception)
            {

                throw;
            }
        }

        //public void RegistrarOcorrencia(int us_id)
        //{
        //    IEnumerable<AcionamentoIntegracaoOmni> dadosOcorrencia = null;
        //    var nomeArquivo = "";
        //    List<Ocorrencia> Ocorrencias = new List<Ocorrencia>();
        //    string codigoClienteParceiroAtual = string.Empty;

        //    try
        //    {
        //        _spinner.SetCursorSpinner(_logger, "Recuperando ocorrencias a serem enviadas");

        //        //Recupera dados das ocorrências a serem importadas para a omni.
        //        using (var dalAndamentoNegocial = new DAL.Acionamentos(_config))
        //        {
        //            dadosOcorrencia = dalAndamentoNegocial.OcorrenciasParaEnvioWs().ToList();
        //        }

        //        _spinner.SetCursorSpinner(_logger, "Gerando relatório contendo dados coletados para envio");

        //        GeradorArquivo.ListaParaCsv(dadosOcorrencia, nomeArquivo = $"{saveFilePath}\\{DateTime.Now:yyyyMMddHHmmss}.csv");

        //        _spinner.SetCursorSpinner(_logger, $"Relatório gerado com sucesso em : {nomeArquivo}");


        //        if (dadosOcorrencia.Any())
        //        {
        //            _spinner.SetCursorSpinner(_logger, $"Iniciando envio de " + dadosOcorrencia.Count() + " registros");

        //            IEnumerable<AcionamentoIntegracaoOmni> dados = null;

        //            try
        //            {
        //                //TODO colocar no arquvo de configuracao a qunatidade de dados a serem enviados
        //                while ((dados = GerarLista(dadosOcorrencia, _parametrosServicoOmni.quantidadeRegistroPorEtapa)).Count() > 0)
        //                {
        //                    foreach (var ocorr in dados)
        //                    {
        //                        try
        //                        {
        //                            if (_clienteServicoOmni.CodigoParceiro != _parametrosServicoOmni.CODIGO_PARCEIRO_OMNI)
        //                            {
        //                                codigoClienteParceiroAtual = _parametrosServicoOmni.CODIGO_PARCEIRO_OMNI;
        //                                _clienteServicoOmni = _clienteServicoOmni.ObterServico();

        //                            }

        //                            var ret = _clienteServicoOmni.IncluirOcorrencia(
        //                                                                  ocorr.contr_contrato
        //                                                                , ocorr.at_reu_cod_externo
        //                                                                , ocorr.us_login_externo
        //                                                                , ocorr.cod_ocorr_omni
        //                                                                , ocorr.andn_data_cadastro
        //                                                                , ocorr.andn_andamento
        //                                                                , ocorr.telefone
        //                                                                , ocorr.cod_cliente_omni
        //                                                            );

        //                            Ocorrencia ocorrencia = GerarOcorrencia(us_id, ocorr, MontarMensagem(ocorr.andn_id, ret), (ret.erro?.Length ?? 0) == 0);
        //                            Ocorrencias.Add(ocorrencia);
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            _logger.LogDebug(" Ocorreu um erro: " + string.Format("IdOcorrencia: {0} | Erro: {1}", ocorr.andn_id, ex.Message));
        //                        }
        //                    }

        //                    _spinner.SetCursorSpinner(_logger, $"Registros enviados {dados.Count()}, de um total de {dadosOcorrencia.Count() + dados.Count()}, restam {dadosOcorrencia.Count()}");

        //                }
        //            }
        //            finally
        //            {

        //                try
        //                {
        //                    _spinner.SetCursorSpinner(_logger, "Finalizando o processamento");
        //                    if (Ocorrencias.Count > 0)
        //                    {
        //                        GeradorArquivo.ListaParaCsv(Ocorrencias, nomeArquivo.Replace(".csv", "resultado_processamento.csv"));

        //                        //Recupera dados das ocorrências a serem importadas para a omni.
        //                        using (var dalAndamentoNegocial = new DAL.Acionamentos(_config))
        //                        {
        //                            dalAndamentoNegocial.RegistrarHistoricoOcorrencia(Ocorrencias);
        //                        }
        //                    }

        //                    _spinner.SetCursorSpinner(_logger, "Envio finalizado");
        //                }
        //                catch (Exception ex)
        //                {
        //                    _logger.LogDebug($"Ocorreu um erro ao finalizar : {ex.Message}");
        //                    throw;
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogDebug($" Ocorreu um erro: {ex.Message}");
        //        throw;
        //    }
        //    finally
        //    {
        //        dadosOcorrencia = null;
        //        Ocorrencias = null;
        //    }
        //}

        private string MontarMensagem(string idOcorrencia, IncluiOcorrenciaResponse retornoOmni)
        {
            string mensagem = string.Empty;

            if (retornoOmni.erro?.Length > 0)
            {
                var erro = retornoOmni.erro.First();

                string codigoOmni = (dynamic)erro.codigo;

                if (codigosOmni.Contains(codigoOmni))
                {
                    mensagem = string.Format("ERROR: {0} , Descrição: {1}, Detalhe: {2}", codigoOmni, (dynamic)erro.descricao, (dynamic)erro.detalhe);
                }
                else
                {
                    mensagem = string.Format("ERROR: {0}, Descrição: {1}, Detalhe: Falha ao registrar ocorrência!", codigoOmni, (dynamic)erro.descricao);
                }
            }
            else
            {
                mensagem = $"Mensagem : Ocorrencia registrada com sucesso, Id_Ocorrencia: {idOcorrencia}";
            }
            return mensagem;
        }

    }
}
