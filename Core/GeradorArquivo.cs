using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;

namespace Core
{
    public class GeradorArquivo
    {
        public static void ArrayListParaCsv(ArrayList dados, string caminhoParaSalvar)
        {
            StreamWriter arquivoCsv = CriarArquivo(caminhoParaSalvar);

            try
            {
                string linha = string.Empty;

                foreach (Dictionary<string, object> item in dados)
                {
                    foreach (KeyValuePair<string, object> _x in item)
                    {
                        linha = linha + _x.Key.ToString() + ";";
                    }

                    linha = linha.Substring(0, linha.Length - 1);
                    arquivoCsv.WriteLine(linha);
                    break;
                }

                linha = string.Empty;

                foreach (Dictionary<string, object> item in dados)
                {
                    foreach (KeyValuePair<string, object> _x in item)
                    {
                        linha = linha + _x.Value.ToString() + ";";
                    }

                    linha = linha.Substring(0, linha.Length - 1);
                    arquivoCsv.WriteLine(linha);
                    linha = string.Empty;
                }

                Console.WriteLine("\nArquivo {caminhoNome} gerado com sucesso !");
            }
            catch (Exception e)
            {
                Console.WriteLine($"\n Erro na geração do arquivo Excel: {e.Message}");
            }
            finally
            {
                arquivoCsv.Close();
            }
        }

        /// <summary>
        /// VArre uma lista de objetos e monta um arquivo CSV com base nas suas propriedades
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dados"></param>
        /// <param name="caminhoParaSalvar"></param>
        /// <param name="separador"></param>
        public static void ListaParaCsv<T>(IEnumerable<T> dados, string caminhoParaSalvar, char separador = ';')
        {

            Type itemType = typeof(T);
            var props = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                .OrderBy(p => p.Name);

            using (var writer = CriarArquivo(caminhoParaSalvar))
            {
                writer.WriteLine(string.Join($"{separador}", props.Select(p => p.Name.ToUpper())));

                foreach (var item in dados)
                {
                    writer.WriteLine(System.Text.RegularExpressions.Regex.Replace(string.Join($"{separador}", props.Select(p => p.GetValue(item, null))), @"\r\n?|\n", " "));
                }
            }
        }

        private static StreamWriter CriarArquivo(string caminhoParaSalvar)
        {
            StreamWriter arquivoCsv;
            string caminhoNome = caminhoParaSalvar;

            if (File.Exists(caminhoNome))
                File.Delete(caminhoNome);

            arquivoCsv = File.CreateText(caminhoNome);
            return arquivoCsv;
        }
    }
}
