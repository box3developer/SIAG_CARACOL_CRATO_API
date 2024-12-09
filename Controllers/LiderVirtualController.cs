using dotnet_api.BLLs;
using dotnet_api.ModelsSIAG;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LiderVirtualController : Controller
    {
        [HttpGet("{identificadorCaracol}")]
        public async Task<ActionResult> Get(string identificadorCaracol)
        {
            try
            {
                var liderVirtual = await LiderVirtualBLL.GetLiderVirtualInfoByOrigem(identificadorCaracol);
                var operadorSIAG = new OperadorSIAGModel();
                var equipamentoDestino = new EquipamentoSIAGModel();
                ParametroMensagemCaracolSIAGModel msg;

                if (liderVirtual == null || DateTime.Now > liderVirtual.DtLoginLimite || liderVirtual.DtLogin != null)
                    return Ok(null);

                var operador = new Object();

                if (liderVirtual.IdEquipamentoDestino == null) {
                    msg = await ParametroBLL.GetMensagem("LV: sem próximo caracol");    
                }
                else {
                    operadorSIAG = await OperadorBLL.GetOperadorByCracha(liderVirtual.IdOperador ?? "");

                    if (operadorSIAG == null)
                        throw new Exception("Operador não encontrado.");

                    msg = await ParametroBLL.GetMensagem("LV: próximo caracol");
                    equipamentoDestino = await EquipamentoBLL.GetEquipamentoById(liderVirtual.IdEquipamentoDestino ?? "");
                    msg.Mensagem = msg.Mensagem?.Replace("{caracol}", equipamentoDestino?.NmIdentificador);

                    operador = new
                    {
                        Cracha = operadorSIAG?.IdOperador,
                        Nome = operadorSIAG?.NmOperador,
                        Cpf = operadorSIAG?.NmCpf,
                        Foto = $"http://cdsrvsob.sob.ad-grendene.com/SIAG/WebService/hdlBuscaFotoColaborador.ashx?sCPF={operadorSIAG?.NmCpf}"
                    };
                }


                return Ok(new {
                    dataLimite = liderVirtual.DtLoginLimite,
                    mensagem = msg,
                    mensagemDestino = await ParametroBLL.GetMensagem("LV: esperando operador"),
                    proximo = equipamentoDestino?.NmIdentificador,
                    Operador = operador
                });
            }
            catch (Exception ex)
            {
                return BadRequest(await ParametroBLL.GetMensagem(ex));
            }
        }
    }
}