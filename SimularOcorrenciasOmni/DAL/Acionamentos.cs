﻿using Core;
using Dapper;
using EnvioOcorrenciasOmni.DTO.OmniFacil;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnvioOcorrenciasOmni.DAL
{
    public class Acionamentos : IDisposable
    {
        private readonly ConnDbDapper _conn;
        private bool disposedValue;

        public Acionamentos(IConfiguration config)
        {
            _conn = new ConnDbDapper(config);
        }


        /// <summary>
        /// Recupera os acionamentos a serem enviados via WS
        /// </summary>
        /// <param name="dataInicio"></param>
        /// <param name="dataFinal"></param>
        /// <returns></returns>
        public IEnumerable<AcionamentoIntegracaoOmni> SimulacaoesOcorrenciasParaEnvioWs(long numeroLinhas, long principioContagem)
        {
            try
            {
                return _conn.GetConn().
                    Query<AcionamentoIntegracaoOmni>("select * from public.fn_integracao_omni_incluir_andamento(_andn_dt_inicio:=@_andn_dt_inicio::timestamp,_andn_dt_fim:=@_andn_dt_fim,_cliente:=@_cliente)"
                    , new
                    {
                        numeroLinhas = numeroLinhas,
                        principioContagem = principioContagem,
                        _cliente = 10
                    });
            }
            finally
            {
                _conn.CloseConn();
            }

        }

        public void RegistrarHistoricoOcorrencia(IEnumerable<Ocorrencia> ocorrencias)
        {
            try
            {
                _conn.GetConn().Execute("INSERT INTO omnifacil.ocorrencia_history(id_ocorrencia, origem_ocorrencia, id_usuario, cliente, contrato,retorno, success) VALUES(@id_ocorrencia, @origem_ocorrencia, @id_usuario, @cliente, @contrato,@retorno, @success)", ocorrencias);
            }
            finally
            {
                _conn.CloseConn();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_conn.Conn.State.Equals(System.Data.ConnectionState.Open)) _conn.CloseConn();
                    _conn.Conn?.Dispose();
                }
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Acionamentos()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
