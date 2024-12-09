using System.Text.Json;
using Dapper;
using dotnet_api.Models;
using dotnet_api.ModelsNodeRED;
using dotnet_api.ModelsSIAG;
using dotnet_api.Utils;
using grendene_caracois_api_csharp;
using Microsoft.Data.SqlClient;

namespace dotnet_api.BLLs
{
    public class OperadorBLL
    {
        public static async Task<OperadorSIAGModel?> GetOperadorByCracha(string cracha, string? cracha2 = null)
        {
            try
            {
                var query = "SELECT id_operador AS IdOperador, " +
                                   "nm_operador AS NmOperador, " +
                                   "nm_cpf AS NmCpf, " +
                                   "fg_funcao AS FgFuncao " +
                            "FROM operador WITH(NOLOCK) " +
                            "WHERE id_operador = @cracha";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var operador = await conexao.QueryFirstOrDefaultAsync<OperadorSIAGModel>(query, new { cracha = cracha });

                    if (operador == null)
                        if (!string.IsNullOrWhiteSpace(cracha2))
                            operador = await GetOperadorByCracha(cracha2, null);

                    return operador;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<bool> LogoffCaracol(string cracha, string idEquipamento)
        {
            try
            {
                var query = "UPDATE equipamento " +
                            "SET id_operador = NULL " +
                            "WHERE id_equipamento = @idEquipamento";

                var queryHistorico = "INSERT INTO operadorhistorico (id_operador, id_equipamento, id_endereco, cd_evento, dt_evento) " +
                                     "VALUES (@cracha, @idEquipamento, NULL, 2, @dataEvento)";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var qtdLinhas = await conexao.ExecuteAsync(query, new { idEquipamento = idEquipamento });
                    var qtdLinhasHistorico = await conexao.ExecuteAsync(queryHistorico, new { idEquipamento = idEquipamento, cracha = cracha, dataEvento = DateTime.Now });

                    return (qtdLinhas > 0 && qtdLinhasHistorico > 0) ? true : false;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<bool> LoginCaracol(string cracha, string idEquipamento)
        {
            try
            {
                var query = "UPDATE equipamento " +
                            "SET id_operador = @cracha " +
                            "WHERE id_equipamento = @idEquipamento";

                var queryHistorico = "INSERT INTO operadorhistorico (id_operador, id_equipamento, id_endereco, cd_evento, dt_evento) " +
                                     "VALUES (@cracha, @idEquipamento, NULL, 1, @dataEvento)";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var qtdLinhas = await conexao.ExecuteAsync(query, new { idEquipamento = idEquipamento, cracha = cracha });
                    var qtdLinhasHistorico = await conexao.ExecuteAsync(queryHistorico, new { idEquipamento = idEquipamento, cracha = cracha, dataEvento = DateTime.Now });

                    return (qtdLinhas > 0 && qtdLinhasHistorico > 0) ? true : false;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<int> GetMeta()
        {
            var hoje = DateTime.Now.Date;

            if (Global.MetaPorHora == null || Global.DataMeta == null || Global.DataMeta.Value.Date != hoje)
            {
                var query = @"SELECT top 1 nm_valor AS Meta
                          FROM parametro WITH(NOLOCK)
                          WHERE nm_parametro = 'Caixa hora operador sorter'";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    Global.DataMeta = hoje;
                    Global.MetaPorHora = await conexao.QueryFirstOrDefaultAsync<int>(query);
                }
            }

            return Global.MetaPorHora ?? 0;
        }

        public static async Task<TurnoSIAGModel?> GetTurno(DateTime horario)
        {
            var agora = DateTime.Now.Date;

            if (Global.Turnos == null || Global.DataTurnos == null || Global.DataTurnos.Value.Date != agora)
            {
                var query = @"SELECT cd_turno AS CodTurno, dt_inicio as DtInicio, dt_fim as DtFim
                          FROM turno WITH(NOLOCK)
                          ORDER BY dt_fim DESC";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var turnos = await conexao.QueryAsync<TurnoSIAGModel>(query);

                    var turnosNow = new List<TurnoSIAGModel>();

                    foreach (var turno in turnos)
                    {
                        turno.DtInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, turno.DtInicio.Hour, turno.DtInicio.Minute, 59);
                        turno.DtFim = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, turno.DtFim.Hour, turno.DtFim.Minute, 59);

                        if (turno.DtFim < turno.DtInicio)
                        {
                            turno.DtFim = turno.DtFim.AddDays(1);

                            turnosNow.Add(new TurnoSIAGModel
                            {
                                CodTurno = turno.CodTurno,
                                DtInicio = new DateTime(turno.DtFim.Year, turno.DtFim.Month, turno.DtFim.Day, 0, 0, 0),
                                DtFim = new DateTime(turno.DtFim.Year, turno.DtFim.Month, turno.DtFim.Day, turno.DtFim.Hour, turno.DtFim.Minute, 59)
                            });

                            turno.DtFim = turno.DtFim.AddDays(-2);

                            turnosNow.Add(new TurnoSIAGModel
                            {
                                CodTurno = turno.CodTurno,
                                DtInicio = new DateTime(turno.DtFim.Year, turno.DtFim.Month, turno.DtFim.Day, 0, 0, 0),
                                DtFim = new DateTime(turno.DtFim.Year, turno.DtFim.Month, turno.DtFim.Day, turno.DtFim.Hour, turno.DtFim.Minute, 59)
                            });

                            turno.DtFim = turno.DtFim.AddDays(1);

                            turnosNow.Add(new TurnoSIAGModel
                            {
                                CodTurno = turno.CodTurno,
                                DtInicio = new DateTime(turno.DtFim.Year, turno.DtFim.Month, turno.DtFim.Day, 0, 0, 0),
                                DtFim = new DateTime(turno.DtFim.Year, turno.DtFim.Month, turno.DtFim.Day, turno.DtFim.Hour, turno.DtFim.Minute, 59)
                            });

                            turno.DtFim = new DateTime(turno.DtInicio.Year, turno.DtInicio.Month, turno.DtInicio.Day, 23, 59, 59);
                        }

                        turnosNow.Add(turno);
                    }

                    Global.DataTurnos = agora;
                    Global.Turnos = turnosNow;
                }
            }

            var turnoAtual = Global.Turnos.Where(x => horario >= x.DtInicio && horario <= x.DtFim).FirstOrDefault();

            return turnoAtual;
        }

        public static async Task<List<EficienciaModel>> GetEficiencias()
        {
            try
            {
                var hoje = DateTime.Now.Date;

                if (Global.Eficiencias == null || Global.DataEficiencias == null || Global.DataEficiencias.Value.Date != hoje)
                {
                    var query = @"SELECT nm_parametro AS Nome, CAST(REPLACE(nm_valor, ',', '.') AS REAL) AS Porcentagem
                                FROM parametro WITH(NOLOCK)
                                WHERE nm_tipo = 'HumorEficiencia'
                                ORDER BY nm_valor DESC";

                    using (var conexao = new SqlConnection(Global.Conexao))
                    {
                        var eficiencias = await conexao.QueryAsync<EficienciaModel>(query);
                        Global.DataEficiencias = hoje;
                        Global.Eficiencias = eficiencias.ToList();
                    }
                }

                return Global.Eficiencias;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<PerformanceOperadorModel> CalcularPerformanceTurnoAtual(string idOperador, Guid? id_requisicao)
        {
            try
            {
                var agora = DateTime.Now;
                var turnoAtual = await GetTurno(agora);
                var metaHora = await GetMeta();
                var eficiencias = await GetEficiencias();
                int meta = 0;
                int real = 0;

                if (turnoAtual == null)
                {
                    await LogBLL.GravarLog(
                        id_requisicao,
                        "",
                        "",
                        $"Erro ao calcular performande para o horário {agora} pois não pertence a nenhum turno",
                        "CalcularPerformanceTurnoAtual",
                        idOperador,
                        "erro"
                    );

                    throw new Exception($"Horário atual ({agora}) não está em um turno.");
                }

                var horas = agora.Subtract(turnoAtual.DtInicio).TotalHours;
                meta += (int)Math.Truncate(horas * metaHora);

                var query = @"SELECT COUNT(*) FROM (
                            SELECT COUNT(*) AS Leituras
                            FROM desempenho WITH(NOLOCK)
                            WHERE id_operador = @idOperador
                                AND (dt_cadastro >= @dataInicio AND dt_cadastro <= @dataFim)
                            GROUP BY id_referencia
                            ) AS T1";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    real += await conexao.QueryFirstOrDefaultAsync<int>(query, new
                    {
                        idOperador = idOperador,
                        dataInicio = turnoAtual.DtInicio,
                        dataFim = turnoAtual.DtFim,
                    });
                }

                var retorno = new PerformanceOperadorModel
                {
                    Tipo = "dia",
                    Dif = meta - real,
                    Real = real,
                    Meta = meta,
                    Percent = meta == 0 ? 100 : Math.Round((real / (decimal)meta) * 100, 2)
                };

                foreach (var eficiencia in eficiencias)
                    if (retorno.Percent >= eficiencia.Porcentagem) {
                        retorno.Eficiencia = eficiencia.Nome?.Replace("Eficiência.", "").ToLower();
                        break;
                    }

                if (retorno.Eficiencia == null)
                    retorno.Eficiencia = eficiencias.Last().Nome?.Replace("Eficiência.", "").ToLower();

                await LogBLL.GravarLog(
                    id_requisicao,
                    "",
                    "",
                    $"Eficiência {retorno.Eficiencia} - {retorno.Percent} do turno {turnoAtual.CodTurno} calculada para operador {idOperador}",
                    "CalcularPerformanceTurnoAtual",
                    idOperador,
                    "info"
                );

                return retorno;
            }
            catch (Exception)
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    "",
                    "",
                    $"Erro ao calcular eficiência do turno para operador {idOperador}",
                    "CalcularPerformanceTurnoAtual",
                    idOperador,
                    "erro"
                );

                throw;
            }
        }

