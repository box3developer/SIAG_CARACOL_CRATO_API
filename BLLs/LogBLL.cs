using Dapper;
using dotnet_api.ModelsSIAG;
using grendene_caracois_api_csharp;
using Microsoft.Data.SqlClient;
using System;

namespace dotnet_api.BLLs
{
    public class LogBLL
    {
        public static async Task GravarLog(
            Guid? id_requisicao,
            string? nm_identificador,
            string? id_caixa,
            string mensagem,
            string metodo,
            string? id_operador,
            string tipo
        )
        {
            try
            {
                var query = @"INSERT INTO logCaracol ( 
                                id_requisicao, 
                                nm_identificador, 
                                id_caixa, 
                                data, 
                                mensagem, 
                                metodo, 
                                id_operador, 
                                tipo
                            )
                            VALUES ( 
                                @id_requisicao, 
                                @nm_identificador, 
                                @id_caixa, 
                                @data, 
                                @mensagem, 
                                @metodo, 
                                @id_operador, 
                                @tipo
                            )";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var linhas = await conexao.ExecuteAsync(query, new
                    {
                        id_requisicao,
                        nm_identificador,
                        id_caixa,
                        mensagem,
                        metodo,
                        id_operador,
                        tipo,
                        data = DateTime.Now
                    });
                }
            }
            catch {}
        }
    }
}
