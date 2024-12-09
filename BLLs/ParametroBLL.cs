using System.Collections;
using Dapper;
using dotnet_api.ModelsSIAG;
using grendene_caracois_api_csharp;
using Microsoft.Data.SqlClient;

namespace dotnet_api.BLLs
{
    public class ParametroBLL
    {
        public static async Task<string> GetParamentro(string descricao)
        {
            try
            {
                var query = @"SELECT nm_valor
                            FROM parametro WITH(NOLOCK)
                            WHERE nm_parametro = @descricao";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    return await conexao.QueryFirstOrDefaultAsync<string?>(query, new { descricao = descricao }) ?? "";
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<ParametroMensagemCaracolSIAGModel> GetMensagem(string descricao)
        {
            try
            {
                var query = @"SELECT id_parametromensagemcaracol AS IdParametroMensagemCaracol,
                                descricao AS Descricao,
                                mensagem AS Mensagem,
                                cor AS Cor
                            FROM parametromensagemcaracol WITH(NOLOCK)
                            WHERE descricao = @descricao";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var mensagem = await conexao.QueryFirstOrDefaultAsync<ParametroMensagemCaracolSIAGModel?>(query, new { descricao = descricao });

                    if (mensagem == null)
                        mensagem = new ModelsSIAG.ParametroMensagemCaracolSIAGModel()
                        {
                            Descricao = descricao,
                            Mensagem = descricao,
                            Cor = "#dc2626"
                        };

                    return mensagem;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<ParametroMensagemCaracolSIAGModel> GetMensagem(Exception ex)
        {
            var resposta = await GetMensagem(ex.Message);

            foreach (DictionaryEntry par in ex.Data)
                resposta.Mensagem = resposta.Mensagem?.Replace($"{{{par.Key.ToString()}}}", par.Value?.ToString() ?? "");

            return resposta;
        }

        public static async Task<PosicaoCaracolRefugoSIAGModel> GetPosicaoCaracolRefugo(string tipo, string? fabrica)
        {
            try
            {
                string query;
                var posicao = new PosicaoCaracolRefugoSIAGModel();

                query = @"SELECT id_posicaocaracolrefugo AS IdPosicaoCaracolRefugo,
                                posicao AS Posicao,
                                tipo AS Tipo,
                                fabrica AS Fabrica
                            FROM posicaocaracolrefugo WITH(NOLOCK)
                            WHERE tipo = @tipo AND fabrica = @fabrica";
                    
                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    posicao = await conexao.QueryFirstOrDefaultAsync<PosicaoCaracolRefugoSIAGModel>(query, new { tipo, fabrica });
                    if (posicao != null) return posicao;
                }
                
                query = @"SELECT id_posicaocaracolrefugo AS IdPosicaoCaracolRefugo,
                            posicao AS Posicao,
                            tipo AS Tipo,
                            fabrica AS Fabrica
                        FROM posicaocaracolrefugo WITH(NOLOCK)
                        WHERE tipo = @tipo AND fabrica is NULL";
                
                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    posicao = await conexao.QueryFirstOrDefaultAsync<PosicaoCaracolRefugoSIAGModel>(query, new { tipo });
                    return posicao;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<PosicaoCaracolRefugoSIAGModel> GetInfoPosicaoCaracolRefugo(int posicao)
        {
            try
            {
                string query;
                var infoPosicao = new PosicaoCaracolRefugoSIAGModel();

                query = @"SELECT id_posicaocaracolrefugo AS IdPosicaoCaracolRefugo,
                                posicao AS Posicao,
                                tipo AS Tipo,
                                fabrica AS Fabrica,
                                descricao AS Descricao
                            FROM posicaocaracolrefugo WITH(NOLOCK)
                            WHERE posicao = @posicao";
                    
                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    infoPosicao = await conexao.QueryFirstOrDefaultAsync<PosicaoCaracolRefugoSIAGModel>(query, new { posicao });
                    return infoPosicao;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}