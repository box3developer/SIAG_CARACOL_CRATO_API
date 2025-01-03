
using System.Transactions;
using dotnet_api.BLLs;
using dotnet_api.Integration;
using dotnet_api.ModelsSIAG;
using grendene_caracois_api_csharp;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace dotnet_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CaixaController : Controller
    {
        private readonly LockService _lockService;

        public CaixaController(ConnectionMultiplexer redis)
        {
            this._lockService = new LockService(redis);
        }

        private static async Task<EquipamentoSIAGModel> VerificaCaixaPendente(CaixaSIAGModel caixa, string identificadorCaracol, EquipamentoSIAGModel? equipamentoAtual, AreaArmazenagemSIAGModel? areaArmazenagemCaixa)
        {
            if (equipamentoAtual.CdCaixaPendente == caixa.IdCaixa)
            {
                var caixaEstufada = caixa.DtEstufamento != null && caixa.IdPallet != null;
                var caixaExpedida = caixa.FgStatus == 5;

                if (caixaExpedida)
                {
                    equipamentoAtual = await CaixaBLL.LiberarCaixaPendenteEquipamento(equipamentoAtual);

                    var ex = new Exception("Caixa não pertence ao local");
                    ex.Data.Add("caixa", caixa.IdCaixa);
                    throw ex;
                }
                else if (caixaEstufada)
                {
                    var palletAtualCaixa = await PalletBLL.GetPallet(caixa.IdPallet);

                    if (palletAtualCaixa.FgStatus != 2)
                    {
                        equipamentoAtual = await CaixaBLL.LiberarCaixaPendenteEquipamento(equipamentoAtual);

                        await CaixaBLL.GravarErro(
                            caixa.IdCaixa,
                            equipamentoAtual.IdEquipamento ?? "",
                            equipamentoAtual.IdOperador ?? "",
                            areaArmazenagemCaixa.IdAreaArmazenagem,
                            areaArmazenagemCaixa.IdEndereco,
                            12,
                            1
                        );

                        await CaixaBLL.InserirDesempenho(caixa.IdCaixa, equipamentoAtual.IdOperador ?? "", equipamentoAtual.IdEquipamento ?? "", null, 1);

                        var ex = new Exception("Caixa não pertence ao local");
                        ex.Data.Add("caixa", caixa.IdCaixa);
                        throw ex;
                    }

                    var areaArmazenagemAtualCaixa = await SiagApi.GetAreaArmazenagemById(long.Parse(palletAtualCaixa.IdAreaArmazenagem??""));

                    var equipamentoCaixa = await EquipamentoBLL.GetEquipamentoByIdentificadorCaracol(areaArmazenagemAtualCaixa.IdentificadorCaracol ?? "");

                    if (equipamentoCaixa == null)
                        throw new Exception("Caracol da Caixa não encontrado!");

                    if (identificadorCaracol != areaArmazenagemAtualCaixa.IdentificadorCaracol)
                    {
                        equipamentoAtual = await CaixaBLL.LiberarCaixaPendenteEquipamento(equipamentoAtual);

                        await CaixaBLL.GravarErro(
                            caixa.IdCaixa,
                            equipamentoAtual.IdEquipamento ?? "",
                            equipamentoAtual.IdOperador ?? "",
                            areaArmazenagemAtualCaixa.IdAreaArmazenagem,
                            areaArmazenagemAtualCaixa.IdEndereco,
                            12,
                            1
                        );

                        await CaixaBLL.InserirDesempenho(caixa.IdCaixa, equipamentoAtual.IdOperador ?? "", equipamentoAtual.IdEquipamento ?? "", null, 1);

                        var ex = new Exception("Caixa não pertence ao local");
                        ex.Data.Add("caixa", caixa.IdCaixa);
                        ex.Data.Add("outro_caracol", areaArmazenagemAtualCaixa.IdentificadorCaracol);
                        throw ex;
                    }
                    else
                    {
                        await SiagApi.RemoveEstufamento(caixa.IdCaixa ?? "");
                        await CaixaBLL.DesvincularCaixaComPallet(identificadorCaracol, areaArmazenagemCaixa.PosicaoY ?? 0, caixa.IdCaixa);
                    }
                }
            }

            return equipamentoAtual;
        }

        [HttpGet("Validar/{idCaixa}/{identificadorCaracol}")]
        public async Task<ActionResult> ValidarCaixa(string idCaixa, string identificadorCaracol)
        {
            Guid id_requisicao = Guid.NewGuid();

            try
            {
                using (this._lockService.Acquire(0, $"#ValidarLeitura{identificadorCaracol}", TimeSpan.FromSeconds(60)))
                {
                    await LogBLL.GravarLog(
                            id_requisicao,
                            identificadorCaracol,
                            idCaixa,
                            $"Inicia validação da caixa {idCaixa} no equipamento {identificadorCaracol}",
                            "ValidarCaixa",
                            "",
                            "info"
                        );

                    var equipamentoAtual = await EquipamentoBLL.GetEquipamentoByIdentificadorCaracol(identificadorCaracol ?? "");

                    if (equipamentoAtual == null)
                    {
                        await LogBLL.GravarLog(
                            id_requisicao,
                            identificadorCaracol,
                            idCaixa,
                            $"Caracol {identificadorCaracol} atual não encontrado!",
                            "ValidarCaixa",
                            "",
                            "erro"
                        );

                        throw new Exception("Caracol atual não encontrado!");
                    }

                    if (equipamentoAtual.IdOperador == null)
                    {
                        await LogBLL.GravarLog(
                            id_requisicao,
                            identificadorCaracol,
                            idCaixa,
                            $"Caracol {identificadorCaracol} sem opeador logado",
                            "ValidarCaixa",
                            "",
                            "erro"
                        );

                        throw new Exception("Faça login para ler caixas.");
                    }

                    if (identificadorCaracol == await ParametroBLL.GetParamentro("Identificador do Caracol de Refugo"))
                    {
                        await LogBLL.GravarLog(
                            id_requisicao,
                            identificadorCaracol,
                            idCaixa,
                            $"Caracol atual {identificadorCaracol} identificado como refugo",
                            "ValidarCaixa",
                            equipamentoAtual.IdOperador,
                            "info"
                        );

                        return Ok(await CaixaBLL.LeituraCaracolRefugo(idCaixa, id_requisicao));
                    }

                    if (idCaixa.Length != 16 && idCaixa.Length != 20)
                    {
                        await LogBLL.GravarLog(
                            id_requisicao,
                            identificadorCaracol,
                            idCaixa,
                            $"Código {idCaixa} inválido. Não atende a quantidade de caracters padrões de uma caixa (16 ou 20 caracters)",
                            "ValidarCaixa",
                            equipamentoAtual.IdOperador,
                            "erro"
                        );

                        throw new Exception("Código de barras inválido");
                    }

                    var caixa = await SiagApi.GetCaixaByIdAsync(idCaixa);

                    if (caixa == null)
                    {
                        await LogBLL.GravarLog(
                            id_requisicao,
                            identificadorCaracol,
                            idCaixa,
                            $"Caixa {idCaixa} não encontrada",
                            "ValidarCaixa",
                            equipamentoAtual.IdOperador,
                            "erro"
                        );

                        await CaixaBLL.GravarSiagLog(idCaixa);
                        throw new Exception("Caixa não encontrada");
                    }

                    var areaArmazenagemCaixaList = await SiagApi.GetAreaArmazenagemByAgrupador(caixa.IdAgrupador);
                    var areaArmazenagemCaixa = areaArmazenagemCaixaList[0];

                    if (areaArmazenagemCaixa == null)
                    {
                        await CaixaBLL.GravarErro(
                            idCaixa,
                            equipamentoAtual.IdEquipamento ?? "",
                            equipamentoAtual.IdOperador ?? "",
                            null,
                            null,
                            12,
                            1
                        );

                        await CaixaBLL.InserirDesempenho(idCaixa, equipamentoAtual.IdOperador ?? "", equipamentoAtual.IdEquipamento ?? "", null, 1);

                        await LogBLL.GravarLog(
                            id_requisicao,
                            identificadorCaracol,
                            idCaixa,
                            $"Agrupador {caixa.IdAgrupador} não localizado em nenhuma área de armazenagem",
                            "ValidarCaixa",
                            equipamentoAtual.IdOperador,
                            "erro"
                        );

                        var ex = new Exception("Caixa não pertence ao local");
                        ex.Data.Add("caixa", idCaixa);
                        throw ex;
                    }

                    var equipamentoCaixa = await EquipamentoBLL.GetEquipamentoByIdentificadorCaracol(areaArmazenagemCaixa.IdentificadorCaracol ?? "");

                    if (equipamentoCaixa == null)
                    {
                        await LogBLL.GravarLog(
                            id_requisicao,
                            identificadorCaracol,
                            idCaixa,
                            $"Equipamento {areaArmazenagemCaixa.IdentificadorCaracol} da área de armazenagem {areaArmazenagemCaixa.IdAreaArmazenagem} não localizado",
                            "ValidarCaixa",
                            equipamentoAtual.IdOperador,
                            "erro"
                        );

                        throw new Exception("Caracol da Caixa não encontrado!");
                    }

                    var ultimaCaixaLeitura = await CaixaBLL.GetUltimaLeitura(equipamentoAtual.IdEquipamento ?? "", 3, 51);

                    // Verifica caixa pendente ----------------------------
                    equipamentoAtual = await VerificaCaixaPendente(caixa, identificadorCaracol, equipamentoAtual, areaArmazenagemCaixa);
                    caixa = await SiagApi.GetCaixaByIdAsync(idCaixa); // atualiza status caixa


                    if (equipamentoAtual.CdUltimaLeitura != null && equipamentoAtual.CdUltimaLeitura != idCaixa)
                    {
                        await LogBLL.GravarLog(
                            id_requisicao,
                            identificadorCaracol,
                            idCaixa,
                            $"Equipamento com estufamento pendente para caixa {equipamentoAtual.CdUltimaLeitura}",
                            "ValidarCaixa",
                            equipamentoAtual.IdOperador,
                            "info"
                        );

                        var dadosCaixaUltimaLeitura = await SiagApi.GetCaixaByIdAsync(equipamentoAtual.CdUltimaLeitura);

                        if (dadosCaixaUltimaLeitura != null)
                        {
                            if (equipamentoAtual.CdCaixaPendente == null)
                            {
                                await LogBLL.GravarLog(
                                    id_requisicao,
                                    identificadorCaracol,
                                    idCaixa,
                                    $"Gerando pendência de estufamento para caixa {idCaixa} no caracol {identificadorCaracol}",
                                    "ValidarCaixa",
                                    equipamentoAtual.IdOperador,
                                    "info"
                                );

                                await CaixaBLL.SetCaixaPendente(idCaixa, equipamentoAtual.IdEquipamento ?? "");
                                equipamentoAtual.CdCaixaPendente = idCaixa;
                            }

                            await CaixaBLL.GravarErro(
                                idCaixa,
                                equipamentoAtual.IdEquipamento ?? "",
                                equipamentoAtual.IdOperador ?? "",
                                areaArmazenagemCaixa.IdAreaArmazenagem,
                                areaArmazenagemCaixa.IdEndereco,
                                51,
                                1
                            );

                            await LogBLL.GravarLog(
                                id_requisicao,
                                identificadorCaracol,
                                idCaixa,
                                $"Equipamento com estufamento pendente para caixa {equipamentoAtual.CdUltimaLeitura}",
                                "ValidarCaixa",
                                equipamentoAtual.IdOperador,
                                "erro"
                            );

                            var ex = new Exception("Estufamento: outra caixa pendente");
                            ex.Data.Add("caixa", equipamentoAtual.CdUltimaLeitura);
                            throw ex;
                        }
                    }

                    if (equipamentoAtual.CdUltimaLeitura == null &&
                    equipamentoAtual.CdCaixaPendente != null &&
                    equipamentoAtual.CdCaixaPendente != idCaixa)
                    {
                        await CaixaBLL.GravarErro(
                            idCaixa,
                            equipamentoAtual.IdEquipamento ?? "",
                            equipamentoAtual.IdOperador ?? "",
                            areaArmazenagemCaixa.IdAreaArmazenagem,
                            areaArmazenagemCaixa.IdEndereco,
                            51,
                            1
                        );

                        await LogBLL.GravarLog(
                                id_requisicao,
                                identificadorCaracol,
                                idCaixa,
                                $"Equipamento com estufamento pendente para caixa {equipamentoAtual.CdCaixaPendente}",
                                "ValidarCaixa",
                                equipamentoAtual.IdOperador,
                                "erro"
                            );

                        var ex = new Exception("Estufamento: outra caixa pendente");
                        ex.Data.Add("caixa", equipamentoAtual.CdCaixaPendente);
                        throw ex;
                    }


                    if (!string.IsNullOrWhiteSpace(equipamentoAtual.CdUltimaLeitura) && equipamentoAtual.CdUltimaLeitura != idCaixa)
                    {
                        //var fgStatus = ultimaCaixaLeitura == null ? 3 : 1;

                        await CaixaBLL.GravarErro(
                            idCaixa,
                            equipamentoAtual.IdEquipamento ?? "",
                            equipamentoAtual.IdOperador ?? "",
                            areaArmazenagemCaixa.IdAreaArmazenagem,
                            areaArmazenagemCaixa.IdEndereco,
                            51,
                            1
                        );

                        await LogBLL.GravarLog(
                                id_requisicao,
                                identificadorCaracol,
                                idCaixa,
                                $"Equipamento com estufamento pendente para caixa {equipamentoAtual.CdUltimaLeitura}",
                                "ValidarCaixa",
                                equipamentoAtual.IdOperador,
                                "erro"
                            );

                        var ex = new Exception("Estufamento: outra caixa pendente");
                        ex.Data.Add("caixa", equipamentoAtual.CdUltimaLeitura);
                        throw ex;
                    }

                    if (identificadorCaracol != equipamentoCaixa.NmIdentificador)
                    {
                        await LogBLL.GravarLog(
                                id_requisicao,
                                identificadorCaracol,
                                idCaixa,
                                $"Caixa {idCaixa} pertencente ao equipamento {equipamentoCaixa.NmIdentificador} lida no equipamento {identificadorCaracol}",
                                "ValidarCaixa",
                                equipamentoAtual.IdOperador,
                                "info"
                            );

                        if (equipamentoAtual.CdCaixaPendente == idCaixa)
                        {
                            await CaixaBLL.SetCaixaPendente(null, equipamentoAtual.IdEquipamento ?? "");
                            equipamentoAtual.CdCaixaPendente = null;

                            await LogBLL.GravarLog(
                                id_requisicao,
                                identificadorCaracol,
                                idCaixa,
                                $"Remove pendência de estufamento da {idCaixa} no equipamento {equipamentoAtual.IdEquipamento}",
                                "ValidarCaixa",
                                equipamentoAtual.IdOperador,
                                "info"
                            );
                        }

                        await CaixaBLL.GravarErro(
                            idCaixa,
                            equipamentoAtual.IdEquipamento ?? "",
                            equipamentoAtual.IdOperador ?? "",
                            areaArmazenagemCaixa.IdAreaArmazenagem,
                            areaArmazenagemCaixa.IdEndereco,
                            12,
                            1
                        );

                        await CaixaBLL.InserirDesempenho(idCaixa, equipamentoAtual.IdOperador ?? "", equipamentoAtual.IdEquipamento ?? "", null, 1);

                        await LogBLL.GravarLog(
                                id_requisicao,
                                identificadorCaracol,
                                idCaixa,
                                $"Caixa {idCaixa} não pertence ao equipamento {equipamentoAtual.NmIdentificador}",
                                "ValidarCaixa",
                                equipamentoAtual.IdOperador,
                                "erro"
                            );

                        var ex = new Exception("Caixa não pertence ao local");
                        ex.Data.Add("caixa", idCaixa);
                        ex.Data.Add("outro_caracol", equipamentoCaixa.NmIdentificador);
                        throw ex;
                    }

                    if (string.IsNullOrWhiteSpace(equipamentoAtual.CdUltimaLeitura) &&
                        !string.IsNullOrWhiteSpace(equipamentoAtual.CdCaixaPendente) &&
                        equipamentoAtual.CdCaixaPendente == idCaixa)
                    {
                        await LogBLL.GravarLog(
                                id_requisicao,
                                identificadorCaracol,
                                idCaixa,
                                $"Remove pendência da caixa {idCaixa} no equipamento {equipamentoAtual.IdEquipamento}",
                                "ValidarCaixa",
                                equipamentoAtual.IdOperador,
                                "info"
                            );

                        await CaixaBLL.LiberarCaixaPendenteEquipamento(equipamentoAtual);
                    }

                    if (caixa.DtExpedicao != null || caixa.IdPallet != null)
                    {
                        var expedida = caixa.DtExpedicao != null ? $"expedida em {caixa.DtExpedicao}" : "";
                        var estufadaPallet = caixa.IdPallet != null ? $"estufada no pallet {caixa.IdPallet}" : "";

                        await LogBLL.GravarLog(
                                id_requisicao,
                                identificadorCaracol,
                                idCaixa,
                                $"Caixa {idCaixa} {expedida} {estufadaPallet}",
                                "ValidarCaixa",
                                equipamentoAtual.IdOperador,
                                "erro"
                            );

                        var ex = new Exception("Caixa já expedida ou está em um pallet");
                        var caracol = await ParametroBLL.GetParamentro("Identificador do Caracol de Refugo");
                        ex.Data.Add("caracol_refugo", caracol);
                        ex.Data.Add("caixa", caixa.IdCaixa);
                        throw ex;
                    }

                    // Verifica pallet disponível ----------------------------
                    var pallet = await PalletBLL.GetPalletByIdAreaArmazenagem(areaArmazenagemCaixa.IdAreaArmazenagem);

                    if (pallet == null)
                    {
                        await LogBLL.GravarLog(
                                id_requisicao,
                                identificadorCaracol,
                                idCaixa,
                                $"Nenhum pallet encontrado para a área de armazenagem {areaArmazenagemCaixa.IdAreaArmazenagem}",
                                "ValidarCaixa",
                                equipamentoAtual.IdOperador,
                                "info"
                            );

                        Console.WriteLine("Pallet null");
                        try
                        {
                            await LogBLL.GravarLog(
                                id_requisicao,
                                identificadorCaracol,
                                idCaixa,
                                $"Tentativa de realiza troca de pallet da área de armazenagem {areaArmazenagemCaixa.IdAreaArmazenagem} no equipamento {identificadorCaracol}",
                                "ValidarCaixa",
                                equipamentoAtual.IdOperador,
                                "info"
                            );

                            await PalletBLL.TrocaPallet(identificadorCaracol ?? "", areaArmazenagemCaixa, equipamentoAtual, idCaixa, caixa, id_requisicao);

                            var areaArmazenagemCaixaListS = await SiagApi.GetAreaArmazenagemByAgrupador(caixa.IdAgrupador);
                            areaArmazenagemCaixa = areaArmazenagemCaixaListS[0];

                            pallet = await PalletBLL.GetPalletByIdAreaArmazenagem(areaArmazenagemCaixa.IdAreaArmazenagem);
                        }
                        catch (Exception)
                        {
                            await LogBLL.GravarLog(
                                id_requisicao,
                                identificadorCaracol,
                                idCaixa,
                                $"Nenhum pallet disponível para área de armazenagem {areaArmazenagemCaixa.IdAreaArmazenagem} no equipamento {identificadorCaracol}",
                                "ValidarCaixa",
                                equipamentoAtual.IdOperador,
                                "erro"
                            );

                            await EquipamentoBLL.LimparUltimaLeitura(equipamentoAtual.IdEquipamento ?? "", idCaixa);
                            throw new Exception("Pallet não disponível");
                        }
                    }

                    if (pallet != null && pallet.FgStatus != 1 && pallet.FgStatus != 2)
                    {
                        await LogBLL.GravarLog(
                                id_requisicao,
                                identificadorCaracol,
                                idCaixa,
                                $"Tentativa de troca da área de armazenagem {areaArmazenagemCaixa.IdAreaArmazenagem} do pallet {pallet.IdPallet} com status {pallet.FgStatus}",
                                "ValidarCaixa",
                                equipamentoAtual.IdOperador,
                                "info"
                            );

                        Console.WriteLine("Pallet com status livre");
                        await PalletBLL.TrocaPallet(identificadorCaracol ?? "", areaArmazenagemCaixa, equipamentoAtual, idCaixa, caixa, id_requisicao);

                        var areaArmazenagemCaixaListT = await SiagApi.GetAreaArmazenagemByAgrupador(caixa.IdAgrupador);
                        areaArmazenagemCaixa = areaArmazenagemCaixaListT[0];

                        pallet = await PalletBLL.GetPalletByIdAreaArmazenagem(areaArmazenagemCaixa.IdAreaArmazenagem);
                    }


                    // Verifica se a caixa estava pendente em outro equipamento ----------------------------
                    var equipamentoCaixaPendente = await EquipamentoBLL.GetEquipamentoByCaixaPendente(idCaixa);
                    if (equipamentoCaixaPendente != null)
                    {
                        CaixaBLL.SetCaixaPendente(null, equipamentoCaixaPendente.IdEquipamento ?? "");

                        await LogBLL.GravarLog(
                                id_requisicao,
                                identificadorCaracol,
                                idCaixa,
                                $"Remove pendência de estufamento do equipamento {equipamentoCaixaPendente.IdEquipamento}",
                                "ValidarCaixa",
                                equipamentoAtual.IdOperador,
                                "info"
                            );
                    }

                    await SiagApi.GravaLeituraCaixa(idCaixa, int.Parse(areaArmazenagemCaixa.IdAreaArmazenagem??""), int.Parse(pallet?.IdPallet??""));
                    await CaixaBLL.AcenderLuzVerde(identificadorCaracol ?? "", areaArmazenagemCaixa.PosicaoY);

                    var performanceDia = await OperadorBLL.CalcularPerformanceTurnoAtual(equipamentoAtual.IdOperador ?? "", id_requisicao);
                    var performanceHora = await OperadorBLL.CalcularPerformanceHoraAtual(equipamentoAtual.IdOperador ?? "", id_requisicao);

                    var mensagem = await ParametroBLL.GetMensagem("Leitura bem sucedida");

                    mensagem.Mensagem = mensagem.Mensagem?
                        .Replace("{caixa}", idCaixa)
                        .Replace("{posicao}", areaArmazenagemCaixa.PosicaoY.ToString());

                    await LogBLL.GravarLog(
                                id_requisicao,
                                identificadorCaracol,
                                idCaixa,
                                $"Finalizada validação da caixa {idCaixa} para posição {areaArmazenagemCaixa.PosicaoY} no equipamento {identificadorCaracol}",
                                "ValidarCaixa",
                                equipamentoAtual.IdOperador,
                                "info"
                            );

                    return Ok(new
                    {
                        Mensagem = mensagem,
                        Gaiola = areaArmazenagemCaixa.PosicaoY,
                        PerformanceDia = performanceDia,
                        PerformanceHora = performanceHora
                    });
                }
            }
            catch (Exception ex)
            {
                await LogBLL.GravarLog(
                                id_requisicao,
                                identificadorCaracol,
                                idCaixa,
                                $"Erro ao realizar validação da caixa {idCaixa} no equipamento {identificadorCaracol}",
                                "ValidarCaixa",
                                "",
                                "erro"
                            );

                System.Console.WriteLine(ex.ToString());
                return BadRequest(await ParametroBLL.GetMensagem(ex));
            }
        }

        [HttpGet("Estufar/{identificadorCaracol}")]
        public async Task<ActionResult> Estufar(string identificadorCaracol)
        {
            Guid id_requisicao = Guid.NewGuid();

            try
            {
                using (this._lockService.Acquire(0, $"#Estufamento{identificadorCaracol}", TimeSpan.FromSeconds(60)))
                {
                    await LogBLL.GravarLog(
                            id_requisicao,
                            identificadorCaracol,
                            "",
                            $"Inicia estufamento no equipamento {identificadorCaracol}",
                            "Estufar",
                            "",
                            "info"
                        );

                    Console.WriteLine("ESTUFANDO: " + identificadorCaracol);
                    var equipamento = await EquipamentoBLL.GetEquipamentoByIdentificadorCaracol(identificadorCaracol);

                    if (equipamento == null)
                    {
                        await LogBLL.GravarLog(
                            id_requisicao,
                            identificadorCaracol,
                            "",
                            $"Caracol {identificadorCaracol} atual não encontrado!",
                            "Estufar",
                            "",
                            "erro"
                        );

                        throw new Exception("Caracol não encontrado.");
                    }

                    var idCaixa = equipamento.CdUltimaLeitura;

                    if (string.IsNullOrWhiteSpace(idCaixa))
                    {
                        await LogBLL.GravarLog(
                            id_requisicao,
                            identificadorCaracol,
                            "",
                            $"Equipamento {identificadorCaracol} sem registro de última leitura",
                            "Estufar",
                            "",
                            "erro"
                        );

                        throw new Exception("Última leitura não encontrada.");
                    }

                    var caixa = await SiagApi.GetCaixaByIdAsync(idCaixa);

                    if (caixa == null)
                    {
                        await LogBLL.GravarLog(
                            id_requisicao,
                            identificadorCaracol,
                            idCaixa,
                            $"Não foi encontrada caixa correspondente a última leitura {idCaixa} no equipamento {identificadorCaracol}",
                            "Estufar",
                            "",
                            "erro"
                        );

                        throw new Exception("Caixa não encontrada.");
                    }

                    Console.WriteLine("CAIXA: " + caixa.IdCaixa);

                    if (caixa.FgStatus >= 4 && caixa.FgStatus != 8)
                    {
                        await LogBLL.GravarLog(
                            id_requisicao,
                            identificadorCaracol,
                            idCaixa,
                            $"Caixa {idCaixa} com status {caixa.FgStatus} (armazenada ou retrabalhada)",
                            "Estufar",
                            "",
                            "erro"
                        );

                        //Caixa armazenada ou retrabalhada
                        var ex = new Exception("Estufamento: Caixa armazenada ou retrabalhada");
                        ex.Data.Add("caracol_refugo", ParametroBLL.GetParamentro("Identificador do Caracol de Refugo"));
                        ex.Data.Add("caixa", caixa.IdCaixa);
                        throw ex;
                    }

                    /*
                    if (equipamento.IdOperador == null)
                        throw new Exception("Estufamento: caracol sem usuario logado");
                    */

                    var areaArmazenagemList = await SiagApi.GetAreaArmazenagemByAgrupador(caixa.IdAgrupador);
                    var areaArmazenagem = areaArmazenagemList[0];


                    if (areaArmazenagem == null)
                    {
                        await LogBLL.GravarLog(
                            id_requisicao,
                            identificadorCaracol,
                            idCaixa,
                            $"Agrupador {caixa.IdAgrupador} não localizado em nenhuma área de armazenagem",
                            "Estufar",
                            "",
                            "erro"
                        );

                        throw new Exception("Área de armazenagem não encontrada!");
                    }

                    var pallet = await PalletBLL.GetPalletByIdAreaArmazenagem(areaArmazenagem.IdAreaArmazenagem);

                    if (pallet == null)
                    {
                        await LogBLL.GravarLog(
                            id_requisicao,
                            identificadorCaracol,
                            idCaixa,
                            $"Nenhum pallet encontrado para a área de armazenagem {areaArmazenagem.IdAreaArmazenagem}",
                            "Estufar",
                            "",
                            "erro"
                        );

                        throw new Exception("Pallet não encontrado!");
                    }

                    using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions() { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
                    {
                        await LogBLL.GravarLog(
                            id_requisicao,
                            identificadorCaracol,
                            idCaixa,
                            $"Realiza estufamento da caixa {idCaixa} na posicao {areaArmazenagem.PosicaoY} do equipamento {identificadorCaracol}",
                            "Estufar",
                            "",
                            "info"
                        );

                        await CaixaBLL.VincularCaixaComPallet(identificadorCaracol, areaArmazenagem.PosicaoY ?? 0, idCaixa, caixa.IdAgrupador.ToString() ?? "", id_requisicao);
                        await SiagApi.EstufarCaixa(idCaixa, id_requisicao);
                        await CaixaBLL.InserirDesempenho(idCaixa, equipamento.IdOperador, equipamento.IdEquipamento ?? "", areaArmazenagem.IdAreaArmazenagem ?? "", 0);

                        var temCaixasPendentes = await CaixaBLL.TemCaixasPendentes(caixa.IdAgrupador.ToString() ?? "");

                        if (!temCaixasPendentes)
                        {
                            await LogBLL.GravarLog(
                                id_requisicao,
                                identificadorCaracol,
                                idCaixa,
                                $"Finaliza agrupador {caixa.IdAgrupador} da caixa {idCaixa} pois não possui caixas pendentes",
                                "Estufar",
                                "",
                                "info"
                            );

                            await PalletBLL.LiberaReservasAreaArmazenagem(areaArmazenagem.IdAgrupador, null, id_requisicao);
                            await SiagApi.FinalizaAgrupador(caixa.IdAgrupador ?? Guid.Empty, id_requisicao);
                            await CaixaBLL.AcenderLuzVermelha(equipamento.NmIdentificador ?? "", areaArmazenagem.PosicaoY, id_requisicao);
                            await SiagApi.LiberaAgrupador(caixa.IdAgrupador ?? Guid.Empty, id_requisicao);
                            await PalletBLL.GerarAtividadePalletCheio(pallet.IdPallet ?? "", areaArmazenagem.IdAreaArmazenagem ?? "");
                            await CaixaBLL.EncherPallet(pallet.IdPallet ?? "", id_requisicao);
                            await CaixaBLL.LiberarAreaArmazenagem(caixa.IdAgrupador.ToString() ?? "", id_requisicao);
                        }

                        await SiagApi.EmitirEstufamentoCaixa(equipamento.NmIdentificador ?? "", id_requisicao);
                        scope.Complete();
                    }

                    await LogBLL.GravarLog(
                                id_requisicao,
                                identificadorCaracol,
                                idCaixa,
                                $"Finalizado estufamento da caixa {idCaixa} para posição {areaArmazenagem.PosicaoY} no equipamento {identificadorCaracol}",
                                "Estufar",
                                "",
                                "info"
                            );

                    Console.WriteLine("ESTUFOU: " + identificadorCaracol);
                    return Ok();
                }
            }
            catch (Exception ex)
            {
                await LogBLL.GravarLog(
                                id_requisicao,
                                identificadorCaracol,
                                "",
                                $"Erro ao realizar estufamento no equipamento {identificadorCaracol}",
                                "Estufar",
                                "",
                                "erro"
                            );

                System.Console.WriteLine(ex.ToString());
                return BadRequest(await ParametroBLL.GetMensagem(ex));
            }
        }

        [HttpGet("GerarAtividadeTeste/{idAreaAramazenagem}/{idPallet}")]
        public async Task<ActionResult> GetCaixasTeste(string idAreaAramazenagem, string idPallet)
        {
            try
            {
                return Ok(await PalletBLL.GerarAtividadePalletCheio(idPallet, idAreaAramazenagem));
            }
            catch (Exception ex)
            {
                return BadRequest(await ParametroBLL.GetMensagem(ex));
            }
        }

        [HttpGet("CaixasTeste/{identificadorCaracol}")]
        public async Task<ActionResult> GetCaixasTeste(string identificadorCaracol)
        {
            try
            {
                return Ok(await CaixaBLL.GetCaixasTeste(identificadorCaracol));
            }
            catch (Exception ex)
            {
                return BadRequest(await ParametroBLL.GetMensagem(ex));
            }
        }

        [HttpGet("LeituraTeste/{identificadorCaracol}")]
        public async Task<ActionResult> GetLeiturasTeste(string identificadorCaracol)
        {
            try
            {
                var equipamento = await EquipamentoBLL.GetEquipamentoByIdentificadorCaracol(identificadorCaracol);

                if (equipamento == null)
                    throw new Exception("Caracol não encontrado.");

                return Ok(await CaixaBLL.GetLeiturasTeste(equipamento.IdEquipamento ?? ""));
            }
            catch (Exception ex)
            {
                return BadRequest(await ParametroBLL.GetMensagem(ex));
            }
        }
    }
}