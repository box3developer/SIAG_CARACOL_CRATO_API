using System.Transactions;
using dotnet_api.BLLs;
using dotnet_api.Models;
using dotnet_api.DTOs;
using dotnet_api.ModelsNodeRED;
using grendene_caracois_api_csharp;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace dotnet_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EquipamentoController : Controller
    {
        private readonly LockService _lockService;

        public EquipamentoController(ConnectionMultiplexer redis)
        {
            this._lockService = new LockService(redis);
        }

        [HttpGet("Niveis/{identificadorCaracol}")]
        public async Task<ActionResult> GetNiveis(string identificadorCaracol)
        {
            try
            {
                var niveis = EquipamentoBLL.GetNiveis(identificadorCaracol);

                var caracoisCheios = await EquipamentoBLL.GetCaracoisCheios();
                var caracoisCheiosId = (caracoisCheios).Where(x => x.Cheio == 1).Select(x => x.Caracol);

                var caixasPendentes = await EquipamentoBLL.GetQtdCaixasPendentesLiderVirtual();
                var caixasPendentesId = (caixasPendentes).Select(x => x.Key).ToList();

                var listaCaracois = await EquipamentoBLL.GetAllCaracoisWithOperador();
                var operadores = new Dictionary<string, string?>();

                foreach (var caracol in listaCaracois)
                    if (!string.IsNullOrWhiteSpace(caracol.NmIdentificador))
                        operadores.Add(caracol.NmIdentificador, caracol.IdOperador);

                var cheios = caracoisCheiosId.Count() > 0
                    ? niveis.Where(x => caracoisCheiosId.Contains(x.Key) && operadores[x.Key] == null).Select(x => x.Key).ToList()
                    : new List<string>();

                var pendentes = caixasPendentesId.Count() > 0
                    ? niveis.Where(x => caixasPendentesId.Contains(x.Key) && operadores[x.Key] == null).Select(x => x.Key).ToList()
                    : new List<string>();

                var caracolStatus = new Dictionary<string, CaracolStatusModel>();

                foreach (var caracol in caracoisCheios)
                {
                    if (niveis.ContainsKey(caracol.Caracol ?? ""))
                    {
                        caracolStatus.Add(caracol.Caracol ?? "", new CaracolStatusModel
                        {
                            Nivel = niveis[caracol.Caracol ?? ""],
                            CaixasPendentes = caixasPendentes
                                .Where(x => x.Key == (caracol.Caracol ?? ""))
                                .Select(x => x.Value)
                                .FirstOrDefault(),
                            CaracolCheio = caracol.Cheio == 1,
                            Operador = operadores[caracol.Caracol ?? ""]
                        });
                    }
                }

                return Ok(new
                {
                    Status = caracolStatus,
                    Cheios = cheios,
                    Pendentes = pendentes
                });
            }
            catch (Exception ex)
            {
                return BadRequest(await ParametroBLL.GetMensagem(ex));
            }
        }

        [HttpGet("Caracois")]
        public async Task<ActionResult> GetCaracois()
        {
            try
            {
                var caracois = await EquipamentoBLL.GetAllCaracoisWithOperador();
                return Ok(caracois);
            }
            catch (Exception ex)
            {
                return BadRequest(await ParametroBLL.GetMensagem(ex));
            }
        }

        [HttpGet("Status")]
        public async Task<ActionResult> statusCaracois()
        {
            try
            {
                var caixasPendents = await EquipamentoBLL.GetQtdCaixasPendentes();
                var caracoisCheios = await EquipamentoBLL.GetCaracoisCheios();

                var caracolStatus = new Dictionary<string, CaracolStatusModel>();

                foreach (var caracol in caracoisCheios)
                {
                    caracolStatus.Add(caracol.Caracol ?? "", new CaracolStatusModel
                    {
                        CaixasPendentes = caixasPendents.Where(x => x.Key == (caracol.Caracol ?? "")).Select(x => x.Value).FirstOrDefault(),
                        CaracolCheio = caracol.Cheio == 1
                    });
                }

                return Ok(caracolStatus);
            }
            catch (Exception ex)
            {
                return BadRequest(await ParametroBLL.GetMensagem(ex));
            }
        }

        [HttpPost("RotinaLuzVermelha")]
        public async Task<ActionResult> RotinaLuzVermelha(CaracolNodeREDModel caracol)
        {
            try
            {
                if (caracol != null)
                {
                    using (this._lockService.Acquire(0, $"#RotinaLuzVermelha{caracol.Caracol}", TimeSpan.FromSeconds(60)))
                    {
                        for (var i = 0; i < caracol.LuzesVM?.Count(); i++)
                            if (caracol.LuzesVM[i] > 0)
                            {
                                using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions() { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled))
                                {
                                    var atualizouStatusPallet = await EquipamentoBLL.RotinaLuzVermelha(caracol.Caracol ?? "", i + 1);
                                    scope.Complete();
                                }
                            }
                    }
                }

                return Ok(true);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
                return BadRequest(await ParametroBLL.GetMensagem(ex));
            }
        }

        [HttpPost("TesteLiderVirtual/{identificadorCaracol}")]
        public async Task<ActionResult> TesteLiderVirtual(string identificadorCaracol)
        {
            try
            {
                return Ok(await EquipamentoBLL.GetProximoLiderVirtual(identificadorCaracol));
            }
            catch (Exception ex)
            {
                return BadRequest(await ParametroBLL.GetMensagem(ex));
            }
        }

        
        [HttpPost("AtualizaStatusLeitor")]
        public async Task<ActionResult> AtualizaStatusLeitor(StatusLeitorModel statusLeitorModel)
        {
            try
            {
                var result = await EquipamentoBLL.AtualizaStatusLeitorAsync(statusLeitorModel);
                return Ok(result);
            }
	        catch (Exception ex)
	        {
                return BadRequest(ex.Message);
	        }
        }

        [HttpPost("StatusLeitores")]
        public async Task<ActionResult> GetStatusLeitor(FiltroStatusLeitorDTO filtroStatusLeitor)
        {
            try
            {
                var result = await EquipamentoBLL.GetStatusLeitorAsync(filtroStatusLeitor);
                return Ok(result);
            }
	        catch (Exception ex)
	        {
                return BadRequest(ex.Message);
	        }
        }
    }
}