        public static async Task<PerformanceOperadorModel> CalcularPerformanceHoraAtual(string idOperador, Guid? id_requisicao)
        {
            try
            {
                var dataFim = DateTime.Now;
                var dataInicio = new DateTime(dataFim.Year, dataFim.Month, dataFim.Day, dataFim.Hour, 0, 0);
                var turno = await GetTurno(dataFim);
                var metaHora = await GetMeta();
                var eficiencias = await GetEficiencias();
                int meta = 0;
                int real = 0;

                if (turno == null)
                {
                    await LogBLL.GravarLog(
                        id_requisicao,
                        "",
                        "",
                        $"Erro ao calcular performande para o horário {dataFim} pois não pertence a nenhum turno",
                        "CalcularPerformanceHoraAtual",
                        idOperador,
                        "erro"
                    );

                    throw new Exception("Turno não encontrado.");
                }

                if (dataInicio < turno.DtInicio)
                    dataInicio = turno.DtInicio;

                var horas = dataFim.Subtract(dataInicio).TotalHours;
                meta += (int)Math.Truncate(horas * metaHora);

                var query = @"SELECT COUNT(*) FROM (
                            SELECT COUNT(*) AS Leituras
                            FROM caixaleitura WITH(NOLOCK)
                            WHERE id_operador = @idOperador
                                AND (fg_tipo = 19 OR fg_tipo = 12)
                                AND (dt_leitura >= @dataInicio AND dt_leitura <= @dataFim)
                            GROUP BY id_caixa
                            ) AS T1";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    real += await conexao.QueryFirstOrDefaultAsync<int>(query, new
                    {
                        idOperador = idOperador,
                        dataInicio = dataInicio,
                        dataFim = dataFim
                    });
                }

                var retorno = new PerformanceOperadorModel
                {
                    Tipo = "hora",
                    Dif = meta - real,
                    Real = real,
                    Meta = meta,
                    Percent = meta == 0 ? 100 : Math.Round((real / (decimal)meta) * 100, 2)
                };

                foreach (var eficiencia in eficiencias)
                {
                    if (retorno.Percent >= eficiencia.Porcentagem) {
                        retorno.Eficiencia = eficiencia.Nome?.Replace("Eficiência.", "").ToLower();
                        break;
                    }
                }

                if (retorno.Eficiencia == null)
                    retorno.Eficiencia = eficiencias.Last().Nome?.Replace("Eficiência.", "").ToLower();

                await LogBLL.GravarLog(
                    id_requisicao,
                    "",
                    "",
                    $"Eficiência {retorno.Eficiencia} - {retorno.Percent} calculada para operador {idOperador} na hora atual {dataFim}",
                    "CalcularPerformanceHoraAtual",
                    idOperador,
                    "info"
                );

                return retorno;
            }
            catch (Exception)
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    "",
                    "",
                    $"Erro ao calcular eficiência da hora atual para operador {idOperador}",
                    "CalcularPerformanceHoraAtual",
                    idOperador,
                    "erro"
                );

                throw;
            }
        }

        public static async Task<EquipamentoSIAGModel?> LiderVirtualPendente(string idOperador, string idEquipamento)
        {
            try
            {
                var liderVirtualPendente = await LiderVirtualBLL.GetLiderVirtualInfoByOperador(idOperador ?? "");

                if (liderVirtualPendente != null && liderVirtualPendente.IdEquipamentoDestino != idEquipamento)
                    if (DateTime.Now < liderVirtualPendente.DtLoginLimite
                        && liderVirtualPendente.DtLogin == null
                    )
                    {
                        var equipamentoDestino = await EquipamentoBLL.GetEquipamentoById(liderVirtualPendente.IdEquipamentoDestino ?? "");

                        if (equipamentoDestino == null)
                            throw new Exception("Equipamento não encontrado.");

                        if (equipamentoDestino.IdOperador == null)
                            return equipamentoDestino;
                    }

                return null;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task ValidaLoginLiderVirtual(string idOperador, string idEquipamentoDestino)
        {
            try
            {
                var liderVirtual = await LiderVirtualBLL.GetLiderVirtualInfoByDestino(idEquipamentoDestino);

                if (liderVirtual != null)
                    if (DateTime.Now <= liderVirtual.DtLoginLimite)
                        if (liderVirtual.IdOperador == idOperador)
                        {
                            if (liderVirtual.DtLogin == null)
                            {
                                liderVirtual.DtLogin = DateTime.Now;
                                liderVirtual.IdOperadorLogin = idOperador ?? "";
                                await LiderVirtualBLL.UpdateLiderVirtual(liderVirtual);
                            }
                        }
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}