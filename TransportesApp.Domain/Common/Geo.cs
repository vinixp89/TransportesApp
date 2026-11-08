namespace TransportesApp.Domain.Common
{
    public static class Geo
    {
        // Distância em km entre dois pontos (fórmula de Haversine) — mesma usada em MotoristaService e
        // CorridaService, aqui exposta pro despacho de corridas Executivo (ver
        // DespachoCorridasExecutivoService).
        public static double DistanciaKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double raioTerraKm = 6371;

            var dLat = ParaRadianos(lat2 - lat1);
            var dLon = ParaRadianos(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ParaRadianos(lat1)) * Math.Cos(ParaRadianos(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            return raioTerraKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ParaRadianos(double graus) => graus * Math.PI / 180;
    }
}
