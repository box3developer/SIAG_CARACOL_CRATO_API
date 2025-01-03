using System.Text.Json;
using Dapper;
using dotnet_api.Integration;
using dotnet_api.Models;
using dotnet_api.ModelsNodeRED;
using dotnet_api.ModelsSIAG;
using dotnet_api.Utils;
using grendene_caracois_api_csharp;
using Microsoft.Data.SqlClient;

namespace dotnet_api.BLLs
{
    public class CaixaBLL
    {
        public static async Task<CaixaSIAGModel?> GetCaixa(string idCaixa)
        {
            try
            {
                var query = "SELECT top 1 id_caixa AS IdCaixa, id_agrupador as IdAgrupador, id_pedido as IdPedido, dt_expedicao as DtExpedicao, dt_estufamento as DtEstufamento, id_pallet as IdPallet, fg_status as FgStatus, dt_sorter as DtSorter " +
                            "FROM caixa WITH(NOLOCK) " +
                            "WHERE id_caixa = @idCaixa";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var caixa = await conexao.QueryFirstOrDefaultAsync<CaixaSIAGModel>(query, new { idCaixa = idCaixa });

                    return caixa;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<bool> InserirDesempenho(string idCaixa, string? idOperador, string idEquipamento, string? idArea, int erroClassificacao)
        {
            try
            {
                var query = "EXEC sp_siag_gestaovisual_gravaperformance @idCaixa, null, @idOperador, @idEquipamento, @idArea, null, 0, 0, @erroClassificacao";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var linhas = await conexao.ExecuteAsync(query, new
                    {
                        idCaixa = idCaixa,
                        idOperador = idOperador,
                        idEquipamento = idEquipamento,
                        idArea = idArea,
                        erroClassificacao = erroClassificacao
                    });
                    return linhas > 0;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<string> GetFabrica(string idCaixa)
        {
            try
            {
                var queryPg = @"SELECT id_programa
                                FROM caixa WITH(NOLOCK)
                                WHERE id_caixa = @idCaixa";

                var query = @"SELECT cd_fabrica
                            FROM programa WITH(NOLOCK)
                            WHERE id_programa = @idPrograma";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var idPrograma = await conexao.QueryFirstOrDefaultAsync<string>(queryPg, new { idCaixa });
                    var fabrica = await conexao.QueryFirstOrDefaultAsync<string>(query, new { idPrograma });
                    return fabrica;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<Object> LeituraCaracolRefugo(string idCaixa, Guid id_requisicao)
        {
            try
            {
                string identificadorCaracol = await ParametroBLL.GetParamentro("Identificador do Caracol de Refugo");

                await LogBLL.GravarLog(
                        id_requisicao,
                        identificadorCaracol,
                        idCaixa,
                        $"Inicia validação da caixa {idCaixa} no caracol de regufo {identificadorCaracol}",
                        "LeituraCaracolRefugo",
                        "",
                        "info"
                    );

                var caracol = await EquipamentoBLL.GetEquipamentoByIdentificadorCaracol(identificadorCaracol);

                if (caracol == null)
                {
                    await LogBLL.GravarLog(
                            id_requisicao,
                            identificadorCaracol,
                            idCaixa,
                            $"Caracol de refugo {identificadorCaracol} não encontrado!",
                            "LeituraCaracolRefugo",
                            caracol.IdOperador,
                            "erro"
                        );

                    throw new Exception("Caracol não encontrado!");
                }


                if (idCaixa.Length != 16 && idCaixa.Length != 20)
                {
                    await LogBLL.GravarLog(
                        id_requisicao,
                        identificadorCaracol,
                        idCaixa,
                        $"Código {idCaixa} inválido. Não atende a quantidade de caracters padrões de uma caixa (16 ou 20 caracters)",
                        "LeituraCaracolRefugo",
                        caracol.IdOperador,
                        "erro"
                    );

                    throw new Exception("Código de barras inválido!");
                }

                var caixa = await SiagApi.GetCaixaByIdAsync(idCaixa);
                var performanceDia = await OperadorBLL.CalcularPerformanceTurnoAtual(caracol.IdOperador ?? "", id_requisicao);
                var performanceHora = await OperadorBLL.CalcularPerformanceHoraAtual(caracol.IdOperador ?? "", id_requisicao);

                // var mensagem = new ParametroMensagemCaracolSIAGModel();
                // var posicao = new PosicaoCaracolRefugoSIAGModel();
                var fabrica = await SiagApi.GetCaixaFabricaAsync(idCaixa);

                var retorno = new RetornoCaracolRefugoModel();

                if (caixa == null)
                {
                    await GravarSiagLog(idCaixa);
                    var posicao = await ParametroBLL.GetPosicaoCaracolRefugo("Caixa não encontrada", null);
                    var mensagem = await ParametroBLL.GetMensagem("Caracol refugo: sucesso ao classicar");
                    mensagem.Mensagem = mensagem.Mensagem?
                        .Replace("{tipo_classificacao}", posicao.Tipo)
                        .Replace("{posicao}", posicao.Posicao.ToString())
                        .Replace("{fabrica}", posicao.Fabrica?.ToString() ?? "");

                    await LogBLL.GravarLog(
                        id_requisicao,
                        identificadorCaracol,
                        idCaixa,
                        mensagem.Mensagem,
                        "LeituraCaracolRefugo",
                        caracol.IdOperador,
                        "info"
                    );

                    retorno = new RetornoCaracolRefugoModel()
                    {
                        Gaiola = posicao.Posicao,
                        Mensagem = mensagem,
                        PerformanceDia = performanceDia,
                        PerformanceHora = performanceHora
                    };
                }
                else
                {
                    PedidoSIAGModel? pedido = null;

                    if (caixa.IdPedido != null)
                        pedido = await GetPedido(caixa.IdPedido ?? "");

                    if (caixa.FgStatus == 6 || (pedido != null && pedido.CdLote == null))
                    {
                        await GravarLeituraCaracolRefugo(idCaixa, caracol.IdOperador ?? "", "1", "3", caracol.IdEquipamento, caracol.IdEquipamento?.Substring(0, 1) ?? "", 1);

                        var posicao = await ParametroBLL.GetPosicaoCaracolRefugo("Caixa cancelada", fabrica);

                        var mensagem = new ParametroMensagemCaracolSIAGModel();

                        if (string.IsNullOrWhiteSpace(posicao.Fabrica))
                            mensagem = await ParametroBLL.GetMensagem("Caracol refugo: sucesso ao classicar");
                        else
                            mensagem = await ParametroBLL.GetMensagem("Caracol refugo: sucesso ao classicar (com fabrica)");

                        mensagem.Mensagem = mensagem.Mensagem?
                            .Replace("{tipo_classificacao}", posicao.Tipo)
                            .Replace("{posicao}", posicao.Posicao.ToString())
                            .Replace("{fabrica}", posicao.Fabrica);

                        await LogBLL.GravarLog(
                            id_requisicao,
                            identificadorCaracol,
                            idCaixa,
                            mensagem.Mensagem,
                            "LeituraCaracolRefugo",
                            caracol.IdOperador,
                            "info"
                        );

                        retorno = new RetornoCaracolRefugoModel()
                        {
                            Gaiola = posicao.Posicao,
                            Mensagem = mensagem,
                            PerformanceDia = performanceDia,
                            PerformanceHora = performanceHora
                        };
                    }
                    else
                    {
                        if (caixa.FgStatus == 5 || caixa.IdPallet != null)
                        {
                            var caixaEstufada = await CaixaBLL.GetUltimaLeituraCaixa(caixa.IdCaixa ?? "");

                            if (caixaEstufada != null) { 
                            
                                var equipamentoEstufada = await EquipamentoBLL.GetEquipamentoById(caixaEstufada.IdEquipamento ?? "");

                                if (equipamentoEstufada == null)
                                    throw new Exception("Equipamento da última leitura não encontrado!");

                                var areaArmazenagemEstufada = await SiagApi.GetAreaArmazenagemById(long.Parse(caixaEstufada.IdAreaArmazenagem ?? ""));

                                if (areaArmazenagemEstufada == null)
                                    throw new Exception("Área de armazename da última leitura não encontrada!");

                                await GravarLeituraCaracolRefugo(idCaixa, caracol.IdOperador ?? "", "59", "1", caracol.IdEquipamento, caracol.IdEquipamento?.Substring(0, 1) ?? "");

                                var posicao = await ParametroBLL.GetPosicaoCaracolRefugo("Caixa com suspeita de duplicata", null);
                                var mensagem = await ParametroBLL.GetMensagem("Caracol refugo: sucesso ao classicar duplicada");
                                mensagem.Mensagem = mensagem.Mensagem?
                                    .Replace("{tipo_classificacao}", posicao.Tipo)
                                    .Replace("{posicao}", posicao.Posicao.ToString())
                                    .Replace("{equipamento}", equipamentoEstufada.NmEquipamento)
                                    .Replace("{gaiola}", areaArmazenagemEstufada.PosicaoY.ToString())
                                    .Replace("{pallet}", caixaEstufada.IdPallet)
                                    .Replace("{data}", caixaEstufada.DtLeitura.ToString("dd/MM/yyyy HH:mm:ss"))
                                    .Replace("{fabrica}", posicao.Fabrica?.ToString() ?? "");

                                await LogBLL.GravarLog(
                                    id_requisicao,
                                    identificadorCaracol,
                                    idCaixa,
                                    mensagem.Mensagem,
                                    "LeituraCaracolRefugo",
                                    caracol.IdOperador,
                                    "info"
                                );

                                retorno = new RetornoCaracolRefugoModel()
                                {
                                    Gaiola = posicao.Posicao,
                                    Mensagem = mensagem,
                                    PerformanceDia = performanceDia,
                                    PerformanceHora = performanceHora
                                };
                            } else
                            {
                                await GravarLeituraCaracolRefugo(idCaixa, caracol.IdOperador ?? "", "1", "59", caracol.IdEquipamento, caracol.IdEquipamento?.Substring(0, 1) ?? "");

                                var posicao = await ParametroBLL.GetPosicaoCaracolRefugo("Caixa com suspeita de duplicata", null);
                                var mensagem = await ParametroBLL.GetMensagem("Caracol refugo: sucesso ao classicar");
                                mensagem.Mensagem = mensagem.Mensagem?
                                    .Replace("{tipo_classificacao}", posicao.Tipo)
                                    .Replace("{posicao}", posicao.Posicao.ToString())
                                    .Replace("{fabrica}", posicao.Fabrica?.ToString() ?? "");

                                await LogBLL.GravarLog(
                                    id_requisicao,
                                    identificadorCaracol,
                                    idCaixa,
                                    mensagem.Mensagem,
                                    "LeituraCaracolRefugo",
                                    caracol.IdOperador,
                                    "info"
                                );

                                retorno = new RetornoCaracolRefugoModel()
                                {
                                    Gaiola = posicao.Posicao,
                                    Mensagem = mensagem,
                                    PerformanceDia = performanceDia,
                                    PerformanceHora = performanceHora
                                };
                            }
                        }
                        else if (caixa.IdAgrupador == null || await SiagApi.GetStatusAgrupadorAtivo(caixa.IdAgrupador ?? Guid.Empty) == 4)
                        {
                            //TODO NOVA DEMANDA await GravarLeituraCaracolRefugo(idCaixa, caracol.IdOperador ?? "",  "1",  "59", caracol.IdEquipamento, caracol.IdEquipamento?.Substring(0,1) ?? "");
                            var posicao = await ParametroBLL.GetPosicaoCaracolRefugo("Caixa de refugo", null);
                            var mensagem = await ParametroBLL.GetMensagem("Caracol refugo: sucesso ao classicar");
                            mensagem.Mensagem = mensagem.Mensagem?
                                .Replace("{tipo_classificacao}", posicao.Tipo)
                                .Replace("{posicao}", posicao.Posicao.ToString())
                                .Replace("{fabrica}", posicao.Fabrica?.ToString() ?? "");

                            await LogBLL.GravarLog(
                                id_requisicao,
                                identificadorCaracol,
                                idCaixa,
                                mensagem.Mensagem,
                                "LeituraCaracolRefugo",
                                caracol.IdOperador,
                                "info"
                            );

                            retorno = new RetornoCaracolRefugoModel()
                            {
                                Gaiola = posicao.Posicao,
                                Mensagem = mensagem,
                                PerformanceDia = performanceDia,
                                PerformanceHora = performanceHora
                            };
                        }

                        else if (caixa.FgStatus == 8 || caixa.DtSorter == null)
                        {
                            await GravarLeituraCaracolRefugo(idCaixa, caracol.IdOperador ?? "", "1", "14", caracol.IdEquipamento, caracol.IdEquipamento?.Substring(0, 1) ?? "");
                            var posicao = await ParametroBLL.GetPosicaoCaracolRefugo("Caixa não lida", null);
                            var mensagem = await ParametroBLL.GetMensagem("Caracol refugo: sucesso ao classicar");
                            mensagem.Mensagem = mensagem.Mensagem?
                                .Replace("{tipo_classificacao}", posicao.Tipo)
                                .Replace("{posicao}", posicao.Posicao.ToString())
                                .Replace("{fabrica}", posicao.Fabrica?.ToString() ?? "");

                            await LogBLL.GravarLog(
                                id_requisicao,
                                identificadorCaracol,
                                idCaixa,
                                mensagem.Mensagem,
                                "LeituraCaracolRefugo",
                                caracol.IdOperador,
                                "info"
                            );

                            retorno = new RetornoCaracolRefugoModel()
                            {
                                Gaiola = posicao.Posicao,
                                Mensagem = mensagem,
                                PerformanceDia = performanceDia,
                                PerformanceHora = performanceHora
                            };
                        }
                        else
                        {
                            var areaArmazenagemList = await SiagApi.GetAreaArmazenagemByAgrupador(caixa.IdAgrupador);
                            var areaArmazenagem = areaArmazenagemList[0];

                            if (areaArmazenagem == null)
                            {
                                await LogBLL.GravarLog(
                                    id_requisicao,
                                    identificadorCaracol,
                                    idCaixa,
                                    $"Agrupador {caixa.IdAgrupador} não localizado em nenhuma área de armazenagem",
                                    "LeituraCaracolRefugo",
                                    caracol.IdOperador,
                                    "erro"
                                );

                                throw new Exception("Área de Armazanagem não encontrada!");
                            }

                            var equipamentoCaixa = await EquipamentoBLL.GetEquipamentoByIdentificadorCaracol(areaArmazenagem.IdentificadorCaracol ?? "");

                            if (equipamentoCaixa == null)
                            {
                                await LogBLL.GravarLog(
                                    id_requisicao,
                                    identificadorCaracol,
                                    idCaixa,
                                    $"Equipamento {areaArmazenagem.IdentificadorCaracol} da área de armazenagem {areaArmazenagem.IdAreaArmazenagem} não localizado",
                                    "LeituraCaracolRefugo",
                                    caracol.IdOperador,
                                    "erro"
                                );

                                throw new Exception("Caracol da Caixa não encontrado!");
                            }

                            if (caracol.NmIdentificador != equipamentoCaixa.NmIdentificador)
                            {
                                await GravarLeituraCaracolRefugo(idCaixa, caracol.IdOperador ?? "", "1", "12", caracol.IdEquipamento, caracol.IdEquipamento?.Substring(0, 1) ?? "");
                                var posicao = await ParametroBLL.GetPosicaoCaracolRefugo("Caixa de erro de classificação", null);
                                var mensagem = await ParametroBLL.GetMensagem("Caracol refugo: sucesso ao classicar");
                                mensagem.Mensagem = mensagem.Mensagem?
                                    .Replace("{tipo_classificacao}", posicao.Tipo)
                                    .Replace("{posicao}", posicao.Posicao.ToString())
                                    .Replace("{fabrica}", posicao.Fabrica?.ToString() ?? "");

                                await LogBLL.GravarLog(
                                    id_requisicao,
                                    identificadorCaracol,
                                    idCaixa,
                                    mensagem.Mensagem,
                                    "LeituraCaracolRefugo",
                                    caracol.IdOperador,
                                    "info"
                                );

                                retorno = new RetornoCaracolRefugoModel()
                                {
                                    Gaiola = posicao.Posicao,
                                    Mensagem = mensagem,
                                    PerformanceDia = performanceDia,
                                    PerformanceHora = performanceHora
                                };
                            }
                            else
                            {
                                await LogBLL.GravarLog(
                                    id_requisicao,
                                    identificadorCaracol,
                                    idCaixa,
                                    $"Erro de classificação para caixa {idCaixa} no caracol de refugo {identificadorCaracol}",
                                    "LeituraCaracolRefugo",
                                    caracol.IdOperador,
                                    "erro"
                                );

                                throw new Exception("Caracol refugo: erro de classificação");
                            }
                        }
                    }
                }

                await LogBLL.GravarLog(
                    id_requisicao,
                    identificadorCaracol,
                    idCaixa,
                    $"Finalizada validação da caixa {idCaixa} no caracol de refugo {identificadorCaracol}",
                    "LeituraCaracolRefugo",
                    "",
                    "info"
                );

                await CaixaBLL.AcenderLuzVerde(identificadorCaracol ?? "", retorno.Gaiola);
                return retorno;
            }
            catch (Exception)
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    "",
                    idCaixa,
                    $"Erro ao realizar validação da caixa {idCaixa} no equipamento de refugo",
                    "LeituraCaracolRefugo",
                    "",
                    "erro"
                );

                throw;
            }
        }

        public static async Task<List<string>> GetCaixasTeste(string idCaracol)
        {
            try
            {
                var query = @"SELECT cast(id_caixa as varchar(50)) + ' - ' + cast(areaarmazenagem.nr_posicaoy as varchar(10))
                            FROM caixa WITH(NOLOCK)
                            LEFT JOIN agrupadorativo WITH(NOLOCK)
                                ON agrupadorativo.id_agrupador = caixa.id_agrupador
                            LEFT JOIN pallet WITH(NOLOCK)
                                ON pallet.id_areaarmazenagem = agrupadorativo.id_areaarmazenagem
                            LEFT join areaarmazenagem WITH(NOLOCK)
                                ON pallet.id_areaarmazenagem = areaarmazenagem.id_areaarmazenagem
                            WHERE caixa.fg_status < 4 AND 
                                  pallet.fg_status < 3 AND
                                  agrupadorativo.id_areaarmazenagem like '100" + idCaracol + "%'";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var caixas = await conexao.QueryAsync<string>(query);
                    return caixas.ToList();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
         
        public static async Task<int> AcenderLuzVerde(string identificadorCaracol, int? identificadorGaiola)
        {
            try
            {
                var resposta = await WebRequestUtil.GetRequest($"{Global.NodeRedUrl}/vd/{identificadorCaracol}/{identificadorGaiola}");
                return identificadorGaiola ?? 0;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<int> AcenderLuzVermelha(string identificadorCaracol, int? identificadorGaiola, Guid? id_requisicao)
        {
            try
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    "",
                    "",
                    $"Acendendo luz vermelha da gaiola {identificadorGaiola} do caracol {identificadorCaracol}",
                    "AcenderLuzVermelha",
                    "",
                    "info"
                );

                var resposta = await WebRequestUtil.GetRequest($"{Global.NodeRedUrl}/vm/{identificadorCaracol}/{identificadorGaiola}");
                return identificadorGaiola ?? 0;
            }
            catch (Exception)
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    "",
                    "",
                    $"Erro ao acender luz vermelha da gaiola {identificadorGaiola} do caracol {identificadorCaracol}",
                    "AcenderLuzVermelha",
                    "",
                    "erro"
                );

                throw;
            }
        }

        public static async Task<bool> EmitirEstufamento(string identificadorCaracol, Guid? id_requisicao)
        {
            try
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    identificadorCaracol,
                    "",
                    $"Emitindo estufamento para caracol {identificadorCaracol}",
                    "EmitirEstufamento",
                    "",
                    "info"
                );

                var resposta = await WebRequestUtil.GetRequest($"http://gra-lxsobcaracol.sob.ad-grendene.com:3000/EmitirEstufamento/{identificadorCaracol}");
                return true;
            }
            catch (Exception)
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    identificadorCaracol,
                    "",
                    $"Erro ao emitir estufamento para caracol {identificadorCaracol}",
                    "EmitirEstufamento",
                    "",
                    "erro"
                );

                throw;
            }
        }

