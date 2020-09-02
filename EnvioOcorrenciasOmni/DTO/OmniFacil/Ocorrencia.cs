using System;
using System.Collections.Generic;
using System.Text;

namespace EnvioOcorrenciasOmni.DTO.OmniFacil
{
    [Serializable]
    public class Ocorrencia : IDisposable
    {
        private bool disposedValue;

        public string id_ocorrencia { get; set; }

        public string origem_ocorrencia { get; set; }
        public int id_usuario { get; set; }
        public string retorno { get; set; }
        public string contrato { get; set; }
        public string cliente { get; set; }
        public bool success { get; set; }
        public DateTime criado_em { get; set; }
        public DateTime atualizado_em { get; set; }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Ocorrencia()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
