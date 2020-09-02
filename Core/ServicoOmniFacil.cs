using System;
using System.Collections.Generic;
using System.Text;
using WsCobrancaOmni;

namespace Core
{
    public class ServicoOmniFacil
    {
        static ServicoOmniFacil _instancia;
        //private string Url;
        private GetTokenResponse _token;

        public static ServicoOmniFacil Instancia(string cnpjParceiro, string codigoParceiro, string usuarioOmni, string codigoAgente, string usuarioProxy = "", string senhaProxy = "")
        {

            if (_instancia is null)
            {
                return (_instancia = new ServicoOmniFacil(cnpjParceiro, codigoParceiro, usuarioOmni, codigoAgente, usuarioProxy, senhaProxy));
            }

            if (
                    _instancia.CodigoParceiro.Equals(codigoParceiro) &&
                    _instancia.CnpjParceiro.Equals(cnpjParceiro) &&
                    _instancia.UsuarioOmni.Equals(usuarioOmni) &&
                    _instancia.CodigoAgente.Equals(codigoAgente)
                ) return _instancia;

            return (_instancia = new ServicoOmniFacil(cnpjParceiro, codigoParceiro, usuarioOmni, codigoAgente, usuarioProxy, senhaProxy));
        }


        private ServicoOmniFacil(string cnpjParceiro, string codigoParceiro, string usuarioOmni, string codigoAgente, string usuarioProxy, string senhaProxy) : this(cnpjParceiro, codigoParceiro, codigoAgente, usuarioOmni)
        {
            //producao: producao
            CnpjParceiro = cnpjParceiro;
            CodigoParceiro = codigoParceiro;
            UsuarioOmni = usuarioOmni;
            CodigoAgente = codigoAgente;
            UsuarioProxy = usuarioProxy;
            SenhaProxy = senhaProxy;
            // Producao = producao;
        }

        private ServicoOmniFacil(string cnpjParceiro, string codigoParceiro, string usuarioOmni, string codigoAgente)
        {
            //bool producao = false
            //this.Url = producao ? "https://hst1.omni.com.br/ws_cob/WS_COB.servidor_soap_cdc_cobranca.handle_request" : "https://hst4.omni.com.br/dsv/eloi/ELOI_MENDES.servidor_soap_cdc_cobranca.handle_request";
            this.CnpjParceiro = cnpjParceiro;
            this.CodigoParceiro = codigoParceiro;
            this.UsuarioOmni = usuarioOmni;
            this.CodigoAgente = codigoAgente;

            this.ObterServico();
        }

        public servidorsoapcdccobrancaSoapClient Cliente { get; private set; }
        public string CnpjParceiro { get; }
        public string CodigoParceiro { get; set; }
        public string UsuarioOmni { get; }
        public string CodigoAgente { get; }
        public string UsuarioProxy { get; }
        public string SenhaProxy { get; }
        public bool Producao { get; }


        public ServicoOmniFacil ObterServico(string codigoParceiro = "")
        {
            if (codigoParceiro != "")
            {
                this.CodigoParceiro = codigoParceiro;
            }

            this.Cliente = new servidorsoapcdccobrancaSoapClient();
            return _instancia;
        }

        public IncluiOcorrenciaResponse IncluirOcorrencia(string contrato, string cod_cliente, string atendente, int cod_ocorrencia, DateTime dt_ocorrencia, string observacao, string fone_contato, string cod_agente = "")
        {
            var inclui = new inclui()
            {
                agente = "".Equals(cod_agente) ? this.CodigoAgente : cod_agente,
                atendente = atendente,
                cod_cliente = cod_cliente,
                cod_ocorrencia = cod_ocorrencia.ToString(),
                dt_ocorrencia = dt_ocorrencia.ToShortDateString(),
                observacao = observacao,
                usuario_agente = this.UsuarioOmni,
                fone_contato = fone_contato,
                contrato = contrato
            };
            return this.Cliente.IncluiOcorrenciaAsync(new IncluiOcorrenciaRequest(GetAuth(), inclui)).Result;
        }

        private auth GetAuth()
        {
            GetToken();

            return new auth
            {
                codigoParceiro = CodigoParceiro,
                token = new authToken
                {
                    idToken = _token.token.idToken,
                    validadeToken = _token.token.validadeToken
                }
            };
        }

        public IDictionary<string, object> GetToken()
        {
            try
            {
                var tokenRequest = new GetTokenRequest(Int64.Parse(this.CnpjParceiro), Int32.Parse(this.CodigoParceiro), "1.0");

                if (_token == null)
                {
                    _token = this.Cliente.GetTokenAsync(tokenRequest).Result;
                }
                else
                {
                    if (!ValidarToken())
                    {
                        _token = this.Cliente.GetTokenAsync(tokenRequest).Result;
                    }
                }

                if ((_token.erro?.Length ?? 0) > 0)
                {
                    return new Dictionary<string, object>() { { "error", _token.erro } };
                }

                return null;
            }
            catch (Exception ex)
            {
                var error = new erro() { codigo = "SAL-0001", descricao = "Não foi possível recuperar o token, serviço indiponível!", detalhe = ex.Message };
                return new Dictionary<string, object>() { { "error", error } };
            }
        }



        private bool ValidarToken()
        {
            var verificaTokenRequest = new VerifyTokenRequest(this.CodigoParceiro, _token.token.idToken);
            var resposta = this.Cliente.VerifyTokenAsync(verificaTokenRequest).Result;

            if (DateTime.TryParse(resposta.token.infoToken.validadeToken, out var validadeToken))
            {
                return validadeToken > DateTime.Now;
            }

            return !(resposta.erro?.Length > 0);
        }





    }
}