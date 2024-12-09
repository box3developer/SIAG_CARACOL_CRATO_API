using Dapper;
using dotnet_api.ModelsSIAG;
using grendene_caracois_api_csharp;
using Microsoft.Data.SqlClient;

namespace dotnet_api.BLLs
{
    public class LiderVirtualBLL
    {
        public static async Task<bool> UpdateLiderVirtual(LiderVirtualSIAGModel liderVirtual)
        {
            try
            {
                var query = @"UPDATE lidervirtual
                            SET
                                id_operador = @IdOperador,
                                id_equipamentoorigem = @IdEquipamentoOrigem,
                                id_equipamentodestino = @IdEquipamentoDestino,
                                dt_login = @DtLogin,
                                dt_logoff = @DtLogoff,
                                id_operadorlogin = @IdOperadorLogin,
                                dt_loginlimite = @DtLoginLimite
                            WHERE id_lidervirtual = @idLiderVirtual";
                
                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var linhas = await conexao.ExecuteAsync(query, liderVirtual);
                    return linhas > 0;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<bool> CreateLiderVirtual(LiderVirtualSIAGModel liderVirtual)
        {
            try
            {
                var query = @"INSERT INTO lidervirtual (id_operador, id_equipamentoorigem, id_equipamentodestino, dt_login, dt_logoff, id_operadorlogin, dt_loginlimite)
                            VALUES (@IdOperador, @IdEquipamentoOrigem, @IdEquipamentoDestino, @DtLogin, @DtLogoff, @IdOperadorLogin, @DtLoginLimite)";
                
                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var linhas = await conexao.ExecuteAsync(query, liderVirtual);
                    return linhas > 0;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<LiderVirtualSIAGModel?> GetLiderVirtualInfoByOperador(string cracha)
        {
            try
            {
                var query = @"SELECT TOP 1 id_lidervirtual AS IdLiderVirtual,
                                id_operador AS IdOperador,
                                id_equipamentoorigem AS IdEquipamentoOrigem,
                                id_equipamentodestino AS IdEquipamentoDestino,
                                dt_login AS DtLogin,
                                dt_logoff AS DtLogoff,
                                id_operadorlogin AS IdOperadorLogin,
                                dt_loginlimite AS DtLoginLimite
                            FROM lidervirtual WITH(NOLOCK)
                            WHERE id_operador = @cracha
                            ORDER BY id_lidervirtual DESC";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var liderVitual = await conexao.QueryFirstOrDefaultAsync<LiderVirtualSIAGModel?>(query, new { cracha = cracha });
                    return liderVitual;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<LiderVirtualSIAGModel?> GetLiderVirtualInfoByDestino(string idEquipamento)
        {
            try
            {
                var query = @"SELECT TOP 1 id_lidervirtual AS IdLiderVirtual,
                                id_operador AS IdOperador,
                                id_equipamentoorigem AS IdEquipamentoOrigem,
                                id_equipamentodestino AS IdEquipamentoDestino,
                                dt_login AS DtLogin,
                                dt_logoff AS DtLogoff,
                                id_operadorlogin AS IdOperadorLogin,
                                dt_loginlimite AS DtLoginLimite
                            FROM lidervirtual WITH(NOLOCK)
                            WHERE id_equipamentodestino = @idEquipamentoDestino
                            ORDER BY id_lidervirtual DESC";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var liderVitual = await conexao.QueryFirstOrDefaultAsync<LiderVirtualSIAGModel?>(query, new { idEquipamentoDestino = idEquipamento });
                    return liderVitual;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<LiderVirtualSIAGModel?> GetLiderVirtualInfoByOrigem(string identificadorCaracol)
        {
            try
            {
                var query = @"SELECT TOP 1 id_lidervirtual AS IdLiderVirtual,
                                id_operador AS IdOperador,
                                id_equipamentoorigem AS IdEquipamentoOrigem,
                                id_equipamentodestino AS IdEquipamentoDestino,
                                dt_login AS DtLogin,
                                dt_logoff AS DtLogoff,
                                id_operadorlogin AS IdOperadorLogin,
                                dt_loginlimite AS DtLoginLimite
                            FROM lidervirtual WITH(NOLOCK)
                            WHERE id_equipamentoOrigem = @idEquipamentoOrigem
                            ORDER BY id_lidervirtual DESC";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var equipamento = await EquipamentoBLL.GetEquipamentoByIdentificadorCaracol(identificadorCaracol);
                    var liderVitual = await conexao.QueryFirstOrDefaultAsync<LiderVirtualSIAGModel?>(query, new { idEquipamentoOrigem = equipamento?.IdEquipamento });
                    return liderVitual;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<Dictionary<string, LiderVirtualSIAGModel>> GetLiderVitualInfo()
        {
            try
            {
                var query = @"select id_lidervirtual AS IdLiderVirtual,
                            id_operador AS IdOperador,
                            id_equipamentoorigem AS IdEquipamentoOrigem,
                            id_equipamentodestino AS IdEquipamentoDestino,
                            dt_login AS DtLogin,
                            dt_logoff AS DtLogoff,
                            id_operadorlogin AS IdOperadorLogin,
                            dt_loginlimite AS DtLoginLimite
                        from lidervirtual WITH(NOLOCK)
                        WHERE id_lidervirtual in (
                            SELECT max(id_lidervirtual)
                            FROM lidervirtual WITH(NOLOCK)
                            GROUP BY id_equipamentodestino
                        )";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var listaLiderVirtual = await conexao.QueryAsync<LiderVirtualSIAGModel>(query);
                    return listaLiderVirtual.ToDictionary(x => x.IdEquipamentoDestino ?? "", x => x);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}