        public static async Task<bool> EstufarCaixa(string idCaixa, Guid? id_requisicao)
        {
            try
            {
                var caixa = await SiagApi.GetCaixaByIdAsync(idCaixa);

                if (caixa == null)
                {
                    await LogBLL.GravarLog(
                        id_requisicao,
                        "",
                        idCaixa,
                        $"Caixa {idCaixa} não encontrada",
                        "EstufarCaixa",
                        "",
                        "erro"
                    );

                    throw new Exception("Caixa não encotrada!");
                }

                var pallet = await PalletBLL.GetPallet(caixa.IdPallet ?? "");

                if (pallet == null)
                {
                    await LogBLL.GravarLog(
                        id_requisicao,
                        "",
                        idCaixa,
                        $"Nenhum pallet encontrado para caixa {idCaixa}",
                        "EstufarCaixa",
                        "",
                        "erro"
                    );

                    throw new Exception("Pallet não encontrado.");
                }

                var areaArmazenagem = await SiagApi.GetAreaArmazenagemById(long.Parse(pallet.IdAreaArmazenagem ?? ""));

                if (areaArmazenagem == null)
                {
                    await LogBLL.GravarLog(
                        id_requisicao,
                        "",
                        idCaixa,
                        $"Nenhum área de armazenagem encontrada para pallet {pallet.IdPallet}",
                        "EstufarCaixa",
                        "",
                        "erro"
                    );

                    throw new Exception("Área de armazenagem não encontrada.");
                }

                var equipamento = await EquipamentoBLL.GetEquipamentoByIdentificadorCaracol(areaArmazenagem.IdentificadorCaracol ?? "");

                if (equipamento == null)
                {
                    await LogBLL.GravarLog(
                        id_requisicao,
                        "",
                        idCaixa,
                        $"Equipamento {areaArmazenagem.IdentificadorCaracol} da área de armazenagem {areaArmazenagem.IdAreaArmazenagem} não identificado",
                        "EstufarCaixa",
                        "",
                        "erro"
                    );

                    throw new Exception("Equipamento não encontrado.");
                }

                if (equipamento.CdCaixaPendente != null && equipamento.CdCaixaPendente == idCaixa)
                {
                    await LogBLL.GravarLog(
                        id_requisicao,
                        equipamento.IdEquipamento,
                        idCaixa,
                        $"Libera estufamento de caixa pendente no equipamento {equipamento.IdEquipamento}",
                        "EstufarCaixa",
                        "",
                        "info"
                    );

                    await LiberarCaixaPendenteEquipamento(equipamento);
                }

                var query = "UPDATE caixa " +
                            "SET dt_estufamento = @dataEstufamento " +
                            "WHERE id_caixa = @idCaixa";

                var queryEquipamento = @"UPDATE equipamento
                             SET cd_ultimaleitura = NULL, dt_ultimaleitura = NULL
                             WHERE id_equipamento = @idEquipamento";

                var queryHistorico = "INSERT INTO caixaleitura (id_caixa, dt_leitura, fg_tipo, fg_status, id_operador, id_equipamento, id_pallet, id_areaarmazenagem, id_endereco, fg_cancelado, id_ordem) " +
                                     "VALUES (@idCaixa, @dataLeitura, @tipo, @status, @idOperador, @idEquipamento, @idPallet, @idAreaArmazenagem, @idEndereco, @cancelado, NULL)";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var qtdLinhas = await conexao.ExecuteAsync(query, new { dataEstufamento = DateTime.Now, idCaixa = idCaixa });
                    var qtdLinhasEquipamento = await conexao.ExecuteAsync(queryEquipamento, new { idEquipamento = equipamento.IdEquipamento });
                    var qtdLinhasHistorico = await conexao.ExecuteAsync(queryHistorico, new
                    {
                        idCaixa = caixa.IdCaixa,
                        dataLeitura = DateTime.Now,
                        tipo = 19,
                        status = 1,
                        idOperador = equipamento.IdOperador,
                        idEquipamento = equipamento.IdEquipamento,
                        idPallet = pallet.IdPallet,
                        idAreaArmazenagem = areaArmazenagem.IdAreaArmazenagem,
                        idEndereco = areaArmazenagem.IdEndereco,
                        cancelado = 0
                    });

                    await LogBLL.GravarLog(
                        id_requisicao,
                        areaArmazenagem.IdentificadorCaracol,
                        idCaixa,
                        $"Caixa {caixa.IdCaixa} estufada no equipamento {equipamento.IdEquipamento} no pallet {pallet.IdPallet}",
                        "EstufarCaixa",
                        "",
                        "info"
                    );

                    return qtdLinhas > 0 && qtdLinhasHistorico > 0 && qtdLinhasEquipamento > 0;
                }
            }
            catch (Exception)
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    "",
                    idCaixa,
                    $"Erro ao estufar caixa {idCaixa}",
                    "EstufarCaixa",
                    "",
                    "info"
                );

