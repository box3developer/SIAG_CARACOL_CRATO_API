using dotnet_api.BLLs;
using dotnet_api.Models;
using grendene_caracois_api_csharp;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OperadorController : Controller
    {
        [HttpGet("Validar/{cracha}/{identificadorCaracol}")]
        public async Task<ActionResult> ValidarOperador(string cracha, string identificadorCaracol)
        {
            try
            {
                var crachaCompleto = cracha.TrimStart('0');
                cracha = cracha.Substring(0, cracha.Length - 1);
                cracha = cracha.TrimStart('0');
                var equipamento = await EquipamentoBLL.GetEquipamentoByIdentificadorCaracol(identificadorCaracol);

                var caracolRefugo = await ParametroBLL.GetParamentro("Identificador do Caracol de Refugo");

                if (equipamento == null)
                    throw new Exception("Caracol não encontrado.");

                var operadorSIAG = await OperadorBLL.GetOperadorByCracha(cracha, crachaCompleto);

                if (operadorSIAG == null)
                    throw new Exception("Operador não cadastrado");

                //TODO: Funções que podem operar os caracois
                // if (operadorSIAG.FgFuncao != 2)
                //    throw new Exception("Operador sem permisão para operar o sorter");

                var equipamentoLogado = await EquipamentoBLL.GetEquipamentoByOperador(operadorSIAG.IdOperador ?? "");

                if (equipamentoLogado != null)
                    if (equipamentoLogado.IdEquipamento != equipamento.IdEquipamento)
                    {
                        if (equipamentoLogado.IdEquipamentoModelo != 1)
                            throw new Exception("Operador logado em outro tipo de equipamento");

                        var caracol = await EquipamentoBLL.GetCaracol(equipamentoLogado.NmIdentificador ?? "");

                        if (caracol == null)
                            throw new Exception("Caracol não encontrado.");

                        if (caracol.LuzVD != 0)
                        {
                            var ex = new Exception("Estufamento pendente em outro caracol");
                            ex.Data.Add("caracol", caracol.Caracol);
                            throw ex;
                        }

                        var liderVirtualPendente = await OperadorBLL.LiderVirtualPendente(operadorSIAG.IdOperador ?? "", equipamento.IdEquipamento ?? "");

                        if (liderVirtualPendente != null && identificadorCaracol != caracolRefugo)
                        {
                            var ex = new Exception("LV: próximo caracol");
                            ex.Data.Add("caracol", liderVirtualPendente.NmIdentificador);
                            throw ex;
                        }

                        await OperadorBLL.ValidaLoginLiderVirtual(operadorSIAG.IdOperador ?? "", equipamento.IdEquipamento ?? "");

                        await OperadorBLL.LogoffCaracol(equipamentoLogado.IdOperador ?? "", equipamentoLogado.IdEquipamento ?? "");
                        await OperadorBLL.LoginCaracol(operadorSIAG.IdOperador ?? "", equipamento.IdEquipamento ?? "");

                        return Ok(new OperadorModel
                        {
                            Cracha = operadorSIAG.IdOperador,
                            Nome = operadorSIAG.NmOperador,
                            Cpf = operadorSIAG.NmCpf,
                            Foto = $"http://cdsrvsob.sob.ad-grendene.com/SIAG/WebService/hdlBuscaFotoColaborador.ashx?sCPF={operadorSIAG.NmCpf}"
                        });
                    }

                var mesmoOperadaor = equipamento.IdOperador == operadorSIAG.IdOperador;
                var caixaPendente = !string.IsNullOrWhiteSpace(equipamento.CdUltimaLeitura);
                var operadorLogado = equipamento.IdEquipamento != null;

                if (mesmoOperadaor)
                {
                    if (caixaPendente)
                    {
                        return Ok(new
                        {
                            Cracha = operadorSIAG.IdOperador,
                            Nome = operadorSIAG.NmOperador,
                            Cpf = operadorSIAG.NmCpf,
                            Foto = $"http://cdsrvsob.sob.ad-grendene.com/SIAG/WebService/hdlBuscaFotoColaborador.ashx?sCPF={operadorSIAG.NmCpf}",
                            Mensagem = await ParametroBLL.GetMensagem("Logoff com estufamento pendente")
                        });
                    }
                    else
                    {
                        var liderVirtual = await LiderVirtualBLL.GetLiderVirtualInfoByDestino(equipamento.IdEquipamento ?? "");

                        if (liderVirtual != null && liderVirtual.DtLogin != null && liderVirtual.IdOperadorLogin == operadorSIAG.IdOperador)
                        {
                            liderVirtual.DtLogoff = DateTime.Now;
                            await LiderVirtualBLL.UpdateLiderVirtual(liderVirtual);
                        }

                        await EquipamentoBLL.ExecutarLiderVirtual(equipamento.NmIdentificador ?? "", operadorSIAG.IdOperador ?? "");
                        await OperadorBLL.LogoffCaracol(operadorSIAG.IdOperador ?? "", equipamento.IdEquipamento ?? "");
                        return Ok();
                    }
                }
                else
                {
                    if (operadorLogado)
                    {
                        if (caixaPendente)
                        {
                            throw new Exception("Logoff com estufamento pendente");
                        }
                        else
                        {
                            var liderVirtualPendente = await OperadorBLL.LiderVirtualPendente(operadorSIAG.IdOperador ?? "", equipamento.IdEquipamento ?? "");

                            if (liderVirtualPendente != null && identificadorCaracol != caracolRefugo)
                            {
                                var ex = new Exception("LV: próximo caracol");
                                ex.Data.Add("caracol", liderVirtualPendente.NmIdentificador);
                                throw ex;
                            }

                            await OperadorBLL.ValidaLoginLiderVirtual(operadorSIAG.IdOperador ?? "", equipamento.IdEquipamento ?? "");

                            await OperadorBLL.LogoffCaracol(equipamento.IdOperador ?? "", equipamento.IdEquipamento ?? "");
                            await OperadorBLL.LoginCaracol(operadorSIAG.IdOperador ?? "", equipamento.IdEquipamento ?? "");

                            return Ok(new OperadorModel
                            {
                                Cracha = operadorSIAG.IdOperador,
                                Nome = operadorSIAG.NmOperador,
                                Cpf = operadorSIAG.NmCpf,
                                Foto = $"http://cdsrvsob.sob.ad-grendene.com/SIAG/WebService/hdlBuscaFotoColaborador.ashx?sCPF={operadorSIAG.NmCpf}"
                            });
                        }
                    }
                    else
                    {
                        var liderVirtualPendente = await OperadorBLL.LiderVirtualPendente(operadorSIAG.IdOperador ?? "", equipamento.IdEquipamento ?? "");

                        if (liderVirtualPendente != null && identificadorCaracol != caracolRefugo)
                        {
                            var ex = new Exception("LV: próximo caracol");
                            ex.Data.Add("caracol", liderVirtualPendente.NmIdentificador);
                            throw ex;
                        }

                        await OperadorBLL.ValidaLoginLiderVirtual(operadorSIAG.IdOperador ?? "", equipamento.IdEquipamento ?? "");

                        await OperadorBLL.LoginCaracol(operadorSIAG.IdOperador ?? "", equipamento.IdEquipamento ?? "");

                        return Ok(new OperadorModel
                        {
                            Cracha = operadorSIAG.IdOperador,
                            Nome = operadorSIAG.NmOperador,
                            Cpf = operadorSIAG.NmCpf,
                            Foto = $"http://cdsrvsob.sob.ad-grendene.com/SIAG/WebService/hdlBuscaFotoColaborador.ashx?sCPF={operadorSIAG.NmCpf}"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(await ParametroBLL.GetMensagem(ex));
            }
        }

        [HttpGet("PossuiLuzAcesa/{identificadorCaracol}/{idCaixa}")]
        public async Task<ActionResult> PossuiLuzAcesa(string identificadorCaracol, string idCaixa)
        {
            try
            {
                var equipamento = await EquipamentoBLL.GetEquipamentoByIdentificadorCaracol(identificadorCaracol);

                if (equipamento == null)
                    throw new Exception("Caracol não encontrado.");

                if (!string.IsNullOrWhiteSpace(equipamento.CdUltimaLeitura) && equipamento.CdUltimaLeitura != idCaixa)
                {
                    var ex = new Exception("Estufamento de outra caixa pendente");
                    ex.Data.Add("caixa", equipamento.CdUltimaLeitura);
                    throw ex;
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(await ParametroBLL.GetMensagem(ex));
            }
        }

        [HttpGet("PerformanceDia/{idOperador}")]
        public async Task<ActionResult> GetPerformanceDia(string idOperador)
        {
            try
            {
                Guid id_requisicao = Guid.NewGuid();

                var performance = await OperadorBLL.CalcularPerformanceTurnoAtual(idOperador, id_requisicao);
                return Ok(performance);
            }
            catch (Exception ex)
            {
                return BadRequest(await ParametroBLL.GetMensagem(ex));
            }
        }

        [HttpGet("PerformanceHora/{idOperador}")]
        public async Task<ActionResult> GetPerformanceHora(string idOperador)
        {
            try
            {
                Guid id_requisicao = Guid.NewGuid();

                var performance = await OperadorBLL.CalcularPerformanceHoraAtual(idOperador, id_requisicao);
                return Ok(performance);
            }
            catch (Exception ex)
            {
                return BadRequest(await ParametroBLL.GetMensagem(ex));
            }
        }


        [HttpGet("testeTurno")]
        public async Task<ActionResult> TesteTurno()
        {
            try
            {
                var dataInicio = new DateTime(2022, 10, 25);
                var dataFim = new DateTime(2022, 10, 27);

                var dict = new Dictionary<DateTime, string>();
                while (dataInicio < dataFim)
                {
                    var turnoAtual = await OperadorBLL.GetTurno(dataInicio);
                    dict.Add(dataInicio, turnoAtual == null ? "SEM-TURNO" : turnoAtual.CodTurno ?? "SEMTURNO");
                    dataInicio = dataInicio.AddMinutes(1);
                }


                return Ok(new { turnos = Global.Turnos, horarios = dict });
            }
            catch (Exception ex)
            {
                return BadRequest(await ParametroBLL.GetMensagem(ex));
            }
        }
    }
}