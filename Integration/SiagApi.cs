using dotnet_api.Models;
using dotnet_api.ModelsSIAG;
using grendene_caracois_api_csharp;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;

namespace dotnet_api.Integration
{
    public class SiagApi
    {
        private static readonly HttpClient client = new ();

        public static async Task<CaixaSIAGModel?> GetCaixaByIdAsync(string Id)
        {
            try
            {
                var url = $"{Global.SiagApi}/Caixa/{Id}";
                var caixa = await client.GetFromJsonAsync<CaixaSIAGModel>(url);
                return caixa ?? new();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static async Task<bool> EmitirEstufamentoCaixa(string identificadorCaracol, Guid? id_requisicao)
        {
            try
            {
                var url = $"{Global.SiagApi}/Caixa/emitir-estufamento";
                var parameters = new Dictionary<string, string?>
                {
                    {"identificadorCaracol",$"{identificadorCaracol}"},
                    {"id_requisicao",$"{id_requisicao}"}
                };

                var urlParams = QueryHelpers.AddQueryString(url, parameters);

                var status = await client.PutAsync(urlParams, null);

                var response = await status.Content.ReadAsStringAsync();

                return bool.Parse(response);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public static async Task<bool> EstufarCaixa(string idCaixa, Guid? id_requisicao)
        {
            try
            {
                var url = $"{Global.SiagApi}/Caixa/estufar";
                var parameters = new Dictionary<string, string?>
                {
                    {"idCaixa",$"{idCaixa}"},
                    {"id_requisicao",$"{id_requisicao}"}
                };

                var urlParams = QueryHelpers.AddQueryString(url, parameters);

                var status = await client.PutAsync(urlParams, null);

                var response = await status.Content.ReadAsStringAsync();

                return bool.Parse(response);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public static async Task<int> GetStatusAgrupadorAtivo(Guid idAgrupador) 
        {
            try
            {
                var url = $"{Global.SiagApi}/AgrupadorAtivo/status/{idAgrupador}";

                var status = await client.GetAsync(url);

                var response = await status.Content.ReadAsStringAsync();

                return int.Parse(response);
            }
            catch(Exception ex) 
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public static async Task<bool> FinalizaAgrupador(Guid idAgrupador, Guid idRequisicao)
        {
            try
            {
                var url = $"{Global.SiagApi}/AgrupadorAtivo/finaliza";
                var parameters = new Dictionary<string, string?>
                {
                    {"idAgrupador",$"{idAgrupador}"},
                    {"idRequisicao",$"{idRequisicao}"}
                };

                var urlParams = QueryHelpers.AddQueryString(url, parameters);

                var status = await client.PutAsync(urlParams,null);

                var response = await status.Content.ReadAsStringAsync();

                return bool.Parse(response);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public static async Task<bool> LiberaAgrupador(Guid idAgrupador, Guid idRequisicao)
        {
            try
            {
                var url = $"{Global.SiagApi}/AgrupadorAtivo/libera";
                var parameters = new Dictionary<string, string?>
                {
                    {"idAgrupador",$"{idAgrupador}"},
                    {"idRequisicao",$"{idRequisicao}"}
                };

                var urlParams = QueryHelpers.AddQueryString(url, parameters);

                var status = await client.PutAsync(urlParams, null);

                var response = await status.Content.ReadAsStringAsync();

                return bool.Parse(response);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public static async Task<string?> GetCaixaFabricaAsync(string Id)
        {
            try
            {
                var url = $"{Global.SiagApi}/Caixa/fabrica-caixa/{Id}";
                var result = await client.GetAsync(url);
                return await result.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static async Task<bool> GravaLeituraCaixa(string idCaixa, int idArea, int idPallet)
        {
            try
            {
                var url = $"{Global.SiagApi}/Caixa/grava-leitura";

                var parameters = new Dictionary<string, string?>
                {
                    {"idCaixa",$"{idCaixa}"},
                    {"idArea",$"{idArea}"},
                    {"idPallet",$"{idPallet}"},
                };

                var urlParams = QueryHelpers.AddQueryString(url, parameters);

                var response = await client.PatchAsync(urlParams,null);
                var responseJson = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<bool>(responseJson);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static async Task<bool> RemoveEstufamento(string IdCaixa)
        {
            if (string.IsNullOrEmpty(IdCaixa)) return false; 

            try
            {
                var url = $"{Global.SiagApi}/Caixa/remove-estufamento/{IdCaixa}";

                var response = await client.PatchAsync(url, null);
                var responseJson = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<bool>(responseJson);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static async Task<AreaArmazenagemSIAGModel> GetAreaArmazenagemById(long Id)
        {
            try
            {
                var url = $"{Global.SiagApi}/AreaArmazenagem/{Id}";
                var areaArmazenagem = await client.GetFromJsonAsync <AreaArmazenagemSIAGModel>(url);

                return areaArmazenagem ?? new();

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static async Task<List<AreaArmazenagemSIAGModel>> GetAreaArmazenagemByAgrupador(Guid? agrupadorId)
        {
            try
            {
                var url = $"{Global.SiagApi}/AreaArmazenagem/agrupador/{agrupadorId}";
                var areaArmazenagemList = await client.GetFromJsonAsync<List<AreaArmazenagemSIAGModel>>(url);

                return areaArmazenagemList ?? new();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static async Task<AreaArmazenagemSIAGModel> GetAreaArmazenagemByPosicao(string idCaracol, int posicaoY)
        {
            try
            {
                var url = $"{Global.SiagApi}/AreaArmazenagem/posicao";

                var parameters = new Dictionary<string, string?>
                {
                    {"identificadorCaracol",$"{idCaracol}"},
                    {"posicaoY",$"{posicaoY}"}
                };

                var urlParams = QueryHelpers.AddQueryString(url, parameters);
                var areaArmazenagem = await client.GetFromJsonAsync<AreaArmazenagemSIAGModel>(urlParams);

                return areaArmazenagem ?? new();
            }
               
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
    }
}

        public static async Task<List<AreaArmazenagemSIAGModel>> GetAreaArmazenagemByCaracol(string idCaracol)
        {
            try
            {
                var url = $"{Global.SiagApi}/AreaArmazenagem/caracol/{idCaracol}";
                var areaArmazenagemList = await client.GetFromJsonAsync<List<AreaArmazenagemSIAGModel>>(url);

                return areaArmazenagemList ?? new();

            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