                throw;
            }
        }

        public static async Task<bool> RemoverEstufamentoCaixa(string idCaixa)
        {
            try
            {
                var caixa = await SiagApi.GetCaixaByIdAsync(idCaixa);

                if (caixa == null)
                    throw new Exception("Caixa não encotrada!");

                var pallet = await PalletBLL.GetPallet(caixa.IdPallet ?? "");

                if (pallet == null)
                    throw new Exception("Pallet não encontrado.");

                var areaArmazenagem = await SiagApi.GetAreaArmazenagemById(long.Parse(pallet.IdAreaArmazenagem ?? ""));

                if (areaArmazenagem == null)
                    throw new Exception("Área de armazenagem não encontrada.");

                var equipamento = await EquipamentoBLL.GetEquipamentoByIdentificadorCaracol(areaArmazenagem.IdentificadorCaracol ?? "");

                if (equipamento == null)
                    throw new Exception("Equipamento não encontrado.");

                if (equipamento.CdCaixaPendente != null && equipamento.CdCaixaPendente == idCaixa)
                    await LiberarCaixaPendenteEquipamento(equipamento);

                var query = "UPDATE caixa " +
                            "SET dt_estufamento = null " +
                            "WHERE id_caixa = @idCaixa";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var qtdLinhas = await conexao.ExecuteAsync(query, new { dataEstufamento = DateTime.Now, idCaixa = idCaixa });

                    return true;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<bool> GravarLeitura(string idCaixa, string idArea, string idPallet)
        {
            try
            {
                var caixa = await SiagApi.GetCaixaByIdAsync(idCaixa);

                if (caixa == null)
                    throw new Exception("Caixa não encotrada!");

                var pallet = await PalletBLL.GetPallet(idPallet);

                if (pallet == null)
                    throw new Exception("Pallet não encontrado.");

                var areaArmazenagem = await SiagApi.GetAreaArmazenagemById(long.Parse(idArea));

                if (areaArmazenagem == null)
                    throw new Exception("Área de armazenagem não encontrada.");

                var equipamento = await EquipamentoBLL.GetEquipamentoByIdentificadorCaracol(areaArmazenagem.IdentificadorCaracol ?? "");

                if (equipamento == null)
                    throw new Exception("Equipamento não encontrado.");

                if (equipamento.CdCaixaPendente != null && equipamento.CdCaixaPendente == idCaixa)
                    await LiberarCaixaPendenteEquipamento(equipamento);

                var query = @"UPDATE equipamento
                             SET cd_ultimaleitura = @idCaixa, dt_ultimaleitura = @dataLeitura
                             WHERE id_equipamento = @idEquipamento";

                var queryHistorico = "INSERT INTO caixaleitura (id_caixa, dt_leitura, fg_tipo, fg_status, id_operador, id_equipamento, id_pallet, id_areaarmazenagem, id_endereco, fg_cancelado, id_ordem) " +
                                     "VALUES (@idCaixa, @dataLeitura, @tipo, @status, @idOperador, @idEquipamento, @idPallet, @idAreaArmazenagem, @idEndereco, @cancelado, NULL)";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var dataLeitura = DateTime.Now;

                    var qtdLinhas = await conexao.ExecuteAsync(query, new
                    {
                        idCaixa = caixa.IdCaixa,
                        idEquipamento = equipamento.IdEquipamento,
                        dataLeitura = dataLeitura
                    });

                    var qtdLinhasHistorico = await conexao.ExecuteAsync(queryHistorico, new
                    {
                        idCaixa = caixa.IdCaixa,
                        dataLeitura = dataLeitura,
                        tipo = 3,
                        status = 1,
                        idOperador = equipamento.IdOperador,
                        idEquipamento = equipamento.IdEquipamento,
                        idPallet = pallet.IdPallet,
                        idAreaArmazenagem = areaArmazenagem.IdAreaArmazenagem,
                        idEndereco = areaArmazenagem.IdEndereco,
                        cancelado = 0
                    });

                    return qtdLinhasHistorico > 0 && qtdLinhas > 0;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<bool> GravarLeituraCaracolRefugo(string idCaixa, string idOperador, string? fgTipo, string? fgStatus, string? idEquipamento, string? idEndereco, int cancelado = 0)
        {
            try
            {
                var queryHistorico = "INSERT INTO caixaleitura (id_caixa, dt_leitura, fg_tipo, fg_status, id_operador, id_equipamento, id_pallet, id_areaarmazenagem, id_endereco, fg_cancelado, id_ordem) " +
                                     "VALUES (@idCaixa, @dataLeitura, @tipo, @status, @idOperador, @idEquipamento, NULL, NULL, @idEndereco, @cancelado, NULL)";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var dataLeitura = DateTime.Now;

                    var qtdLinhasHistorico = await conexao.ExecuteAsync(queryHistorico, new
                    {
                        idCaixa = idCaixa,
                        dataLeitura = dataLeitura,
                        tipo = fgTipo,
                        status = fgStatus,
                        idOperador = idOperador,
                        idEquipamento = idEquipamento,
                        idEndereco = idEndereco,
                        cancelado = cancelado
                    });

                    return qtdLinhasHistorico > 0;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<bool> GravarCaixaHistorico(string idCaixa, string identificadorCaracol)
        {
            try
            {
                var equipamento = await EquipamentoBLL.GetEquipamentoByIdentificadorCaracol(identificadorCaracol ?? "");

                if (equipamento == null)
                    identificadorCaracol = "NULL";

                var queryHistorico = "INSERT INTO caixahistorico (id_caixa, id_pallet, id_operador, dt_sorter, dt_estufamento, dt_expedicao, id_equipamento, nr_sorteresteira, nr_sortercorredor, nr_sortergaiola, dt_caracol, nr_pares) " +
                                     "VALUES (@idCaixa, null, null, null, null, null, @idEquipamento, null, null, null, null, null)";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var qtdLinhasHistorico = await conexao.ExecuteAsync(queryHistorico, new
                    {
                        idCaixa = idCaixa,
                        idEquipamento = identificadorCaracol
                    });

                    return qtdLinhasHistorico > 0;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<bool> GravarErro(string idCaixa, string idEquipamento, string idOperador, string? idAreaArmazenagem, string? idEndereco, int tipo, int status)
        {
            try
            {
                var queryHistorico = "INSERT INTO caixaleitura (id_caixa, dt_leitura, fg_tipo, fg_status, id_operador, id_equipamento, id_pallet, id_areaarmazenagem, id_endereco, fg_cancelado, id_ordem) " +
                                     "VALUES (@idCaixa, @dataLeitura, @tipo, @status, @idOperador, @idEquipamento, NULL, @idAreaArmazenagem, @idEndereco, @cancelado, NULL)";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var qtdLinhasHistorico = await conexao.ExecuteAsync(queryHistorico, new
                    {
                        idCaixa = idCaixa,
                        dataLeitura = DateTime.Now,
                        tipo = tipo,
                        status = status,
                        idOperador = idOperador,
                        idEquipamento = idEquipamento,
                        idAreaArmazenagem = idAreaArmazenagem,
                        idEndereco = idEndereco,
                        cancelado = 0
                    });

                    return qtdLinhasHistorico > 0;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<bool> TemCaixasPendentes(string idAgrupador)
        {
            try
            {
                var query = @"SELECT count(*)
                              FROM caixa WITH(NOLOCK)
                              WHERE id_agrupador = @idAgrupador AND (fg_status < 4 OR fg_status = 8)";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var qtd = await conexao.QueryFirstOrDefaultAsync<int>(query, new { idAgrupador = idAgrupador });

                    return qtd > 0;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<bool> FinalizarAgrupador(string idAgrupador, Guid? id_requisicao)
        {
            try
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    "",
                    "",
                    $"Finalizando agrupador {idAgrupador}. Troca status do agrupador para 4",
                    "FinalizarAgrupador",
                    "",
                    "info"
                );

                var query = "UPDATE agrupadorativo " +
                            "SET fg_status = 4 " +
                            "WHERE id_agrupador = @idAgrupador";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var qtdLinhas = await conexao.ExecuteAsync(query, new { idAgrupador = idAgrupador });

                    return qtdLinhas > 0;
                }
            }
            catch (Exception)
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    "",
                    "",
                    $"Erro ao finalizar agrupador {idAgrupador}",
                    "FinalizarAgrupador",
                    "",
                    "erro"
                );

                throw;
            }
        }

        public static async Task<bool> LiberarAgrupador(string idAgrupador, Guid? id_requisicao)
        {
            try
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    "",
                    "",
                    $"Removendo vínculo com área de armazenagem do agrupador {idAgrupador}",
                    "LiberarAgrupador",
                    "",
                    "info"
                );

                var query = "UPDATE agrupadorativo " +
                            "SET id_areaarmazenagem = NULL " +
                            "WHERE id_agrupador = @idAgrupador";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var qtdLinhas = await conexao.ExecuteAsync(query, new { idAgrupador = idAgrupador });

                    return qtdLinhas > 0;
                }
            }
            catch (Exception)
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    "",
                    "",
                    $"Erro ao remover vínculo com área de armazenagem do agrupador {idAgrupador}",
                    "LiberarAgrupador",
                    "",
                    "erro"
                );

                throw;
            }
        }

        public static async Task<bool> LiberarAreaArmazenagem(string idAgrupador, Guid? id_requisicao)
        {
            try
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    "",
                    "",
                    $"Liberando áreas de armazenagem do agrupador {idAgrupador}",
                    "LiberarAreaArmazenagem",
                    "",
                    "info"
                );

                var query = "UPDATE areaarmazenagem " +
                            "SET id_agrupador = NULL, fg_status = 1 " +
                            "WHERE id_agrupador = @idAgrupador";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var qtdLinhas = await conexao.ExecuteAsync(query, new { idAgrupador = idAgrupador });

                    return qtdLinhas > 0;
                }
            }
            catch (Exception)
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    "",
                    "",
                    $"Erro ao liberar áreas de armazenagem do agrupador {idAgrupador}",
                    "LiberarAreaArmazenagem",
                    "",
                    "erro"
                );

                throw;
            }
        }

        public static async Task<bool> EncherPallet(string idPallet, Guid? id_requisicao)
        {
            try
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    "",
                    "",
                    $"Trocando status do pallet {idPallet} para cheio",
                    "EncherPallet",
                    "",
                    "info"
                );

                var query = @"UPDATE pallet
                            SET fg_status = 3
                            WHERE id_pallet = @idPallet";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var qtdLinhas = await conexao.ExecuteAsync(query, new { idPallet = idPallet });

                    return qtdLinhas > 0;
                }
            }
            catch (Exception)
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    "",
                    "",
                    $"Erro ao trocar status do pallet {idPallet} para cheio",
                    "EncherPallet",
                    "",
                    "erro"
                );

                throw;
            }
        }

        public static async Task<bool> VincularCaixaComPallet(string identificadorCaracol, int posicaoY, string idCaixa, string idAgrupador, Guid? id_requisicao)
        {
            try
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    identificadorCaracol,
                    idCaixa,
                    $"Inciando vínculo da caixa {idCaixa} com agrupador {idAgrupador} na posicção {posicaoY} do equipamento {identificadorCaracol}",
                    "VincularCaixaComPallet",
                    "",
                    "info"
                );

                var areaArmazenagem = await SiagApi.GetAreaArmazenagemByPosicao(identificadorCaracol, posicaoY);

                if (areaArmazenagem == null)
                {
                    await LogBLL.GravarLog(
                        id_requisicao,
                        identificadorCaracol,
                        idCaixa,
                        $"Não foi possível identificar área de armazenagem na posição {posicaoY} do equipamento {identificadorCaracol}",
                        "VincularCaixaComPallet",
                        "",
                        "erro"
                    );

                    throw new Exception("Área de armazenagem não encontrada.");
                }

                var pallet = await PalletBLL.GetPalletByIdAreaArmazenagem(areaArmazenagem.IdAreaArmazenagem);

                if (pallet == null)
                {
                    await LogBLL.GravarLog(
                        id_requisicao,
                        identificadorCaracol,
                        idCaixa,
                        $"Nenhum pallet encontrado para a área de armazenagem {areaArmazenagem.IdAreaArmazenagem}",
                        "VincularCaixaComPallet",
                        "",
                        "erro"
                    );

                    throw new Exception("Pallet não encontrado.");
                }

                if (pallet.IdAgrupador == null)
                {
                    await LogBLL.GravarLog(
                        id_requisicao,
                        identificadorCaracol,
                        idCaixa,
                        $"Vincula agrupador {idAgrupador} no pallet {pallet.IdPallet} e altera status do pallet para 2",
                        "VincularCaixaComPallet",
                        "",
                        "info"
                    );

                    await LogBLL.GravarLog(
                        id_requisicao,
                        identificadorCaracol,
                        idCaixa,
                        $"Atualiza área {areaArmazenagem.IdAreaArmazenagem} para status 3",
                        "VincularCaixaComPallet",
                        "",
                        "info"
                    );

                    var queryPallet = "UPDATE pallet " +
                            "SET id_agrupador = @idAgrupador, fg_status = 2 " +
                            "WHERE id_pallet = @idPallet";

                    var queryAreaArmazenagem = "UPDATE areaarmazenagem " +
                            "SET fg_status = 3 " +
                            "WHERE id_areaarmazenagem = @idAreaArmazenagem";

                    using (var conexao = new SqlConnection(Global.Conexao))
                    {
                        await conexao.ExecuteAsync(queryPallet, new { idPallet = pallet.IdPallet, idAgrupador = idAgrupador });
                        await conexao.ExecuteAsync(queryAreaArmazenagem, new { idAreaArmazenagem = areaArmazenagem.IdAreaArmazenagem });
                    }
                }

                var query = "UPDATE caixa " +
                            "SET id_pallet = @idPallet, fg_status = 4 " +
                            "WHERE id_caixa = @idCaixa";

                await LogBLL.GravarLog(
                        id_requisicao,
                        identificadorCaracol,
                        idCaixa,
                        $"Vincula pallet {pallet.IdPallet} na caixa {idCaixa} e altera status da caixa para 4",
                        "VincularCaixaComPallet",
                        "",
                        "info"
                    );

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var qtdLinhas = await conexao.ExecuteAsync(query, new { idPallet = pallet.IdPallet, idCaixa = idCaixa });

                    await LogBLL.GravarLog(
                        id_requisicao,
                        identificadorCaracol,
                        idCaixa,
                        $"Finalizado vínculo da caixa {idCaixa} com agrupador {idAgrupador} na posicção {posicaoY} do equipamento {identificadorCaracol}",
                        "VincularCaixaComPallet",
                        "",
                        "info"
                    );

                    return qtdLinhas > 0 ? true : false;
                }
            }
            catch (Exception)
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    identificadorCaracol,
                    idCaixa,
                    $"Erro ao vincular caixa {idCaixa} com agrupador {idAgrupador} na posicção {posicaoY} do equipamento {identificadorCaracol}",
                    "VincularCaixaComPallet",
                    "",
                    "erro"
                );

                throw;
            }
        }

        public static async Task<bool> DesvincularCaixaComPallet(string identificadorCaracol, int posicaoY, string idCaixa)
        {
            try
            {
                var areaArmazenagem = await SiagApi.GetAreaArmazenagemByPosicao(identificadorCaracol, posicaoY);

                if (areaArmazenagem == null)
                    throw new Exception("Área de armazenagem não encontrada.");

                var pallet = await PalletBLL.GetPalletByIdAreaArmazenagem(areaArmazenagem.IdAreaArmazenagem);

                if (pallet == null)
                    throw new Exception("Pallet não encontrado.");

                var query = "UPDATE caixa " +
                            "SET id_pallet = null, fg_status = 3 " +
                            "WHERE id_caixa = @idCaixa";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var qtdLinhas = await conexao.ExecuteAsync(query, new { idCaixa = idCaixa });

                    return qtdLinhas > 0 ? true : false;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<CaixaLeituraSIAGModel?> GetUltimaLeitura(string idEquipamento, int fgStatus, int fgTipo)
        {
            try
            {
                var query = @"SELECT TOP 1 id_caixaleitura AS IdCaixaLeitura,
                                id_caixa AS IdCaixa,
                                dt_leitura AS DtLeitura,
                                fg_tipo AS FgTipo,
                                fg_status AS FgStatus,
                                id_operador AS IdOperador,
                                id_equipamento AS IdEquipamento,
                                id_pallet AS IdPallet,
                                id_areaarmazenagem AS IdAreaArmazenagem,
                                id_endereco AS IdEndereco,
                                fg_cancelado AS FgCancelado,
                                id_ordem AS IdOrdem
                            FROM caixaleitura WITH(NOLOCK)
                            WHERE id_equipamento = @idEquipamento
                                AND fg_status = @fgStatus
                                AND fg_tipo = @fgTipo
                            ORDER BY id_caixaleitura DESC";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var leitura = await conexao.QueryFirstOrDefaultAsync<CaixaLeituraSIAGModel>(query, new
                    {
                        idEquipamento = idEquipamento,
                        fgStatus = fgStatus,
                        fgTipo = fgTipo
                    });
                    return leitura;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<CaixaLeituraSIAGModel?> GetUltimaLeituraCaixa(string idCaixa)
        {
            try
            {
                var query = @"SELECT TOP 1 id_caixaleitura AS IdCaixaLeitura,
                                id_caixa AS IdCaixa,
                                dt_leitura AS DtLeitura,
                                fg_tipo AS FgTipo,
                                fg_status AS FgStatus,
                                id_operador AS IdOperador,
                                id_equipamento AS IdEquipamento,
                                id_pallet AS IdPallet,
                                id_areaarmazenagem AS IdAreaArmazenagem,
                                id_endereco AS IdEndereco,
                                fg_cancelado AS FgCancelado,
                                id_ordem AS IdOrdem
                            FROM caixaleitura WITH(NOLOCK)
                            WHERE id_caixa = @idCaixa
                                AND fg_tipo = 19
                            ORDER BY id_caixaleitura DESC";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var leitura = await conexao.QueryFirstOrDefaultAsync<CaixaLeituraSIAGModel>(query, new
                    {
                        idCaixa = idCaixa
                    });
                    return leitura;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<List<CaixaLeituraSIAGModel>> GetLeiturasTeste(string idEquipamento)
        {
            try
            {
                var query = @"SELECT TOP 5 id_caixa AS IdCaixa, fg_tipo AS FgTipo, id_areaarmazenagem as IdAreaArmazenagem, id_equipamento as IdEquipamento
                            FROM caixaleitura WITH(NOLOCK)
                            WHERE id_equipamento = @idEquipamento
                            ORDER BY id_caixaleitura DESC ";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var caixas = await conexao.QueryAsync<CaixaLeituraSIAGModel>(query, new { idEquipamento = idEquipamento });
                    return caixas.ToList();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<bool> GravarSiagLog(string idCaixa)
        {
            try
            {
                var query = @"INSERT INTO siaglog (data, tipo, usuario, historico)
                            VALUES (@data, 'NaoEncontrado', 'Caracol', @caixa)";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var linhas = await conexao.ExecuteAsync(query, new
                    {
                        data = DateTime.Now,
                        caixa = $"<caixa>{idCaixa}</caixa>"
                    });

                    return linhas > 0;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<int> GetAgrupadorStatus(string idAgrupador)
        {
            try
            {

                var query = @"SELECT fg_status
                            FROM agrupadorativo WITH(NOLOCK)
                            WHERE id_agrupador = @idAgrupador";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var status = await conexao.QueryFirstOrDefaultAsync<int>(query, new { idAgrupador });
                    return status;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<PedidoSIAGModel> GetPedido(string idPedido)
        {
            try
            {

                var query = @"SELECT TOP 1 id_pedido AS IdPedido, cd_lote AS CdLote
                            FROM pedido WITH(NOLOCK)
                            WHERE id_pedido = @idPedido";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var pedido = await conexao.QueryFirstOrDefaultAsync<PedidoSIAGModel>(query, new { idPedido });
                    return pedido;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task SetCaixaPendente(string? idCaixa, string idEquipamento)
        {
            try
            {
                var queryEquipamento = @"UPDATE equipamento
                             SET cd_leitura_pendente = @idCaixa
                             WHERE id_equipamento = @idEquipamento";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var caixaPendente = await conexao.QueryAsync(queryEquipamento, new
                    {
                        idCaixa = idCaixa,
                        idEquipamento = idEquipamento
                    });
                }
            }
            catch (System.Exception)
            {

                throw;
            }
        }

        public static async Task<EquipamentoSIAGModel> LiberarCaixaPendenteEquipamento(EquipamentoSIAGModel? equipamento)
        {
            await SetCaixaPendente(null, equipamento.IdEquipamento ?? "");
            equipamento.CdCaixaPendente = null;

            return equipamento;
        }
    }
}
