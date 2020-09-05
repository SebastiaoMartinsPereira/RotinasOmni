using System;
using System.Collections.Generic;
using System.Text;

namespace EnvioOcorrenciasOmni.DTO.OmniFacil
{
    public class AcionamentoIntegracaoOmni
    {
        public string andn_id { get; set; }
        public int cod_ocorr_omni { get; set; }
        public DateTime andn_data_cadastro { get; set; }
        public string at_reu_cod_externo { get; set; }
        public string at_reu_cpf_cnpj { get; set; }
        public string at_reu_nome { get; set; }
        public string us_login_externo { get; set; }
        public string andn_andamento { get; set; }
        public string contr_contrato { get; set; }
        public string telefone { get; set; }
        public string us_login { get; set; }
        public string us_nome { get; set; }
        public string cod_cliente_omni { get; set; }
        public int cart_id { get; set; }
        public string cart_nome { get; set; }
        public string origem_acionamento { get; set; }
    }
}
