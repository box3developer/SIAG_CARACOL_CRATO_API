using dotnet_api.DTOs.AreaArmazenagem;
using dotnet_api.DTOs.Caixa;
using dotnet_api.DTOs.CaixaLeitura;
using dotnet_api.ModelsSIAG;

namespace dotnet_api.Utils
{
    public class Mapping
    {
        public static IEnumerable<AreaArmazenagemSIAGModel> ListAreaArmazenagemDTOToSiagModel(List<AreaArmazenagemDTO> areaArmazenagemList)
        {
            if(areaArmazenagemList == null)
            {
                return new List<AreaArmazenagemSIAGModel>();
            }

            var mappedList = areaArmazenagemList.Select(dto => new AreaArmazenagemSIAGModel
            {
                FgStatus = (int)dto.FgStatus,
                IdAgrupador = dto.IdAgrupador,
                IdAgrupadorReservado = dto.IdAgrupador,
                IdAreaArmazenagem = dto.IdAreaArmazenagem.ToString(),
                IdEndereco = dto.IdEndereco.ToString(),
                IdentificadorCaracol = dto.IdCaracol,
                PosicaoX = dto.NrPosicaoX,
                PosicaoY = dto.NrPosicaoY
            });

            return mappedList;
        }

        public static AreaArmazenagemSIAGModel AreaArmazenagemDTOToSiagModel(AreaArmazenagemDTO areaArmazenagem)
        {
            if (areaArmazenagem == null)
            {
                return new ();
            }

            var mapped = new AreaArmazenagemSIAGModel
            {
                FgStatus = (int)areaArmazenagem.FgStatus,
                IdAgrupador = areaArmazenagem.IdAgrupador,
                IdAgrupadorReservado = areaArmazenagem.IdAgrupador,
                IdAreaArmazenagem = areaArmazenagem.IdAreaArmazenagem.ToString(),
                IdEndereco = areaArmazenagem.IdEndereco.ToString(),
                IdentificadorCaracol = areaArmazenagem.IdCaracol,
                PosicaoX = areaArmazenagem.NrPosicaoX,
                PosicaoY = areaArmazenagem.NrPosicaoY
            };

            return mapped;
        }

        public static CaixaSIAGModel CaixaDTOToSiagModel(CaixaDTO caixa)
        {
            if (caixa == null)
            {
                return new ();
            }

            var mapped = new CaixaSIAGModel
            {
                DtEstufamento = caixa.DtEstufamento,
                DtExpedicao = caixa.DtExpedicao,
                DtLeitura = caixa.DtLeitura,
                DtSorter = caixa.DtSorter,
                FgStatus = caixa.FgStatus,
                IdAgrupador = caixa.IdAgrupador,
                IdCaixa = caixa.IdCaixa,
                IdPallet = caixa.IdPallet.ToString(),
                IdPedido = caixa.IdPedido
                
            };

            return mapped;
        }

        public static CaixaLeituraSIAGModel CaixaLeituraDTOToSiagModel(CaixaLeituraDTO caixaLeitura)
        {
            if (caixaLeitura == null)
            {
                return new();
            }

            var mapped = new CaixaLeituraSIAGModel
            {
                DtLeitura = caixaLeitura.DtLeitura,
                FgCancelado = caixaLeitura.FgCancelado,
                FgStatus = caixaLeitura.FgStatus,
                FgTipo = caixaLeitura.FgTipo,
                IdAreaArmazenagem = caixaLeitura.IdAreaArmazenagem.ToString(),
                IdCaixa = caixaLeitura.IdCaixa,
                IdCaixaLeitura = caixaLeitura.IdCaixaLeitura.ToString(),
                IdEndereco = caixaLeitura.IdEndereco.ToString(),
                IdEquipamento = caixaLeitura.IdEquipamento.ToString(),
                IdOperador = caixaLeitura.IdOperador.ToString(),
                IdOrdem = caixaLeitura.IdOrdem.ToString(),
                IdPallet = caixaLeitura.IdPallet.ToString()

            };

            return mapped;

        }


    }
}
