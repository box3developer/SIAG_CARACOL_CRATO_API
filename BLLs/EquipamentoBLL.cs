using System.Text.Json;
using Dapper;
using dotnet_api.DTOs;
using dotnet_api.Integration;
using dotnet_api.Models;
using dotnet_api.ModelsNodeRED;
using dotnet_api.ModelsSIAG;
using dotnet_api.Utils;
using grendene_caracois_api_csharp;
using Microsoft.Data.SqlClient;

namespace dotnet_api.BLLs
{
    public class EquipamentoBLL
    {
        private static Guid? id_requisicao;

        public static void ConfigurarIdRequisicao(Guid? idRequisicao)
        {
            id_requisicao = idRequisicao;
        }

        public static async Task<EquipamentoSIAGModel?> GetEquipamentoByOperador(string cracha)
        {
            try
            {
                var query = "SELECT id_equipamento AS IdEquipamento, " +
                                   "nm_equipamento AS NmEquipamento, " +
                                   "id_equipamentomodelo AS IdEquipamentoModelo, " +
                                   "nm_identificador AS NmIdentificador, " +
                                   "cd_ultimaleitura AS CdUltimaLeitura, " +
                                   "dt_ultimaleitura AS DtUltimaLeitura, " +
                                   "id_operador AS IdOperador " +
                            "FROM equipamento WITH(NOLOCK) " +
                            "WHERE id_operador = @cracha";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var equipamento = await conexao.QueryFirstOrDefaultAsync<EquipamentoSIAGModel>(query, new { cracha = cracha });

                    return equipamento;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<EquipamentoSIAGModel?> GetEquipamentoById(string idEquipamento)
        {
            try
            {
                var query = "SELECT id_equipamento AS IdEquipamento, " +
                                   "nm_equipamento AS NmEquipamento, " +
                                   "id_equipamentomodelo AS IdEquipamentoModelo, " +
                                   "nm_identificador AS NmIdentificador, " +
                                   "cd_ultimaleitura AS CdUltimaLeitura, " +
                                   "dt_ultimaleitura AS DtUltimaLeitura, " +
                                   "id_operador AS IdOperador " +
                            "FROM equipamento WITH(NOLOCK) " +
                            "WHERE id_equipamento = @idEquipamento";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var equipamento = await conexao.QueryFirstOrDefaultAsync<EquipamentoSIAGModel>(query, new { idEquipamento = idEquipamento });

                    return equipamento;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<List<EquipamentoModel>> GetAllCaracois()
        {
            try
            {
                var query = "SELECT id_equipamento AS IdEquipamento, " +
                                   "nm_equipamento AS NmEquipamento, " +
                                   "nm_identificador AS NmIdentificador, " +
                                   "id_operador AS IdOperador " +
                            "FROM equipamento WITH(NOLOCK) " +
                            "WHERE id_equipamentomodelo = 1";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var equipamentos = await conexao.QueryAsync<EquipamentoModel>(query);
                    return equipamentos.ToList();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<List<EquipamentoModel>> GetAllCaracoisWithOperador()
        {
            try
            {
                var query = "SELECT id_equipamento AS IdEquipamento, " +
                                   "nm_equipamento AS NmEquipamento, " +
                                   "nm_identificador AS NmIdentificador, " +
                                   "id_operador AS IdOperador " +
                            "FROM equipamento WITH(NOLOCK) " +
                            "WHERE id_equipamentomodelo = 1";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var equipamentos = await conexao.QueryAsync<EquipamentoModel>(query);

                    foreach (var equipamento in equipamentos)
                        if (equipamento.IdOperador != null)
                        {
                            var operadorSIAG = await OperadorBLL.GetOperadorByCracha(equipamento.IdOperador);

                            if (operadorSIAG == null)
                                throw new Exception("Operador não encontrado.");

                            equipamento.Operador = new OperadorModel
                            {
                                Cracha = operadorSIAG.IdOperador,
                                Nome = operadorSIAG.NmOperador,
                                Cpf = operadorSIAG.NmCpf,
                                Foto = $"http://cdsrvsob.sob.ad-grendene.com/SIAG/WebService/hdlBuscaFotoColaborador.ashx?sCPF={operadorSIAG.NmCpf}"
                            };
                        }

                    return equipamentos.ToList();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<List<EquipamentoModel>> GetCaracoisInfoRelativeTo(string identificadorCaracol)
        {
            try
            {
                var equipamentos = (await GetAllCaracois()).ToDictionary(x => x.NmIdentificador ?? "", x => x);
                var caixasPendentes = await EquipamentoBLL.GetQtdCaixasPendentesLiderVirtual();
                var caracoisCheios = (await EquipamentoBLL.GetCaracoisCheios()).Where(x => x.Cheio == 1).Select(x => x.Caracol);
                var niveis = EquipamentoBLL.GetNiveis(identificadorCaracol);
                var caracoisLV = await LiderVirtualBLL.GetLiderVitualInfo();
                var listaResposta = new List<EquipamentoModel>();

                foreach (string idCaracol in niveis.Keys)
                {
                    var IdEquipamento = equipamentos[idCaracol].IdEquipamento ?? "";
                    equipamentos[idCaracol].Cheio = caracoisCheios.Contains(idCaracol);
                    equipamentos[idCaracol].CaixasPendentes = caixasPendentes.Where(x => x.Key == idCaracol).Select(x => x.Value).FirstOrDefault();
                    equipamentos[idCaracol].Nivel = niveis[idCaracol];
                    equipamentos[idCaracol].LiderVirtual = caracoisLV.Where(x => x.Key == IdEquipamento).Select(x => x.Value).FirstOrDefault();

                    listaResposta.Add(equipamentos[idCaracol]);
                }

                return listaResposta
                            .OrderBy(x => x.Nivel)
                            .ThenByDescending(x => x.CaixasPendentes)
                            .ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<bool> LimparUltimaLeitura(string idEquipamento, string idCaixa)
        {
            try
            {
                var equipamento = await GetEquipamentoById(idEquipamento);

                if (equipamento != null && equipamento.CdUltimaLeitura == idCaixa)
                {
                    var query = @"UPDATE equipamento
                                SET cd_ultimaleitura = null
                                WHERE id_equipamento = @idEquipamento";

                    using (var conexao = new SqlConnection(Global.Conexao))
                    {
                        var linhas = await conexao.ExecuteAsync(query, new { idEquipamento = idEquipamento });

                        return linhas > 0;
                    }
                }

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<EquipamentoSIAGModel?> GetEquipamentoByIdentificadorCaracol(string identificadorCaracol)
        {
            try
            {
                var query = "SELECT id_equipamento AS IdEquipamento," +
                                   "nm_equipamento AS NmEquipamento, " +
                                   "id_equipamentomodelo AS IdEquipamentoModelo, " +
                                   "nm_identificador AS NmIdentificador, " +
                                   "cd_ultimaleitura AS CdUltimaLeitura, " +
                                   "cd_leitura_pendente AS CdCaixaPendente, " +
                                   "dt_ultimaleitura AS DtUltimaLeitura, " +
                                   "id_operador AS IdOperador " +
                            "FROM equipamento WITH(NOLOCK) " +
                            "WHERE nm_identificador = @identificadorCaracol";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var equipamento = await conexao.QueryFirstOrDefaultAsync<EquipamentoSIAGModel>(query, new { identificadorCaracol = identificadorCaracol });

                    return equipamento;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static Dictionary<string, int> GetNiveis(string caracolAtual)
        {
            var posicaoAtual = Global.Mapa[caracolAtual];
            var niveis = new Dictionary<string, int>();

            foreach (var (caracol, posicao) in Global.Mapa)
            {
                if ((posicao[0] == posicaoAtual[0] - 1 && posicao[1] == posicaoAtual[1]) || (posicao[0] == posicaoAtual[0] && posicao[1] == posicaoAtual[1] + 1) || (posicao[0] == posicaoAtual[0] && posicao[1] == posicaoAtual[1] - 1) || (posicao[0] == posicaoAtual[0] + 1 && posicao[1] == posicaoAtual[1]))
                    niveis.Add(caracol, 2);
                else if ((posicao[0] == posicaoAtual[0] - 1 && posicao[1] == posicaoAtual[1] - 1) || (posicao[0] == posicaoAtual[0] - 1 && posicao[1] == posicaoAtual[1] + 1) || (posicao[0] == posicaoAtual[0] && posicao[1] == posicaoAtual[1] + 2) || (posicao[0] == posicaoAtual[0] && posicao[1] == posicaoAtual[1] - 2) || (posicao[0] == posicaoAtual[0] + 1 && posicao[1] == posicaoAtual[1] - 1) || (posicao[0] == posicaoAtual[0] + 1 && posicao[1] == posicaoAtual[1] + 1))
                    niveis.Add(caracol, 3);
                else if ((posicao[0] == posicaoAtual[0] - 1 && posicao[1] == posicaoAtual[1] - 2) || (posicao[0] == posicaoAtual[0] - 1 && posicao[1] == posicaoAtual[1] + 2) || (posicao[0] == posicaoAtual[0] && posicao[1] == posicaoAtual[1] + 3) || (posicao[0] == posicaoAtual[0] && posicao[1] == posicaoAtual[1] - 3) || (posicao[0] == posicaoAtual[0] + 1 && posicao[1] == posicaoAtual[1] - 2) || (posicao[0] == posicaoAtual[0] + 1 && posicao[1] == posicaoAtual[1] + 2))
                    niveis.Add(caracol, 4);
                else if ((posicao[0] == posicaoAtual[0] - 1 && posicao[1] == posicaoAtual[1] - 3) || (posicao[0] == posicaoAtual[0] - 1 && posicao[1] == posicaoAtual[1] + 3) || (posicao[0] == posicaoAtual[0] && posicao[1] == posicaoAtual[1] + 4) || (posicao[0] == posicaoAtual[0] && posicao[1] == posicaoAtual[1] - 4) || (posicao[0] == posicaoAtual[0] + 1 && posicao[1] == posicaoAtual[1] - 3) || (posicao[0] == posicaoAtual[0] + 1 && posicao[1] == posicaoAtual[1] + 3))
                    niveis.Add(caracol, 5);
                else if ((posicao[0] == posicaoAtual[0] && posicao[1] == posicaoAtual[1] + 5) || (posicao[0] == posicaoAtual[0] && posicao[1] == posicaoAtual[1] - 5))
                    niveis.Add(caracol, 6);
                else if ((posicao[0] == posicaoAtual[0] - 1 && posicao[1] < posicaoAtual[1] - 3) || (posicao[0] == posicaoAtual[0] - 1 && posicao[1] > posicaoAtual[1] + 3) || ((posicao[0] == posicaoAtual[0] && posicao[1] > posicaoAtual[1] + 5) || (posicao[0] == posicaoAtual[0] && posicao[1] < posicaoAtual[1] - 5)) || (posicao[0] == posicaoAtual[0] + 1 && posicao[1] < posicaoAtual[1] - 3) || (posicao[0] == posicaoAtual[0] + 1 && posicao[1] > posicaoAtual[1] + 3))
                    niveis.Add(caracol, 7);
                else if ((posicao[0] > posicaoAtual[0] + 1) || (posicao[0] < posicaoAtual[0] - 1))
                    niveis.Add(caracol, 8);
                else
                    niveis.Add(caracol, 1);
            }

            return niveis.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        }

        public static async Task<CaracolNodeREDModel?> GetCaracol(string identificadorCaracol)
        {
            try
            {
                var resposta = await WebRequestUtil.GetRequest($"{Global.NodeRedUrl}/luzes/{identificadorCaracol}");
                var caracol = JsonSerializer.Deserialize<CaracolNodeREDModel>(resposta);
                return caracol;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<List<CaracolNodeREDModel>> GetCaracoisCheios()
        {
            try
            {
                var resposta = await WebRequestUtil.GetRequest($"{Global.NodeRedUrl}/caracolcheio");
                var caracois = JsonSerializer.Deserialize<List<CaracolNodeREDModel>>(resposta) ?? new List<CaracolNodeREDModel>();
                return caracois.ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<Dictionary<string, int>> GetQtdCaixasPendentes()
        {
            try
            {
                var query = @"select CAST(areaarmazenagem.id_endereco AS varchar(10)) + RIGHT('00' + CAST(nr_posicaox AS varchar(10)), 2) as Item1, count(*) as Item2
                              from caixa WITH(NOLOCK)
                              left join agrupadorativo WITH(NOLOCK)
                                  on agrupadorativo.id_agrupador = caixa.id_agrupador
                              inner join areaarmazenagem WITH(NOLOCK)
                                  on agrupadorativo.id_areaarmazenagem = areaarmazenagem.id_areaarmazenagem
                              where caixa.fg_status < 4 AND caixa.dt_sorter IS NOT NULL AND caixa.dt_estufamento IS NULL
                              group by CAST(areaarmazenagem.id_endereco AS varchar(10)) + RIGHT('00' + CAST(nr_posicaox AS varchar(10)), 2)
                              order by CAST(areaarmazenagem.id_endereco AS varchar(10)) + RIGHT('00' + CAST(nr_posicaox AS varchar(10)), 2) desc";
                //TODO: hora atual - 5
                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var qtdCaixasPendentes = await conexao.QueryAsync<Tuple<string, int>>(query);
                    return qtdCaixasPendentes.ToDictionary(x => x.Item1, x => x.Item2);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<Dictionary<string, int>> GetQtdCaixasPendentesLiderVirtual()
        {
            try
            {
                var query = @"select CAST(areaarmazenagem.id_endereco AS varchar(10)) + RIGHT('00' + CAST(nr_posicaox AS varchar(10)), 2) as Item1, count(*) as Item2
                                from caixa WITH(NOLOCK)
                                left join agrupadorativo WITH(NOLOCK)
                                    on agrupadorativo.id_agrupador = caixa.id_agrupador
                                inner join areaarmazenagem WITH(NOLOCK)
                                    on agrupadorativo.id_areaarmazenagem = areaarmazenagem.id_areaarmazenagem
                                left join equipamento WITH(NOLOCK)
                                    on equipamento.nm_identificador = 
                                            CAST(areaarmazenagem.id_endereco AS varchar(10)) + RIGHT('00' + CAST(nr_posicaox AS varchar(10)), 2)
                                where caixa.fg_status < 4 AND caixa.dt_sorter IS NOT NULL AND caixa.dt_estufamento IS NULL 
                                    AND caixa.dt_sorter > (select max(operadorhistorico.dt_evento) from operadorhistorico where operadorhistorico.cd_evento = 2 and operadorhistorico.dt_evento is not null and operadorhistorico.id_equipamento = equipamento.id_equipamento)
                                group by CAST(areaarmazenagem.id_endereco AS varchar(10)) + RIGHT('00' + CAST(nr_posicaox AS varchar(10)), 2)
                                order by CAST(areaarmazenagem.id_endereco AS varchar(10)) + RIGHT('00' + CAST(nr_posicaox AS varchar(10)), 2) desc";


                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var qtdCaixasPendentes = await conexao.QueryAsync<Tuple<string, int>>(query);
                    return qtdCaixasPendentes.ToDictionary(x => x.Item1, x => x.Item2);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task ExecutarLiderVirtual(string identificadorCaracol, string cracha)
        {
            try
            {
                var equipamento = await GetEquipamentoByIdentificadorCaracol(identificadorCaracol);

                if (equipamento == null)
                    throw new Exception("Caracol não encontrado.");

                var agora = DateTime.Now;

                var caracolRefugo = await ParametroBLL.GetParamentro("Identificador do Caracol de Refugo");

                // Busca equipamentos que atendam aos seguintes critérios:
                // nao possui operador
                // não é caracol de refugo
                // não possui lider virutal pendente
                var equipamentosInfo = (await GetCaracoisInfoRelativeTo(identificadorCaracol))
                    .Where(x => x.IdOperador == null &&
                                x.NmIdentificador != caracolRefugo &&
                                ((x.LiderVirtual != null &&
                                    (x.LiderVirtual.IdOperador == null ||
                                        x.LiderVirtual.DtLogoff != null ||
                                        agora > x.LiderVirtual.DtLoginLimite)
                                ) ||
                                x.LiderVirtual == null)
                    );

                EquipamentoModel? proximo = await CaracolCheioAteLimite(equipamentosInfo);
                Console.Write("Caracol chheio", proximo);

                if (proximo == null)
                {
                    proximo = await CaracolComMaisCaixasAteLimite(equipamentosInfo);
                    Console.Write("Caracol com mais caixas", proximo);
                }

                var urgencia = await ParametroBLL.GetParamentro("TEMPO DE ESPERA LIDER VIRTUAL");
                var liderVirtual = new LiderVirtualSIAGModel();

                liderVirtual.DtLoginLimite = (proximo != null) ? DateTime.Now.AddSeconds(int.Parse(urgencia)) : null;
                liderVirtual.IdOperador = cracha;
                liderVirtual.IdEquipamentoOrigem = equipamento.IdEquipamento;
                liderVirtual.IdEquipamentoDestino = proximo?.IdEquipamento;

                await LiderVirtualBLL.CreateLiderVirtual(liderVirtual);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static async Task<EquipamentoModel?> CaracolComMaisCaixasAteLimite(IEnumerable<EquipamentoModel> equipamentosInfo)
        {
            // define limite nivel mais caixas
            int limiteNivelMaisCaixas = 7;
            var limiteSalvo = await ParametroBLL.GetParamentro("Regra Lider Virtual caracol com mais caixas");

            if (int.TryParse(limiteSalvo, out limiteNivelMaisCaixas))
                limiteNivelMaisCaixas = (limiteNivelMaisCaixas < 1) ? 1 : limiteNivelMaisCaixas;

            return equipamentosInfo.Where(x =>
                                    x.Nivel <= limiteNivelMaisCaixas &&
                                    x.CaixasPendentes > 0
                                 )
                                .OrderByDescending(x => x.CaixasPendentes)
                                .FirstOrDefault();
        }

        private static async Task<EquipamentoModel?> CaracolCheioAteLimite(IEnumerable<EquipamentoModel> equipamentosInfo)
        {
            // define limite nivel cheio
            int limiteNivelCheios = 7;
            var limiteSalvo = await ParametroBLL.GetParamentro("Regra Lider Virtual caracol cheio");

            if (int.TryParse(limiteSalvo, out limiteNivelCheios))
                limiteNivelCheios = (limiteNivelCheios < 1) ? 1 : limiteNivelCheios;

            return equipamentosInfo.Where(x => 
                                    x.Nivel <= limiteNivelCheios && 
                                    x.Cheio
                                 )
                                .FirstOrDefault();
        }

        public static async Task<Object> GetProximoLiderVirtual(string identificadorCaracol)
        {
            try
            {
                var equipamento = await GetEquipamentoByIdentificadorCaracol(identificadorCaracol);

                if (equipamento == null)
                    throw new Exception("Caracol não encontrado.");

                var agora = DateTime.Now;


                var caracolRefugo = await ParametroBLL.GetParamentro("Identificador do Caracol de Refugo");

                var equipamentosInfo = (await GetCaracoisInfoRelativeTo(identificadorCaracol))
                    .Where(x => x.IdOperador == null && x.NmIdentificador != caracolRefugo
                        && ((x.LiderVirtual != null
                            && (x.LiderVirtual.IdOperador == null
                                || x.LiderVirtual.DtLogoff != null
                                || agora > x.LiderVirtual.DtLoginLimite
                            ))
                            || x.LiderVirtual == null
                        )
                    );

                var proximo = equipamentosInfo
                    .Where(x => x.Cheio)
                    .FirstOrDefault();

                int limiteNivelCheios = 7;
                var limiteCaracolCheio = await ParametroBLL.GetParamentro("Regra Lider Virtual caracol cheio");

                if (int.TryParse(limiteCaracolCheio, out limiteNivelCheios))
                    limiteNivelCheios = (limiteNivelCheios < 1) ? 1 : limiteNivelCheios;

                if (proximo != null && proximo.Nivel > limiteNivelCheios)
                    proximo = null;

                if (proximo == null)
                    proximo = equipamentosInfo
                    .Where(x => x.CaixasPendentes > 0)
                    .FirstOrDefault();

                return new
                {
                    proximo,
                    equipamentosInfo
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<bool> RotinaLuzVermelha(string identificadorCaracol, int posicaoY)
        {
            try
            {
                var area = await SiagApi.GetAreaArmazenagemByPosicao(identificadorCaracol, posicaoY);

                if (area == null)
                    throw new Exception("Área de armazenagem não encontrada.");

                var pallet = await PalletBLL.GetPalletByIdAreaArmazenagem(area.IdAreaArmazenagem);

                if (pallet == null)
                    throw new Exception("Pallet não encontrado.");

                var queryAtividade = @"SELECT id_atividade as idAtividade
                    FROM atividade WITH(NOLOCK)
                    WHERE nm_atividade = 'Pallet cheio no sorter'";

                var queryChamada = @"SELECT top 1 id_chamada
                                    FROM chamada WITH(NOLOCK)
                                    WHERE id_palletorigem = @idPallet
                                        AND fg_status < 4
                                        AND id_areaarmazenagemorigem = @idArea
                                        AND id_atividade = @idAtividade";

                var queryPallet = "UPDATE pallet " +
                        "SET fg_status = 3 " +
                        "WHERE id_pallet = @idPallet";

                var queryAreaArmazenagem = "UPDATE areaarmazenagem " +
                        "SET fg_status = 2 " +
                        "WHERE id_areaarmazenagem = @idAreaArmazenagem";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var idAtividade = await conexao.QueryFirstOrDefaultAsync<int?>(queryAtividade);

                    if (idAtividade == null)
                        throw new Exception("Atividade 'Pallet cheio no sorter' não encontrada.");


                    var idChamada = await conexao.QueryFirstOrDefaultAsync<Guid?>(queryChamada, new
                    {
                        idPallet = pallet.IdPallet,
                        idArea = area.IdAreaArmazenagem,
                        idAtividade = idAtividade
                    });

                    if (idChamada != null)
                        return true;

                    await PalletBLL.GerarAtividadePalletCheio(pallet.IdPallet ?? "", area.IdAreaArmazenagem ?? "");
                    var qtdLinhasArea = await conexao.ExecuteAsync(queryAreaArmazenagem, new { idAreaArmazenagem = area.IdAreaArmazenagem });
                    var qtdLinhasPallet = await conexao.ExecuteAsync(queryPallet, new { idPallet = pallet.IdPallet });

                    await PalletBLL.VincularAgrupadorAreaReservada(area, identificadorCaracol);

                    return qtdLinhasArea > 0 && qtdLinhasPallet > 0;
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<bool> AtualizaStatusLeitorAsync(StatusLeitorModel statusLeitorModel)
        {
            var caracol = GetCaracol(statusLeitorModel.Equipamento);

            if (caracol == null)
                throw new Exception("Equipamento não encontrado");

            var queryUltimoStatus = @"SELECT *
                    FROM status_leitor WITH(NOLOCK)
                    WHERE equipamento = @equipamento AND leitor = @leitor";

            using (var conexao = new SqlConnection(Global.Conexao))
            {
                var quantidadeUltimoStatus = (await conexao.QueryAsync<StatusLeitorModel>(queryUltimoStatus, new
                {
                    equipamento = statusLeitorModel.Equipamento,
                    leitor = statusLeitorModel.Leitor
                })).Count();

                if (quantidadeUltimoStatus <= 0)
                {
                    var querySalvaStatus = @"INSERT INTO status_leitor (equipamento, leitor, configurado, conectado, executando, dt_status) " +
                                     "VALUES (@equipamento, @leitor, @configurado, @conectado, @executando, @dt_status)";

                    var quantidadeSalva = await conexao.ExecuteAsync(querySalvaStatus, new
                    {
                        equipamento = statusLeitorModel.Equipamento,
                        leitor = statusLeitorModel.Leitor,
                        configurado = statusLeitorModel.Configurado ? 1 : 0,
                        conectado = statusLeitorModel.Conectado ? 1 : 0,
                        executando = statusLeitorModel.Executando ? 1 : 0,
                        dt_status = DateTime.Now,
                    });

                    if (quantidadeSalva > 0)
                    {
                        if (!statusLeitorModel.Configurado || !statusLeitorModel.Conectado || !statusLeitorModel.Executando)
                        {
                            string conteudo = $"configurado: {statusLeitorModel.Configurado}, conectado: {statusLeitorModel.Conectado}, executando: {statusLeitorModel.Executando}";
                            await EnviaAlertaLeitor($"{statusLeitorModel.Equipamento}, Leitor {statusLeitorModel.Leitor}", conteudo);
                        }

                        return true;
                    }
                    else throw new Exception($"{statusLeitorModel.Equipamento}: Erro ao salvar status do leitor {statusLeitorModel.Leitor}");
                }
                else
                {
                    var queryAtualizaStatus = @"UPDATE status_leitor
                            SET conectado = @conectado, configurado = @configurado, executando = @executando, dt_status = @dt_status
                            WHERE equipamento = @equipamento AND leitor = @leitor";

                    var quantidadeAtualizada = await conexao.ExecuteAsync(queryAtualizaStatus, new
                    {
                        equipamento = statusLeitorModel.Equipamento,
                        leitor = statusLeitorModel.Leitor,
                        configurado = statusLeitorModel.Configurado ? 1 : 0,
                        conectado = statusLeitorModel.Conectado ? 1 : 0,
                        executando = statusLeitorModel.Executando ? 1 : 0,
                        dt_status = DateTime.Now,
                    });

                    if (quantidadeAtualizada > 0)
                    {
                        if (!statusLeitorModel.Configurado || !statusLeitorModel.Conectado || !statusLeitorModel.Executando)
                        {
                            string conteudo = $"configurado: {statusLeitorModel.Configurado}, conectado: {statusLeitorModel.Conectado}, executando: {statusLeitorModel.Executando}";
                            await EnviaAlertaLeitor($"{statusLeitorModel.Equipamento}, Leitor {statusLeitorModel.Leitor}", conteudo);
                        }

                        return true;
                    }
                    else throw new Exception($"{statusLeitorModel.Equipamento}: Erro ao atualizar status do leitor {statusLeitorModel.Leitor}");
                }
            }
        }

        public static string AdicionarCondicao(string atual, string nova)
        {
            if (string.IsNullOrWhiteSpace(atual)) atual += " WHERE ";
            else atual += " AND ";

            return atual += nova;
        }

        public static async Task<List<StatusLeitorModel>> GetStatusLeitorAsync(FiltroStatusLeitorDTO filtroStatusLeitor)
        {
            var condicoes = "";

            if (!string.IsNullOrWhiteSpace(filtroStatusLeitor.Equipamento))
                condicoes = AdicionarCondicao(condicoes, "equipamento = @equipamento");

            if (!string.IsNullOrWhiteSpace(filtroStatusLeitor.Leitor))
                condicoes = AdicionarCondicao(condicoes, "leitor = @leitor");

            if (filtroStatusLeitor.Configurado != null)
                condicoes = AdicionarCondicao(condicoes, "configurado = @configurado");

            if (filtroStatusLeitor.Conectado != null)
                condicoes = AdicionarCondicao(condicoes, "conectado = @conectado");

            if (filtroStatusLeitor.Executando != null)
                condicoes = AdicionarCondicao(condicoes, "executando = @executando");

            if (filtroStatusLeitor.InicioPeriodo != null)
                condicoes = AdicionarCondicao(condicoes, "dt_status >= @inicioPerido");

            if (filtroStatusLeitor.FimPeriodo != null)
                condicoes = AdicionarCondicao(condicoes, "dt_status <= @fimPerido");

            var queryListaStatusLeitor = @"SELECT *
                    FROM status_leitor WITH(NOLOCK)";

            if (!string.IsNullOrWhiteSpace(condicoes))
                queryListaStatusLeitor += condicoes;

            queryListaStatusLeitor += " ORDER BY equipamento ASC, leitor ASC, dt_status DESC";

            using (var conexao = new SqlConnection(Global.Conexao))
            {
                var listaStatusLeitor = (await conexao.QueryAsync<StatusLeitorModel>(queryListaStatusLeitor, new
                {
                    equipamento = filtroStatusLeitor.Equipamento,
                    leitor = filtroStatusLeitor.Leitor,
                    configurado = filtroStatusLeitor.Configurado,
                    conectado = filtroStatusLeitor.Conectado,
                    executando = filtroStatusLeitor.Executando,
                    inicioPerido = filtroStatusLeitor.InicioPeriodo,
                    fimPerido = filtroStatusLeitor.FimPeriodo,
                })).ToList();

                return listaStatusLeitor;
            }
        }

        public static string VerificaParametroEmail(List<ParametroModel> parametros, string parametro)
        {
            string valor = "";

            var dadosParametro = parametros.Where(x => x.nm_parametro == "Nome Envio Status Leitores Cacacol").FirstOrDefault();

            if (dadosParametro != null && !string.IsNullOrWhiteSpace(dadosParametro.nm_valor))
                valor = dadosParametro.nm_valor;

            return valor;
        }

        public static async Task<bool> EnviaAlertaLeitor(string assunto, string conteudo)
        {
            var queryListaStatusLeitor = "select * from parametro";

            using (var conexao = new SqlConnection(Global.Conexao))
            {
                var parametros = (await conexao.QueryAsync<ParametroModel>(queryListaStatusLeitor)).ToList();

                var destinatarios = VerificaParametroEmail(parametros, "Emails Acompanhamento Status Leitores Cacacol");
                string[] listaDestinatarios = destinatarios.Split(';');

                foreach (var destinatario in listaDestinatarios)
                {
                    if (string.IsNullOrWhiteSpace(destinatario)) continue;

                    var dadosEmail = new EmailDTO
                    {
                        EmailUsuario = VerificaParametroEmail(parametros, "Email Envio Status Leitores Cacacol"),
                        EmailNome = VerificaParametroEmail(parametros, "Nome Envio Status Leitores Cacacol"),
                        EmailSenha = VerificaParametroEmail(parametros, "Senha Envio Status Leitores Cacacol"),

                        EmailServidor = VerificaParametroEmail(parametros, "Servidor Status Leitores Cacacol"),
                        EmailPorta = int.Parse(VerificaParametroEmail(parametros, "Porta Status Leitores Cacacol")),
                        EmailSSL = VerificaParametroEmail(parametros, "Ativar SSl Status Leitores Cacacol") == "1" ? true : false,

                        EmailDestinatario = destinatario,
                        EmailAssunto = assunto,
                        EmailConteudo = conteudo,
                    };

                    var resposta = await EmailUtil.EnviarEmail(dadosEmail);
                }
            }

            return true;
        }

        public static async Task<EquipamentoSIAGModel?> GetEquipamentoByCaixaPendente(string cdCaixaPendente)
        {
            try
            {
                var query = "SELECT id_equipamento AS IdEquipamento, " +
                                   "nm_equipamento AS NmEquipamento, " +
                                   "id_equipamentomodelo AS IdEquipamentoModelo, " +
                                   "nm_identificador AS NmIdentificador, " +
                                   "cd_ultimaleitura AS CdUltimaLeitura, " +
                                   "dt_ultimaleitura AS DtUltimaLeitura, " +
                                   "id_operador AS IdOperador " +
                            "FROM equipamento WITH(NOLOCK) " +
                            "WHERE cd_leitura_pendente = @cdCaixaPendente";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var equipamento = await conexao.QueryFirstOrDefaultAsync<EquipamentoSIAGModel>(query, new { cdCaixaPendente = cdCaixaPendente });

                    return equipamento;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}