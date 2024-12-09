using System.Text.Json;
using Dapper;
using dotnet_api.BLLs;
using dotnet_api.Models;
using dotnet_api.ModelsNodeRED;
using dotnet_api.Utils;
using grendene_caracois_api_csharp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace dotnet_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PalletController : Controller
    {
        [HttpGet("luzes")]
        public async Task<ActionResult> GetLuzes()
        {
            try
            {
                var query = "SELECT nm_identificador AS NmIdentificador FROM equipamento WITH(NOLOCK)";

                List<int> caracois;

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    caracois = (await conexao.QueryAsync<int>(query)).ToList();
                }

                var luzes = new List<CaracolNodeREDModel>();

                foreach (var caracol in caracois)
                {
                    var resposta = await WebRequestUtil.GetRequest($"{Global.NodeRedUrl}/luzes/{caracol}");
                    var luzCaracol = JsonSerializer.Deserialize<CaracolNodeREDModel>(resposta);

                    if (luzCaracol != null) luzes.Add(luzCaracol);
                }

                return Ok(luzes);
            }
            catch (Exception ex)
            {
                return BadRequest(await ParametroBLL.GetMensagem(ex));
            }
        }

        [HttpGet("Todos")]
        public async Task<ActionResult> GetPalets()
        {
            try
            {
                var pallets = new Dictionary<string, List<PalletModel>>();

                foreach (string key in Global.Mapa.Keys)
                    pallets.Add(key, await PalletBLL.GetPalletStatus(key));

                return Ok(pallets);
            }
            catch (Exception ex)
            {
                return BadRequest(await ParametroBLL.GetMensagem(ex));
            }
        }
    }
}