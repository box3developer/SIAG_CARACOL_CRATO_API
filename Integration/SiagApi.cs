using Azure;
using dotnet_api.DTOs.AreaArmazenagem;
using dotnet_api.DTOs.Caixa;
using dotnet_api.DTOs.CaixaLeitura;
using dotnet_api.Models;
using dotnet_api.ModelsSIAG;
using dotnet_api.Utils;
using grendene_caracois_api_csharp;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.NetworkInformation;
using System.Text;
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
                var caixa = await client.GetFromJsonAsync<CaixaDTO>(url);

                if (caixa == null)
                    return new ();

                var objSiag = Mapping.CaixaDTOToSiagModel(caixa);

                return objSiag;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static async Task<bool> VinculaCaixaPallet(string identificadorCaracol, int posicaoY, string idCaixa, Guid idAgrupador, Guid? id_requisicao)
        {
            try
            {
                var url = $"{Global.SiagApi}/Caixa/vincula-caixa-pallet";

                var body = new
                {
                    identificadorCaracol,
                    posicaoY,
                    idCaixa,
                    idAgrupador,
                    id_requisicao
                };

                var content = JsonContent.Create(body);

                var response = await client.PatchAsync(url, content);
                var result = await response.Content.ReadAsStringAsync();

                return bool.Parse(result);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static async Task<bool> DesvinculaCaixaPallet(string identificadorCaracol, int posicaoY, string idCaixa, Guid idAgrupador, Guid? id_requisicao)
        {
            try
            {
                var url = $"{Global.SiagApi}/Caixa/desvincula-caixa-pallet";

                var body = new
                {
                    identificadorCaracol,
                    posicaoY,
                    idCaixa,
                    idAgrupador,
                    id_requisicao
                };

                var content = JsonContent.Create(body);

                var response = await client.PatchAsync(url, content);
                var result = await response.Content.ReadAsStringAsync();

                return bool.Parse(result);
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

        public static async Task<CaixaLeituraSIAGModel> GetUltimaLeitura(int idEquipamento, int fgStatus, int fgTipo)
        {
            try
            {
                var url = $"{Global.SiagApi}/CaixaLeitura/ultima-leitura";

                var parameters = new Dictionary<string, string?>
                {
                    { "idEquipamento", idEquipamento.ToString() },
                    { "fgStatus", fgStatus.ToString() },
                    { "fgTipo", fgTipo.ToString() }
                };
                var urlParams = QueryHelpers.AddQueryString(url, parameters);

                var response = await client.GetFromJsonAsync<CaixaLeituraDTO>(urlParams);

                if (response == null)
                    return new();

                var obj = Mapping.CaixaLeituraDTOToSiagModel(response);

                return obj;

                


            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }
        public static async Task<bool> TemCaixaPendente(Guid idAgrupador)
        {
            try
            {
                var url = $"{Global.SiagApi}/Caixa/existe-pendentes/{idAgrupador}";

                var status = await client.GetAsync(url);

                var response = await status.Content.ReadAsStringAsync();

                return bool.Parse(response);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public static async Task<bool> CreateCaixaLeitura(CaixaLeituraDTO caixaLeitura)
        {
            try
            {
                var url = $"{Global.SiagApi}/CaixaLeitura";

                var jsonBody = JsonSerializer.Serialize(caixaLeitura);

                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var status = await client.PostAsync(url, content);

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

        public static async Task<bool> LiberarAreaArmazenagemAsync(Guid idAgrupador, Guid? id_requisicao)
        {
            try
            {
                var url = $"{Global.SiagApi}/AreaArmazenagem/liberar/agrupador/{idAgrupador}/requisição/{id_requisicao}";
                var response = await client.GetAsync(url);

                var result = await response.Content.ReadAsStringAsync();

                return bool.Parse(result);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
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
                var areaArmazenagem = await client.GetFromJsonAsync <AreaArmazenagemDTO>(url);

                if (areaArmazenagem == null)
                    return new ();

                var obj = Mapping.AreaArmazenagemDTOToSiagModel(areaArmazenagem);
                


                return obj;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static async Task<IEnumerable<AreaArmazenagemSIAGModel>> GetAreaArmazenagemByAgrupador(Guid? agrupadorId)
        {
            try
            {
                var url = $"{Global.SiagApi}/AreaArmazenagem/agrupador/{agrupadorId}";
                var areaArmazenagemList = await client.GetFromJsonAsync<List<AreaArmazenagemDTO>>(url);
                if (areaArmazenagemList == null)
                    return new List<AreaArmazenagemSIAGModel>();
                
                var list = Mapping.ListAreaArmazenagemDTOToSiagModel(areaArmazenagemList);


                return list;
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
                var areaArmazenagem = await client.GetFromJsonAsync<AreaArmazenagemDTO>(urlParams);
                if (areaArmazenagem == null)
                    return new();

                var objMapped = Mapping.AreaArmazenagemDTOToSiagModel(areaArmazenagem);

                return objMapped;
            }
               
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
    }
}

        public static async Task<IEnumerable<AreaArmazenagemSIAGModel>> GetAreaArmazenagemByCaracol(string idCaracol)
        {
            try
            {
                var url = $"{Global.SiagApi}/AreaArmazenagem/caracol/{idCaracol}";
                var areaArmazenagemList = await client.GetFromJsonAsync<List<AreaArmazenagemDTO>>(url);

                if (areaArmazenagemList == null)
                    return new List<AreaArmazenagemSIAGModel>();

                var list = Mapping.ListAreaArmazenagemDTOToSiagModel(areaArmazenagemList);

                return list;

            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static async Task<bool> EncherPallet(int idPallet, Guid? id_requisicao)
        {
            try
            {
                var url = $"{Global.SiagApi}/Pallet/encher-pallet/pallet/{idPallet}/requisicao/{id_requisicao}";
                var response = await client.PutAsync(url,null);

                var result = await response.Content.ReadAsStringAsync();

                return bool.Parse(result);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

    }
}
