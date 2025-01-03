using dotnet_api.Models;
using dotnet_api.ModelsSIAG;

namespace grendene_caracois_api_csharp
{
    public static class Global
    {
        //public static string Conexao = "Server=cldbsob-tst;Database=SIAG;User Id=siagdb;Password=LlHJaaDuuI12;MultipleActiveResultSets=False;Encrypt=False;TrustServerCertificate=False;Connection Timeout=30";
        //public static string Conexao = "Server=swsrvsob01;Database=SIAG;User Id=dev;Password=uDevFs01.;MultipleActiveResultSets=False;Encrypt=False;TrustServerCertificate=False;Connection Timeout=30";
        public static string Conexao = "Server=dbsiag.sob.ad-grendene.com;Database=SIAG;User Id=siag;Password=J2GpePekBTCzbh09OwVG;MultipleActiveResultSets=False;Encrypt=False;TrustServerCertificate=False;Connection Timeout=3000";
        public static string NodeRedUrl = "http://gra-lxsobcaracol.sob.ad-grendene.com:1880";
        public static string SiagApi = "";
        public static List<TurnoSIAGModel>? Turnos = null;
        public static DateTime? DataTurnos = null;
        public static List<EficienciaModel>? Eficiencias = null;
        public static DateTime? DataEficiencias = null;
        public static int? MetaPorHora = null;
        public static DateTime? DataMeta = null;
        public static Dictionary<string, List<int>> Mapa = new Dictionary<string, List<int>> {
            { "101", new List<int>{0, 0} },  { "201", new List<int>{1, 0} },  { "301", new List<int>{2, 0} },  { "401", new List<int>{3, 0} }, { "501", new List<int>{4, 0} },
            { "102", new List<int>{0, 0} },  { "202", new List<int>{1, 0} },  { "302", new List<int>{2, 0} },  { "402", new List<int>{3, 0} }, { "502", new List<int>{4, 0} },
            { "103", new List<int>{0, 1} },  { "203", new List<int>{1, 1} },  { "303", new List<int>{2, 1} },  { "403", new List<int>{3, 1} }, { "503", new List<int>{4, 1} },
            { "104", new List<int>{0, 1} },  { "204", new List<int>{1, 1} },  { "304", new List<int>{2, 1} },  { "404", new List<int>{3, 1} }, { "504", new List<int>{4, 1} },
            { "105", new List<int>{0, 2} },  { "205", new List<int>{1, 2} },  { "305", new List<int>{2, 2} },  { "405", new List<int>{3, 2} }, { "505", new List<int>{4, 2} },
            { "106", new List<int>{0, 2} },  { "206", new List<int>{1, 2} },  { "306", new List<int>{2, 2} },  { "406", new List<int>{3, 2} }, { "506", new List<int>{4, 2} },
            { "107", new List<int>{0, 3} },  { "207", new List<int>{1, 3} },  { "307", new List<int>{2, 3} },  { "407", new List<int>{3, 3} }, { "507", new List<int>{4, 3} },
            { "108", new List<int>{0, 3} },  { "208", new List<int>{1, 3} },  { "308", new List<int>{2, 3} },  { "408", new List<int>{3, 3} }, { "508", new List<int>{4, 3} },
            { "109", new List<int>{0, 4} },  { "209", new List<int>{1, 4} },  { "309", new List<int>{2, 4} },  { "409", new List<int>{3, 4} }, { "509", new List<int>{4, 4} },
            { "110", new List<int>{0, 4} },  { "210", new List<int>{1, 4} },  { "310", new List<int>{2, 4} },  { "410", new List<int>{3, 4} }, { "510", new List<int>{4, 4} },
            { "111", new List<int>{0, 5} },  { "211", new List<int>{1, 5} },  { "311", new List<int>{2, 5} },  { "411", new List<int>{3, 5} }, { "511", new List<int>{4, 5} },
            { "112", new List<int>{0, 5} },  { "212", new List<int>{1, 5} },  { "312", new List<int>{2, 5} },  { "412", new List<int>{3, 5} }, { "512", new List<int>{4, 5} },
            { "113", new List<int>{0, 6} },  { "213", new List<int>{1, 6} },  { "313", new List<int>{2, 6} },  { "413", new List<int>{3, 6} }, { "513", new List<int>{4, 6} },
            { "114", new List<int>{0, 6} },  { "214", new List<int>{1, 6} },  { "314", new List<int>{2, 6} },  { "414", new List<int>{3, 6} }, { "514", new List<int>{4, 6} },
            { "115", new List<int>{0, 7} },  { "215", new List<int>{1, 7} },  { "315", new List<int>{2, 7} },  { "415", new List<int>{3, 7} }, { "515", new List<int>{4, 7} },
            { "116", new List<int>{0, 7} },  { "216", new List<int>{1, 7} },  { "316", new List<int>{2, 7} },  { "416", new List<int>{3, 7} }, { "516", new List<int>{4, 7} },
            { "117", new List<int>{0, 8} },  { "217", new List<int>{1, 8} },  { "317", new List<int>{2, 8} },  { "417", new List<int>{3, 8} }, { "517", new List<int>{4, 8} },
            { "118", new List<int>{0, 8} },  { "218", new List<int>{1, 8} },  { "318", new List<int>{2, 8} },  { "418", new List<int>{3, 8} }, { "518", new List<int>{4, 8} },
            { "119", new List<int>{0, 9} },  { "219", new List<int>{1, 9} },  { "319", new List<int>{2, 9} },  { "419", new List<int>{3, 9} }, { "519", new List<int>{4, 9} },
            { "120", new List<int>{0, 9} },  { "220", new List<int>{1, 9} },  { "320", new List<int>{2, 9} },  { "420", new List<int>{3, 9} }, { "520", new List<int>{4, 9} },
            { "121", new List<int>{0, 10} }, { "221", new List<int>{1, 10} }, { "321", new List<int>{2, 10} },
            { "122", new List<int>{0, 10} }, { "222", new List<int>{1, 10} }, { "322", new List<int>{2, 10} }
        };
        public static SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
    }
}

/*
 * 
cd /var/www &&
mv /home/suporte/publish.zip /var/www &&
unzip publish.zip &&
rm -r caracois-api/ &&
mv publish caracois-api &&
sudo systemctl restart caracois_api

cp caracois-api.old2/appsettings.json caracois-api/
 */