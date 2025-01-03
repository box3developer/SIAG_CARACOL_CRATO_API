using Dapper;
using dotnet_api.Integration;
using dotnet_api.Models;
using dotnet_api.ModelsSIAG;
using grendene_caracois_api_csharp;
using Microsoft.Data.SqlClient;

namespace dotnet_api.BLLs
{
    public class PalletBLL
    {
        public static async Task<PalletSIAGModel?> GetPallet(string idPallet)
        {
            try
            {
                var query = "SELECT id_pallet AS IdPallet, id_areaarmazenagem AS IdAreaArmazenagem, id_agrupador as IdAgrupador, fg_status AS FgStatus " +
                             "FROM pallet WITH(NOLOCK)" +
                             "WHERE id_pallet = @idPallet";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var pallet = await conexao.QueryFirstOrDefaultAsync<PalletSIAGModel>(query, new { idPallet = idPallet });

                    return pallet;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<PalletSIAGModel?> GetPalletByIdAreaArmazenagem(string? idAreaArmazenagem)
        {
            try
            {
                var query = "SELECT id_pallet AS IdPallet, id_areaarmazenagem AS IdAreaArmazenagem, id_agrupador as IdAgrupador, fg_status AS FgStatus " +
                             "FROM pallet WITH(NOLOCK)" +
                             "WHERE id_areaarmazenagem = @idAreaArmazenagem";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var pallet = await conexao.QueryFirstOrDefaultAsync<PalletSIAGModel>(query, new { idAreaArmazenagem = idAreaArmazenagem });

                    return pallet;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<int> ContarCaixasVinculadas(string idPallet)
        {
            try
            {
                var query = "SELECT COUNT(*) " +
                            "FROM caixa WITH(NOLOCK) " +
                            "WHERE id_pallet = @idPallet";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var qtd = await conexao.QueryFirstOrDefaultAsync<int>(query, new { idPallet = idPallet });
                    return qtd;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<int> ContarCaixasPendentes(string idAgrupador)
        {
            try
            {
                var query = "SELECT COUNT(*) " +
                            "FROM caixa WITH(NOLOCK) " +
                            "WHERE id_agrupador = @idAgrupador and (caixa.fg_status < 4 or caixa.fg_status = 8)";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var qtd = await conexao.QueryFirstOrDefaultAsync<int>(query, new { idAgrupador = idAgrupador });
                    return qtd;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<List<AreaArmazenagemSIAGModel>> GetAreasReservadas(string nmEquipamento, System.Guid idAgrupador, SqlConnection conexao)
        {
            var queryAreasReservadas = @"SELECT id_agrupador as IdAgrupador, id_agrupador_reservado as IdAgrupadorReservado, nr_posicaoy as PosicaoY FROM areaarmazenagem 
                WHERE CAST(id_endereco AS varchar(10)) + RIGHT('00' + CAST(nr_posicaox AS varchar(10)), 2) = @identificadorCaracol AND
                id_agrupador_reservado = @idAgrupador";

            var areasReservadas = (await conexao.QueryAsync<AreaArmazenagemSIAGModel>(queryAreasReservadas, new
            {
                idAgrupador = idAgrupador,
                identificadorCaracol = nmEquipamento
            })).ToList();

            return areasReservadas;
        }

        public static async Task<int> ReservarAreaArmazenagem(AreaArmazenagemSIAGModel areaArmazenagem, SqlConnection conexao, Guid? id_requisicao)
        {
            await LiberaReservasAreaArmazenagem(areaArmazenagem.IdAgrupador, conexao, id_requisicao);

            var qtdLinhasAreaArmazenagemReservada = 0;

            var queryAreaArmazenagemReservada = "UPDATE areaarmazenagem " +
                    "SET id_agrupador_reservado = @idAgrupador, fg_status = 2 " +
                    "WHERE id_areaarmazenagem = @idAreaArmazenagem";

            Console.WriteLine("queryAreaArmazenagemReservada => " + queryAreaArmazenagemReservada);
            Console.WriteLine("idAreaArmazenageOrigem => " + areaArmazenagem.IdAreaArmazenagem);

            qtdLinhasAreaArmazenagemReservada = await conexao.ExecuteAsync(queryAreaArmazenagemReservada, new
            {
                idAreaArmazenagem = areaArmazenagem.IdAreaArmazenagem,
                idAgrupador = areaArmazenagem.IdAgrupador
            });

            return qtdLinhasAreaArmazenagemReservada;
        }

        public static async Task<int> LiberaReservasAreaArmazenagem(System.Guid idAgrupador, SqlConnection? conexao, Guid? id_requisicao)
        {
            await LogBLL.GravarLog(
                id_requisicao,
                "",
                "",
                $"Libera áreas reservadas para agrupador {idAgrupador}",
                "LiberaReservasAreaArmazenagem",
                "",
                "info"
            );

            bool conexaoNova = false;
            if (conexao == null)
            {
                conexao = new SqlConnection(Global.Conexao);
                conexaoNova = true;
            }

            var qtdLinhasAreaArmazenagemReservada = 0;

            var queryAreaArmazenagemReservada = "UPDATE areaarmazenagem " +
                    "SET id_agrupador_reservado = null, fg_status = 1 " +
                    "WHERE id_agrupador_reservado = @idAgrupador AND (id_agrupador is null OR id_agrupador = @idAgrupadorAtual) AND fg_status in (2,3)";

            Console.WriteLine("queryAreaArmazenagemReservada => " + queryAreaArmazenagemReservada);
            Console.WriteLine("idAgrupador => " + idAgrupador);

            qtdLinhasAreaArmazenagemReservada = await conexao.ExecuteAsync(queryAreaArmazenagemReservada, new
            {
                idAgrupador = idAgrupador,
                idAgrupadorAtual = idAgrupador
            });

            if (conexaoNova)
                conexao.Close();

            return qtdLinhasAreaArmazenagemReservada;
        }

        public static async Task<int> GetNivelAgrupador(Guid idAgrupador)
        {
            var queryQuantidadeCaixas = @"select quantidade_caixas = count(*) from caixa
            where caixa.id_agrupador = @idAgrupador and caixa.fg_status not in (4, 5, 6, 7)";

            var queryNivel = @"select nivel from niveisagrupadores
            where @quantidadeCaixas >= inicio and(termino is null or @quantidadeCaixas <= termino)";

            using (var conexao = new SqlConnection(Global.Conexao))
            {
                var quantidadeCaixas = await conexao.QueryFirstOrDefaultAsync<int>(queryQuantidadeCaixas, new { idAgrupador = idAgrupador });

                var nivel = await conexao.QueryFirstOrDefaultAsync<int>(queryNivel, new { quantidadeCaixas = quantidadeCaixas });

                return nivel;
            }
        }


        public static async Task<bool> VincularAgrupadorAreaReservada(AreaArmazenagemSIAGModel areaAtual, string identificadorCaracol)
        {
            var palletNovo = new PalletSIAGModel();

            var queryReservada = @"SELECT top 1 id_pallet AS IdPallet,
                                pallet.id_areaarmazenagem AS IdAreaArmazenagem,
                                pallet.id_agrupador AS IdAgrupador,
                                pallet.fg_status AS FgStatus
                            FROM areaarmazenagem WITH(NOLOCK)
                            INNER JOIN pallet WITH(NOLOCK)
                                ON areaarmazenagem.id_areaarmazenagem = pallet.id_areaarmazenagem
                            WHERE CAST(id_endereco AS varchar(10)) + RIGHT('00' + CAST(nr_posicaox AS varchar(10)), 2) = @identificadorCaracol
                                AND areaarmazenagem.id_agrupador IS NULL
                                AND areaarmazenagem.id_agrupador_reservado = @idAgrupadorReservado
                                AND pallet.fg_status = 1
                                AND (areaarmazenagem.fg_status = 2 or areaarmazenagem.fg_status = 1)
                            ORDER BY nr_posicaoy";

            using (var conexao = new SqlConnection(Global.Conexao))
            {
                palletNovo = await conexao.QueryFirstOrDefaultAsync<PalletSIAGModel?>(queryReservada,
                    new
                    {
                        identificadorCaracol = identificadorCaracol,
                        idAgrupadorReservado = areaAtual.IdAgrupador
                    });
            }

            if (palletNovo == null) return false;

            var queryAreaArmazenagemOrigem = "UPDATE areaarmazenagem " +
                    "SET id_agrupador = null, fg_status = 1 " +
                    "WHERE id_areaarmazenagem = @idAreaArmazenageOrigem";

            var queryAgrupadorDestino = "UPDATE agrupadorativo " +
                    "SET id_areaarmazenagem = null, fg_status = 2 " +
                    "WHERE id_agrupador = @idAgrupadorDestino";

            var queryAreaArmazenagemDestino = "UPDATE areaarmazenagem " +
                    "SET id_agrupador = @idAgrupadorOrigem, fg_status = 2 " +
                    "WHERE id_areaarmazenagem = @idAreaArmazenagemDestino";

            var queryAgrupadorOrigem = "UPDATE agrupadorativo " +
                    "SET id_areaarmazenagem = @idAreaArmazenagemDestino " +
                    "WHERE id_agrupador = @idAgrupadorOrigem";

            using (var conexao = new SqlConnection(Global.Conexao))
            {
                Console.WriteLine("------------------------------------------------------------");
                Console.WriteLine("queryAreaArmazenagemOrigem => " + queryAreaArmazenagemOrigem);
                Console.WriteLine("idAreaArmazenageOrigem => " + areaAtual.IdAreaArmazenagem);

                var qtdLinhasAreaOrigem = await conexao.ExecuteAsync(queryAreaArmazenagemOrigem, new { idAreaArmazenageOrigem = areaAtual.IdAreaArmazenagem });

                if (palletNovo.IdAgrupador == Guid.Empty)
                    throw new Exception("Pallet Novo sem agrupador.");

                Console.WriteLine("------------------------------------------------------------");
                Console.WriteLine("queryAgrupadorDestino => " + queryAgrupadorDestino);
                Console.WriteLine("idAgrupadorDestino => " + palletNovo.IdAgrupador);

                var qtdLinhasAgrupadorDestino = await conexao.ExecuteAsync(queryAgrupadorDestino, new { idAgrupadorDestino = palletNovo.IdAgrupador });

                Console.WriteLine("------------------------------------------------------------");
                Console.WriteLine("queryAreaArmazenagemDestino => " + queryAreaArmazenagemDestino);
                Console.WriteLine("idAreaArmazenagemDestino => " + palletNovo.IdAreaArmazenagem);
                Console.WriteLine("idAgrupadorOrigem => " + areaAtual.IdAgrupador);

                var qtdLinhasAreaArmazenagemDestino = await conexao.ExecuteAsync(queryAreaArmazenagemDestino, new
                {
                    idAreaArmazenagemDestino = palletNovo.IdAreaArmazenagem,
                    idAgrupadorOrigem = areaAtual.IdAgrupador
                });

                Console.WriteLine("------------------------------------------------------------");
                Console.WriteLine("queryAgrupadorOrigem => " + queryAgrupadorOrigem);
                Console.WriteLine("idAreaArmazenagemDestino => " + palletNovo.IdAreaArmazenagem);
                Console.WriteLine("idAgrupadorOrigem => " + areaAtual.IdAgrupador);

                var qtdLinhasAgrupadorOrigem = await conexao.ExecuteAsync(queryAgrupadorOrigem, new
                {
                    idAreaArmazenagemDestino = palletNovo.IdAreaArmazenagem,
                    idAgrupadorOrigem = areaAtual.IdAgrupador
                });

                //if (qtdLinhasAreaArmazenagemDestino == 0 || qtdLinhasAreaOrigem == 0 || qtdLinhasAgrupadorOrigem == 0 || qtdLinhasAgrupadorDestino == 0)
                //    throw new Exception("Erro ao voltar pallet para área reservada");

                return true;
            }
        }

        public static async Task<bool> VincularNovoPalletPorPrioridade(string? identificadorCaracol, AreaArmazenagemSIAGModel areaAtual, Guid? id_requisicao)
        {
            PalletSIAGModel? palletNovo = null;
            var livreSemPrioridade = false;
            var nivelAgrupador = await GetNivelAgrupador(areaAtual.IdAgrupador);

            var queryReservada = @"SELECT top 1 id_pallet AS IdPallet,
                                pallet.id_areaarmazenagem AS IdAreaArmazenagem,
                                pallet.id_agrupador AS IdAgrupador,
                                pallet.fg_status AS FgStatus
                            FROM areaarmazenagem WITH(NOLOCK)
                            INNER JOIN pallet WITH(NOLOCK)
                                ON areaarmazenagem.id_areaarmazenagem = pallet.id_areaarmazenagem
                            WHERE CAST(id_endereco AS varchar(10)) + RIGHT('00' + CAST(nr_posicaox AS varchar(10)), 2) = @identificadorCaracol
                                AND areaarmazenagem.id_agrupador IS NULL
                                AND areaarmazenagem.id_agrupador_reservado = @idAgrupadorReservado
                                AND pallet.fg_status = 1
                                AND (areaarmazenagem.fg_status = 2 or areaarmazenagem.fg_status = 1)
                            ORDER BY nr_posicaoy";

            using (var conexao = new SqlConnection(Global.Conexao))
            {
                palletNovo = await conexao.QueryFirstOrDefaultAsync<PalletSIAGModel?>(queryReservada,
                    new
                    {
                        identificadorCaracol = identificadorCaracol,
                        idAgrupadorReservado = areaAtual.IdAgrupador
                    });
            }

            if (palletNovo == null)
            {
                var queryLivreSemPrioridade = @"SELECT top 1 id_pallet AS IdPallet,
                                pallet.id_areaarmazenagem AS IdAreaArmazenagem,
                                pallet.id_agrupador AS IdAgrupador,
                                pallet.fg_status AS FgStatus
                            FROM areaarmazenagem WITH(NOLOCK)
                            INNER JOIN pallet WITH(NOLOCK)
                                ON areaarmazenagem.id_areaarmazenagem = pallet.id_areaarmazenagem
	                        LEFT JOIN prioridadesareasarmazenagem as p on p.id_areaarmazenagem = areaarmazenagem.id_areaarmazenagem
                            WHERE CAST(id_endereco AS varchar(10)) + RIGHT('00' + CAST(nr_posicaox AS varchar(10)), 2) = @identificadorCaracol
	                            AND p.nr_prioridade1 < 1 
                                AND p.nr_prioridade2 < 1
                                AND areaarmazenagem.id_agrupador IS NULL
                                AND pallet.fg_status = 1
                                AND areaarmazenagem.fg_status = 1
                            ORDER BY nr_posicaoy";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    palletNovo = await conexao.QueryFirstOrDefaultAsync<PalletSIAGModel?>(queryLivreSemPrioridade, new { identificadorCaracol = identificadorCaracol });
                }

                if (palletNovo == null)
                {
                    var queryLivreComPrioridade = @"SELECT top 1 id_pallet AS IdPallet,
                                                pallet.id_areaarmazenagem AS IdAreaArmazenagem,
                                                pallet.id_agrupador AS IdAgrupador,
                                                pallet.fg_status AS FgStatus
                                            FROM areaarmazenagem WITH(NOLOCK)
                                            INNER JOIN pallet WITH(NOLOCK)
                                                ON areaarmazenagem.id_areaarmazenagem = pallet.id_areaarmazenagem
	                                        LEFT JOIN prioridadesareasarmazenagem as p on p.id_areaarmazenagem = areaarmazenagem.id_areaarmazenagem
                                            WHERE CAST(id_endereco AS varchar(10)) + RIGHT('00' + CAST(nr_posicaox AS varchar(10)), 2) = @identificadorCaracol
	                                            AND (p.nr_prioridade1 > 0 OR p.nr_prioridade2 > 0)
                                                AND areaarmazenagem.id_agrupador IS NULL
                                                AND pallet.fg_status = 1
                                                AND areaarmazenagem.fg_status = 1
                                            ORDER BY nr_posicaoy";

                    using (var conexao = new SqlConnection(Global.Conexao))
                    {
                        palletNovo = await conexao.QueryFirstOrDefaultAsync<PalletSIAGModel?>(queryLivreComPrioridade, new
                        {
                            identificadorCaracol = identificadorCaracol,
                            nivelAgrupadorPrioridade1 = nivelAgrupador
                        });
                    }

                    await LogBLL.GravarLog(
                        id_requisicao,
                        identificadorCaracol,
                        "",
                        $"Troca de pallet para área livre com prioridade",
                        "VincularNovoPalletPorPrioridade",
                        "",
                        "info"
                    );

                    Console.WriteLine("Vinculou em área com prioridade 1");
                }
                else
                {
                    await LogBLL.GravarLog(
                        id_requisicao,
                        identificadorCaracol,
                        "",
                        $"Troca de pallet para área livre sem prioridade",
                        "VincularNovoPalletPorPrioridade",
                        "",
                        "info"
                    );

                    livreSemPrioridade = true;
                }
            }
            else
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    identificadorCaracol,
                    "",
                    $"Troca de pallet para área reservada",
                    "VincularNovoPalletPorPrioridade",
                    "",
                    "info"
                );

                Console.WriteLine("Vinculou em área reservada");
            }

            if (palletNovo == null) return false;

            var queryAreaArmazenagemOrigem = "UPDATE areaarmazenagem " +
                    "SET id_agrupador = null, fg_status = 1 " +
                    "WHERE id_areaarmazenagem = @idAreaArmazenageOrigem";

            var queryAgrupadorDestino = "UPDATE agrupadorativo " +
                    "SET id_areaarmazenagem = null, fg_status = 2 " +
                    "WHERE id_agrupador = @idAgrupadorDestino";

            var queryAreaArmazenagemDestino = "UPDATE areaarmazenagem " +
                    "SET id_agrupador = @idAgrupadorOrigem, fg_status = 2 " +
                    "WHERE id_areaarmazenagem = @idAreaArmazenagemDestino";

            var queryAgrupadorOrigem = "UPDATE agrupadorativo " +
                    "SET id_areaarmazenagem = @idAreaArmazenagemDestino " +
                    "WHERE id_agrupador = @idAgrupadorOrigem";

            using (var conexao = new SqlConnection(Global.Conexao))
            {
                Console.WriteLine("------------------------------------------------------------");
                Console.WriteLine("queryAreaArmazenagemOrigem => " + queryAreaArmazenagemOrigem);
                Console.WriteLine("idAreaArmazenageOrigem => " + areaAtual.IdAreaArmazenagem);

                var qtdLinhasAreaOrigem = await conexao.ExecuteAsync(queryAreaArmazenagemOrigem, new { idAreaArmazenageOrigem = areaAtual.IdAreaArmazenagem });

                var qtdLinhasAgrupadorDestino = 1;
                if (!livreSemPrioridade)
                {
                    if (palletNovo.IdAgrupador == Guid.Empty)
                        throw new Exception("Pallet Novo sem agrupador.");

                    Console.WriteLine("------------------------------------------------------------");
                    Console.WriteLine("queryAgrupadorDestino => " + queryAgrupadorDestino);
                    Console.WriteLine("idAgrupadorDestino => " + palletNovo.IdAgrupador);

                    qtdLinhasAgrupadorDestino = await conexao.ExecuteAsync(queryAgrupadorDestino, new { idAgrupadorDestino = palletNovo.IdAgrupador });
                }

                Console.WriteLine("------------------------------------------------------------");
                Console.WriteLine("queryAreaArmazenagemDestino => " + queryAreaArmazenagemDestino);
                Console.WriteLine("idAreaArmazenagemDestino => " + palletNovo.IdAreaArmazenagem);
                Console.WriteLine("idAgrupadorOrigem => " + areaAtual.IdAgrupador);

                var qtdLinhasAreaArmazenagemDestino = await conexao.ExecuteAsync(queryAreaArmazenagemDestino, new
                {
                    idAreaArmazenagemDestino = palletNovo.IdAreaArmazenagem,
                    idAgrupadorOrigem = areaAtual.IdAgrupador
                });

                Console.WriteLine("------------------------------------------------------------");
                Console.WriteLine("queryAgrupadorOrigem => " + queryAgrupadorOrigem);
                Console.WriteLine("idAreaArmazenagemDestino => " + palletNovo.IdAreaArmazenagem);
                Console.WriteLine("idAgrupadorOrigem => " + areaAtual.IdAgrupador);

                var qtdLinhasAgrupadorOrigem = await conexao.ExecuteAsync(queryAgrupadorOrigem, new
                {
                    idAreaArmazenagemDestino = palletNovo.IdAreaArmazenagem,
                    idAgrupadorOrigem = areaAtual.IdAgrupador
                });

                //if (qtdLinhasAreaArmazenagemDestino == 0 || qtdLinhasAreaOrigem == 0 || qtdLinhasAgrupadorOrigem == 0 || qtdLinhasAgrupadorDestino == 0)
                //    throw new Exception("Troca de pallet incompleta");

                await LogBLL.GravarLog(
                    id_requisicao,
                    identificadorCaracol,
                    "",
                    $"Troca do agrupador {palletNovo.IdAgrupador} da área {areaAtual} para a área {palletNovo.IdAreaArmazenagem} ",
                    "VincularNovoPalletPorPrioridade",
                    "",
                    "info"
                );

                return true;
            }
        }

        public static async Task<bool> VincularNovoPalletDisponivel(string? identificadorCaracol, AreaArmazenagemSIAGModel areaAtual)
        {
            try
            {
                var query = @"SELECT top 1 id_pallet AS IdPallet,
                                pallet.id_areaarmazenagem AS IdAreaArmazenagem,
                                pallet.id_agrupador AS IdAgrupador,
                                pallet.fg_status AS FgStatus
                            FROM areaarmazenagem WITH(NOLOCK)
                            LEFT JOIN agrupadorativo
                                ON agrupadorativo.id_areaarmazenagem = areaarmazenagem.id_areaarmazenagem AND agrupadorativo.fg_status = 3
                            INNER JOIN pallet WITH(NOLOCK)
                                ON areaarmazenagem.id_areaarmazenagem = pallet.id_areaarmazenagem
                            WHERE CAST(id_endereco AS varchar(10)) + RIGHT('00' + CAST(nr_posicaox AS varchar(10)), 2) = @identificadorCaracol
                                AND areaarmazenagem.id_agrupador IS NULL
                                AND pallet.fg_status = 1
                                AND areaarmazenagem.fg_status = 1
                                AND agrupadorativo.id_agrupador IS NULL
                            ORDER BY nr_posicaoy";

                PalletSIAGModel? palletNovo = null;

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    palletNovo = await conexao.QueryFirstOrDefaultAsync<PalletSIAGModel?>(query, new { identificadorCaracol = identificadorCaracol });
                }

                if (palletNovo == null)
                    return false;


                Console.WriteLine("Vinculou em área livre");

                var queryAreaArmazenagemAntiga = "UPDATE areaarmazenagem " +
                        "SET id_agrupador = null, fg_status = 1 " +
                        "WHERE id_areaarmazenagem = @idAreaArmazenagem";

                var queryAreaArmazenagem = "UPDATE areaarmazenagem " +
                        "SET id_agrupador = @idAgrupador, fg_status = 2 " +
                        "WHERE id_areaarmazenagem = @idAreaArmazenagem";

                var queryAgrupador = "UPDATE agrupadorativo " +
                        "SET id_areaarmazenagem = @idAreaArmazenagem " +
                        "WHERE id_agrupador = @idAgrupador";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    Console.WriteLine("------------------------------------------------------------");
                    Console.WriteLine("queryAreaArmazenagem => " + queryAreaArmazenagem);
                    Console.WriteLine("idAreaArmazenagem => " + palletNovo.IdAreaArmazenagem);
                    Console.WriteLine("idAgrupador => " + areaAtual.IdAgrupador);

                    var qtdLinhasArea = await conexao.ExecuteAsync(queryAreaArmazenagem, new { idAreaArmazenagem = palletNovo.IdAreaArmazenagem, idAgrupador = areaAtual.IdAgrupador });



                    Console.WriteLine("------------------------------------------------------------");
                    Console.WriteLine("queryAreaArmazenagemAntiga => " + queryAreaArmazenagemAntiga);
                    Console.WriteLine("idAreaArmazenagem => " + areaAtual.IdAreaArmazenagem);

                    var qtdLinhasAreaAntiga = await conexao.ExecuteAsync(queryAreaArmazenagemAntiga, new { idAreaArmazenagem = areaAtual.IdAreaArmazenagem });



                    Console.WriteLine("------------------------------------------------------------");
                    Console.WriteLine("queryAgrupador => " + queryAgrupador);
                    Console.WriteLine("idAreaArmazenagem => " + palletNovo.IdAreaArmazenagem);
                    Console.WriteLine("idAgrupador => " + areaAtual.IdAgrupador);

                    var qtdLinhasAgrupador = await conexao.ExecuteAsync(queryAgrupador, new { idAreaArmazenagem = palletNovo.IdAreaArmazenagem, idAgrupador = areaAtual.IdAgrupador });

                    return qtdLinhasArea > 0 && qtdLinhasAreaAntiga > 0 && qtdLinhasAgrupador > 0;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<bool> VincularNovoPalletReservado(string? idAgrupadorCaixa, string? identificadorCaracol, AreaArmazenagemSIAGModel areaAtual, Guid? id_requisicao)
        {
            try
            {
                PalletSIAGModel? palletNovo = null;
                var reservadaSemAgrupador = false;

                var queryReservadaSemAgrupador = @"SELECT top 1 id_pallet AS IdPallet,
                                pallet.id_areaarmazenagem AS IdAreaArmazenagem,
                                pallet.id_agrupador AS IdAgrupador,
                                pallet.fg_status AS FgStatus
                            FROM areaarmazenagem WITH(NOLOCK)
                            INNER JOIN pallet WITH(NOLOCK)
                                ON areaarmazenagem.id_areaarmazenagem = pallet.id_areaarmazenagem
                            WHERE CAST(id_endereco AS varchar(10)) + RIGHT('00' + CAST(nr_posicaox AS varchar(10)), 2) = @identificadorCaracol
                                AND areaarmazenagem.id_agrupador IS NULL
                                AND pallet.fg_status = 1
                                AND areaarmazenagem.fg_status = 2
                            ORDER BY nr_posicaoy";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    palletNovo = await conexao.QueryFirstOrDefaultAsync<PalletSIAGModel?>(queryReservadaSemAgrupador, new { identificadorCaracol = identificadorCaracol });
                }

                if (palletNovo != null)
                {
                    await LogBLL.GravarLog(
                        id_requisicao,
                        identificadorCaracol,
                        "",
                        $"Troca de pallet para área reservada sem agrupador",
                        "VincularNovoPalletReservado",
                        "",
                        "info"
                    );

                    reservadaSemAgrupador = true;
                }
                else
                {
                    var queryReservadaMenosCaixasSemSorter = @"select top 1 pallet.id_pallet AS IdPallet,
                                            pallet.id_areaarmazenagem AS IdAreaArmazenagem,
                                            agrupadorativo.id_agrupador AS IdAgrupador,
                                            pallet.fg_status AS FgStatus
                                            from areaarmazenagem WITH(NOLOCK)
                                            left join agrupadorativo WITH(NOLOCK)
                                                on agrupadorativo.id_agrupador = areaarmazenagem.id_agrupador
                                            left join caixa WITH(NOLOCK)
                                                on agrupadorativo.id_agrupador = caixa.id_agrupador
                                            inner JOIN pallet WITH(NOLOCK)
                                                ON areaarmazenagem.id_areaarmazenagem = pallet.id_areaarmazenagem
                                            where 
											(caixa.fg_status < 4 or caixa.fg_status = 8) AND caixa.dt_sorter IS NULL
											AND CAST(id_endereco AS varchar(10)) + RIGHT('00' + CAST(nr_posicaox AS varchar(10)), 2) = @identificadorCaracol
											AND areaarmazenagem.id_endereco < 6
                                            AND agrupadorativo.id_agrupador IS NOT NULL
                                            AND pallet.fg_status = 1
                                            AND areaarmazenagem.fg_status = 2
                                            group by pallet.id_pallet, pallet.id_areaarmazenagem, agrupadorativo.id_agrupador, pallet.fg_status, areaarmazenagem.nr_posicaoy
                                            order by count (*), areaarmazenagem.nr_posicaoy";

                    using (var conexao = new SqlConnection(Global.Conexao))
                    {
                        palletNovo = await conexao.QueryFirstOrDefaultAsync<PalletSIAGModel?>(queryReservadaMenosCaixasSemSorter, new { identificadorCaracol = identificadorCaracol });
                    }

                    if (palletNovo == null)
                    {
                        var queryReservadaMenosCaixasSorter = @"select top 1 pallet.id_pallet AS IdPallet,
                                            pallet.id_areaarmazenagem AS IdAreaArmazenagem,
                                            agrupadorativo.id_agrupador AS IdAgrupador,
                                            pallet.fg_status AS FgStatus
                                            from areaarmazenagem WITH(NOLOCK)
                                            left join agrupadorativo WITH(NOLOCK)
                                                on agrupadorativo.id_agrupador = areaarmazenagem.id_agrupador
                                            left join caixa WITH(NOLOCK)
                                                on agrupadorativo.id_agrupador = caixa.id_agrupador
                                            inner JOIN pallet WITH(NOLOCK)
                                                ON areaarmazenagem.id_areaarmazenagem = pallet.id_areaarmazenagem
                                            where 
											(caixa.fg_status < 4 or caixa.fg_status = 8) AND caixa.dt_sorter IS NOT NULL
											AND CAST(id_endereco AS varchar(10)) + RIGHT('00' + CAST(nr_posicaox AS varchar(10)), 2) = @identificadorCaracol
											AND areaarmazenagem.id_endereco < 6
                                            AND agrupadorativo.id_agrupador IS NOT NULL
                                            AND pallet.fg_status = 1
                                            AND areaarmazenagem.fg_status = 2
                                            group by pallet.id_pallet, pallet.id_areaarmazenagem, agrupadorativo.id_agrupador, pallet.fg_status, areaarmazenagem.nr_posicaoy
                                            order by count (*), areaarmazenagem.nr_posicaoy";

                        using (var conexao = new SqlConnection(Global.Conexao))
                        {
                            palletNovo = await conexao.QueryFirstOrDefaultAsync<PalletSIAGModel?>(queryReservadaMenosCaixasSorter, new { identificadorCaracol = identificadorCaracol });
                        }

                        await LogBLL.GravarLog(
                            id_requisicao,
                            identificadorCaracol,
                            "",
                            $"Troca de pallet para área com menor quantidade de caixas que já passaram pelo sorter",
                            "VincularNovoPalletReservado",
                            "",
                            "info"
                        );
                    }
                    else
                    {
                        await LogBLL.GravarLog(
                            id_requisicao,
                            identificadorCaracol,
                            "",
                            $"Troca de pallet para área com menor quantidade de caixas antes do sorter",
                            "VincularNovoPalletReservado",
                            "",
                            "info"
                        );
                    }
                }

                if (palletNovo == null) return false;

                var queryAreaArmazenagemOrigem = "UPDATE areaarmazenagem " +
                        "SET id_agrupador = null, fg_status = 1 " +
                        "WHERE id_areaarmazenagem = @idAreaArmazenageOrigem";

                var queryAgrupadorDestino = "UPDATE agrupadorativo " +
                        "SET id_areaarmazenagem = null, fg_status = 2 " +
                        "WHERE id_agrupador = @idAgrupadorDestino";

                var queryAreaArmazenagemDestino = "UPDATE areaarmazenagem " +
                        "SET id_agrupador = @idAgrupadorOrigem, fg_status = 2 " +
                        "WHERE id_areaarmazenagem = @idAreaArmazenagemDestino";

                var queryAgrupadorOrigem = "UPDATE agrupadorativo " +
                        "SET id_areaarmazenagem = @idAreaArmazenagemDestino " +
                        "WHERE id_agrupador = @idAgrupadorOrigem";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    Console.WriteLine("------------------------------------------------------------");
                    Console.WriteLine("queryAreaArmazenagemOrigem => " + queryAreaArmazenagemOrigem);
                    Console.WriteLine("idAreaArmazenageOrigem => " + areaAtual.IdAreaArmazenagem);

                    var qtdLinhasAreaOrigem = await conexao.ExecuteAsync(queryAreaArmazenagemOrigem, new { idAreaArmazenageOrigem = areaAtual.IdAreaArmazenagem });



                    var qtdLinhasAgrupadorDestino = 1;
                    if (!reservadaSemAgrupador)
                    {
                        if (palletNovo.IdAgrupador == Guid.Empty)
                            throw new Exception("Pallet Novo sem agrupador.");

                        Console.WriteLine("------------------------------------------------------------");
                        Console.WriteLine("queryAgrupadorDestino => " + queryAgrupadorDestino);
                        Console.WriteLine("idAgrupadorDestino => " + palletNovo.IdAgrupador);

                        qtdLinhasAgrupadorDestino = await conexao.ExecuteAsync(queryAgrupadorDestino, new { idAgrupadorDestino = palletNovo.IdAgrupador });
                    }



                    Console.WriteLine("------------------------------------------------------------");
                    Console.WriteLine("queryAreaArmazenagemDestino => " + queryAreaArmazenagemDestino);
                    Console.WriteLine("idAreaArmazenagemDestino => " + palletNovo.IdAreaArmazenagem);
                    Console.WriteLine("idAgrupadorOrigem => " + areaAtual.IdAgrupador);

                    var qtdLinhasAreaArmazenagemDestino = await conexao.ExecuteAsync(queryAreaArmazenagemDestino, new
                    {
                        idAreaArmazenagemDestino = palletNovo.IdAreaArmazenagem,
                        idAgrupadorOrigem = areaAtual.IdAgrupador
                    });



                    Console.WriteLine("------------------------------------------------------------");
                    Console.WriteLine("queryAgrupadorOrigem => " + queryAgrupadorOrigem);
                    Console.WriteLine("idAreaArmazenagemDestino => " + palletNovo.IdAreaArmazenagem);
                    Console.WriteLine("idAgrupadorOrigem => " + areaAtual.IdAgrupador);

                    var qtdLinhasAgrupadorOrigem = await conexao.ExecuteAsync(queryAgrupadorOrigem, new
                    {
                        idAreaArmazenagemDestino = palletNovo.IdAreaArmazenagem,
                        idAgrupadorOrigem = areaAtual.IdAgrupador
                    });

                    await LogBLL.GravarLog(
                        id_requisicao,
                        identificadorCaracol,
                        "",
                        $"Troca do agrupador {palletNovo.IdAgrupador} da área {areaAtual} para a área {palletNovo.IdAreaArmazenagem} ",
                        "VincularNovoPalletReservado",
                        "",
                        "info"
                    );

                    //return qtdLinhasAreaArmazenagemDestino > 0 && qtdLinhasAreaOrigem > 0 && qtdLinhasAgrupadorOrigem > 0 && qtdLinhasAgrupadorDestino > 0;

                    return true;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }


        public static async Task<bool> GerarAtividadePalletCheio(string idPalletOrigem, string idAreaarmazenagemOrigem)
        {
            try
            {
                var queryAtividade = @"SELECT id_atividade as idAtividade
                    FROM atividade WITH(NOLOCK)
                    WHERE nm_atividade = 'Pallet cheio no sorter'";

                var query = "EXEC sp_siag_criachamada @id_atividade, @id_palletorigem, @id_areaarmazenagemorigem, null, 100100000";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var idAtividade = await conexao.QueryFirstOrDefaultAsync<int?>(queryAtividade);

                    if (idAtividade == null)
                        throw new Exception("Atividade 'Pallet cheio no sorter' não encontrada.");

                    var chamadaId = await conexao.QueryFirstOrDefaultAsync<Guid?>(query, new
                    {
                        id_atividade = idAtividade,
                        id_palletorigem = idPalletOrigem,
                        id_areaarmazenagemorigem = idAreaarmazenagemOrigem
                    });

                    return chamadaId != null;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<List<PalletModel>> GetPalletStatus(string caracol)
        {
            try
            {
                var idEndereco = caracol.Substring(0, 1);
                var posicaoX = caracol.Substring(1, 2);

                var areas = await SiagApi.GetAreaArmazenagemByCaracol(caracol);
                var resposta = new List<PalletModel>();

                foreach (var area in areas)
                {
                    var palletResposta = new PalletModel();
                    palletResposta.Posicao = area.PosicaoY;

                    if (area.FgStatus == 6)
                    {
                        palletResposta.Cor = "#57534e"; // cinza
                        palletResposta.Mensagem = "Bloqueado";
                    }
                    else if (area.FgStatus == 5)
                    {
                        palletResposta.Cor = "#292524"; // cinza claro
                        palletResposta.Mensagem = "Desabilitado";
                    }
                    else if (area.FgStatus == 4)
                    {
                        palletResposta.Cor = "#292524"; // cinza claro
                        palletResposta.Mensagem = "Manutenção";
                    }
                    else
                    {
                        var pallet = await PalletBLL.GetPalletByIdAreaArmazenagem(area.IdAreaArmazenagem);

                        if (pallet == null)
                        {
                            palletResposta.Cor = "#000000"; // preto
                            palletResposta.Mensagem = "Sem pallet";
                        }
                        else
                        {
                            palletResposta.CaixasEstufadas = await PalletBLL.ContarCaixasVinculadas(pallet.IdPallet ?? "");
                            palletResposta.CaixasPendentes = await PalletBLL.ContarCaixasPendentes(area.IdAgrupador.ToString());

                            if (area.FgStatus == 3)
                            {
                                if (pallet.FgStatus == 3)
                                {
                                    palletResposta.Cor = "#dc2626"; // vermelho
                                    palletResposta.Mensagem = "Cheio";
                                }
                                else if (pallet.FgStatus == 2)
                                {
                                    palletResposta.Cor = "#16a34a"; // verde
                                    palletResposta.Mensagem = "Ocupado";
                                }
                            }
                            else if (area.FgStatus == 2)
                            {
                                if (pallet.FgStatus == 1)
                                {
                                    palletResposta.Cor = "#cab604"; // amarelo
                                    palletResposta.Mensagem = "Reservado";
                                }
                                else if (pallet.FgStatus == 3)
                                {
                                    palletResposta.Cor = "#dc2626"; // vermelho
                                    palletResposta.Mensagem = "Cheio";
                                }
                            }
                            else if (area.FgStatus == 1)
                            {
                                if (pallet.FgStatus == 3)
                                {
                                    palletResposta.Cor = "#dc2626"; // vermelho
                                    palletResposta.Mensagem = "Cheio";
                                }
                                else if (pallet.FgStatus == 1)
                                {
                                    palletResposta.Cor = "#ffffff"; // branco
                                    palletResposta.Mensagem = "Livre";
                                }
                            }
                        }
                    }
                    var caracolRefugo = await ParametroBLL.GetParamentro("Identificador do Caracol de Refugo");

                    if (caracol == caracolRefugo)
                    {
                        var infoPosicao = await ParametroBLL.GetInfoPosicaoCaracolRefugo(palletResposta.Posicao ?? 0);
                        palletResposta.Descricao = infoPosicao != null ? infoPosicao.Descricao : null;
                    }

                    resposta.Add(palletResposta);
                }

                return resposta;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<bool> TrocaPallet(
                string identificadorCaracol,
                AreaArmazenagemSIAGModel areaArmazenagemCaixa,
                EquipamentoSIAGModel equipamentoAtual,
                string idCaixa,
                CaixaSIAGModel caixa,
                Guid id_requisicao
            )
        {
            await LogBLL.GravarLog(
                id_requisicao,
                identificadorCaracol,
                idCaixa,
                $"Iniciando troca de pallet para área {areaArmazenagemCaixa.IdAreaArmazenagem} no equipamento {equipamentoAtual}",
                "TrocaPallet",
                equipamentoAtual.IdOperador,
                "info"
            );

            var areaAtual = await SiagApi.GetAreaArmazenagemById(long.Parse(areaArmazenagemCaixa.IdAreaArmazenagem ?? ""));

            if (areaAtual == null)
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    identificadorCaracol,
                    idCaixa,
                    $"Área de armazenagem {areaArmazenagemCaixa.IdAreaArmazenagem} não localizada",
                    "TrocaPallet",
                    equipamentoAtual.IdOperador,
                    "erro"
                );

                throw new Exception("Área de Armazanagem não encontrado.");
            }

            if (areaAtual.IdAgrupador == Guid.Empty)
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    identificadorCaracol,
                    idCaixa,
                    $"Área de armazenagem {areaArmazenagemCaixa.IdAreaArmazenagem} sem agrupador",
                    "TrocaPallet",
                    equipamentoAtual.IdOperador,
                    "erro"
                );

                throw new Exception("Área de Armazanagem sem agrupador.");
            }

            var vinculouPorPrioridade = await PalletBLL.VincularNovoPalletPorPrioridade(identificadorCaracol, areaAtual, id_requisicao);

            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine("vinculouPorPrioridade => " + vinculouPorPrioridade);

            if (!vinculouPorPrioridade)
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    identificadorCaracol,
                    idCaixa,
                    $"Não foi encontrada nenhuma área livre disponível para realizar troca por prioridade",
                    "TrocaPallet",
                    equipamentoAtual.IdOperador,
                    "info"
                );

                var vinculouReservado = await PalletBLL.VincularNovoPalletReservado(caixa.IdAgrupador.ToString(), identificadorCaracol, areaAtual, id_requisicao);

                Console.WriteLine("------------------------------------------------------------");
                Console.WriteLine("vinculouReservado => " + vinculouReservado);

                if (!vinculouReservado)
                {
                    await LogBLL.GravarLog(
                        id_requisicao,
                        identificadorCaracol,
                        idCaixa,
                        $"Não foi encontrada nenhuma área reservada para realizar troca",
                        "TrocaPallet",
                        equipamentoAtual.IdOperador,
                        "info"
                    );

                    await EquipamentoBLL.LimparUltimaLeitura(equipamentoAtual.IdEquipamento ?? "", idCaixa);
                    throw new Exception("Pallet não disponível");
                }
                else
                {
                    var areaArmazenagemCaixaList = await SiagApi.GetAreaArmazenagemByAgrupador(caixa.IdAgrupador);

                    areaArmazenagemCaixa = areaArmazenagemCaixaList[0];

                    if (areaArmazenagemCaixa == null)
                    {
                        await LogBLL.GravarLog(
                            id_requisicao,
                            identificadorCaracol,
                            idCaixa,
                            $"Erro ao buscar nova área de armazenagem do agrupador {caixa.IdAgrupador} após troca de pallet",
                            "TrocaPallet",
                            equipamentoAtual.IdOperador,
                            "erro"
                        );

                        throw new Exception("Área de armazenagem não encontrada!");
                    }
                    else
                    {
                        await LogBLL.GravarLog(
                        id_requisicao,
                        identificadorCaracol,
                        idCaixa,
                        $"Toca da área {areaArmazenagemCaixa} realizada para área reservada",
                        "TrocaPallet",
                        equipamentoAtual.IdOperador,
                        "info"
                    );
                    }
                }
            }

            await LogBLL.GravarLog(
                id_requisicao,
                identificadorCaracol,
                idCaixa,
                $"Toca da área {areaArmazenagemCaixa} realizada para área livre considerando prioridade",
                "TrocaPallet",
                equipamentoAtual.IdOperador,
                "info"
            );

            Console.WriteLine("Reservar");

            // Reservar Área de Armazenagem
            using (var conexao = new SqlConnection(Global.Conexao))
            {
                await LogBLL.GravarLog(
                    id_requisicao,
                    identificadorCaracol,
                    idCaixa,
                    $"Realizando reserva da área {areaAtual} para agrupador {areaAtual.IdAgrupador}",
                    "TrocaPallet",
                    equipamentoAtual.IdOperador,
                    "info"
                );

                var areasReservadas = await GetAreasReservadas(equipamentoAtual.NmIdentificador, areaAtual.IdAgrupador, conexao);

                Console.WriteLine("areasReservadas => " + areasReservadas.Count);

                if (areasReservadas.Count == 0)
                {
                    await LogBLL.GravarLog(
                        id_requisicao,
                        identificadorCaracol,
                        idCaixa,
                        $"Agrupador {areaAtual.IdAgrupador} não possui nenhuma área reservada",
                        "TrocaPallet",
                        equipamentoAtual.IdOperador,
                        "info"
                    );

                    var qtdLinhasAreaArmazenagemReservada = await ReservarAreaArmazenagem(areaAtual, conexao, id_requisicao);
                    if (qtdLinhasAreaArmazenagemReservada <= 0) return false;
                }
                else
                {
                    await LogBLL.GravarLog(
                        id_requisicao,
                        identificadorCaracol,
                        idCaixa,
                        $"Agrupador {areaAtual.IdAgrupador} possui {areasReservadas.Count} área(s) reservada(s)",
                        "TrocaPallet",
                        equipamentoAtual.IdOperador,
                        "info"
                    );

                    var posicao = areasReservadas
                                        .Where(x => x.IdAgrupadorReservado == areaAtual.IdAgrupador &&
                                                    x.PosicaoY == areaAtual.PosicaoY)
                                        .FirstOrDefault();

                    if (posicao != null)
                    {
                        var qtdLinhasAreaArmazenagemReservada = await ReservarAreaArmazenagem(areaAtual, conexao, id_requisicao);
                        if (qtdLinhasAreaArmazenagemReservada <= 0) return false;
                    }
                }

                await LogBLL.GravarLog(
                    id_requisicao,
                    identificadorCaracol,
                    idCaixa,
                    $"Área de armazenagem {areaAtual} reservada para agrupador {areaAtual.IdAgrupador}",
                    "TrocaPallet",
                    equipamentoAtual.IdOperador,
                    "info"
                );
            }

            return true;
        }
    }
}