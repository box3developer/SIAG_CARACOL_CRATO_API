using Dapper;
using dotnet_api.ModelsSIAG;
using grendene_caracois_api_csharp;
using Microsoft.Data.SqlClient;

namespace dotnet_api.BLLs
{
    public class AreaArmazenagemBLL
    {
        public static async Task<AreaArmazenagemSIAGModel?> GetAreaArmazenagem(string idAreaArmazenagem)
        {
            try
            {
                var query = "SELECT CAST(id_endereco AS varchar(10)) + RIGHT('00' + CAST(nr_posicaox AS varchar(10)), 2) as IdentificadorCaracol, " +
                                "id_endereco AS IdEndereco, " +
                                "id_areaarmazenagem AS IdAreaArmazenagem, " + 
                                "id_agrupador AS IdAgrupador, " +
                                "fg_status AS FgStatus, " +
                                "nr_posicaox AS PosicaoX, " +
                                "nr_posicaoy AS PosicaoY " +
                             "FROM areaarmazenagem WITH(NOLOCK) " +
                             "WHERE id_areaarmazenagem = @idAreaArmazenagem";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var areaArmazenagem = await conexao.QueryFirstOrDefaultAsync<AreaArmazenagemSIAGModel>(query, new { idAreaArmazenagem = idAreaArmazenagem });
                    
                    return areaArmazenagem;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<AreaArmazenagemSIAGModel?> GetAreaArmazenagemByIdAgrupador(string? idAgrupador)
        {
            try
            {
                var query = "SELECT CAST(id_endereco AS varchar(10)) + RIGHT('00' + CAST(nr_posicaox AS varchar(10)), 2) as IdentificadorCaracol, " +
                                "id_areaarmazenagem AS IdAreaArmazenagem, " +
                                "id_agrupador AS IdAgrupador, " +
                                "fg_status AS FgStatus, " +
                                "nr_posicaox AS PosicaoX, " +
                                "nr_posicaoy AS PosicaoY " +
                             "FROM areaarmazenagem WITH(NOLOCK) " +
                             "WHERE id_agrupador = @idAgrupador";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var areaArmazenagem = await conexao.QueryFirstOrDefaultAsync<AreaArmazenagemSIAGModel>(query, new { idAgrupador = idAgrupador });

                    return areaArmazenagem;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<AreaArmazenagemSIAGModel?> GetAreaArmazenagemByPosicao(string identificadorCaracol, int posicaoY)
        {
            try
            {
                var query = "SELECT CAST(id_endereco AS varchar(10)) + RIGHT('00' + CAST(nr_posicaox AS varchar(10)), 2) AS IdentificadorCaracol, " +
                                "id_areaarmazenagem AS IdAreaArmazenagem, " +
                                "id_agrupador AS IdAgrupador, " +
                                "fg_status AS FgStatus, " +
                                "nr_posicaox AS PosicaoX, " +
                                "nr_posicaoy AS PosicaoY " +
                            "FROM areaarmazenagem WITH(NOLOCK) " +
                            "WHERE CAST(id_endereco AS varchar(10)) + RIGHT('00' + CAST(nr_posicaox AS varchar(10)), 2) = @identificadorCaracol " +
                                "AND nr_posicaoy = @posicaoY";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var areaArmazenagem = await conexao.QueryFirstOrDefaultAsync<AreaArmazenagemSIAGModel>(query, new
                    {
                        identificadorCaracol = identificadorCaracol,
                        posicaoY = posicaoY
                    });

                    return areaArmazenagem;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<List<AreaArmazenagemSIAGModel>> GetAreasArmazenagemByIdentificadoCaracol(string identificadorCaracol)
        {
            try
            {
                var query = "SELECT CAST(id_endereco AS varchar(10)) + RIGHT('00' + CAST(nr_posicaox AS varchar(10)), 2) AS IdentificadorCaracol, " +
                                "id_areaarmazenagem AS IdAreaArmazenagem, " +
                                "id_agrupador AS IdAgrupador, " +
                                "fg_status AS FgStatus, " +
                                "nr_posicaox AS PosicaoX, " +
                                "nr_posicaoy AS PosicaoY " +
                            "FROM areaarmazenagem WITH(NOLOCK) " +
                            "WHERE CAST(id_endereco AS varchar(10)) + RIGHT('00' + CAST(nr_posicaox AS varchar(10)), 2) = @identificadorCaracol " +
                            "ORDER BY nr_posicaoy DESC";

                using (var conexao = new SqlConnection(Global.Conexao))
                {
                    var areaArmazenagem = await conexao.QueryAsync<AreaArmazenagemSIAGModel>(query, new { identificadorCaracol = identificadorCaracol });
                    return areaArmazenagem.ToList();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}