using System.Globalization;

namespace Votaciones.Application.Utils
{
    public class Fecha
    {
        public static DateTime DevolverDatetime(string fecha)
        {
            if (string.IsNullOrWhiteSpace(fecha))
                return new DateTime(1900, 1, 1);

            try
            {
                // Zona horaria de Ecuador
                TimeZoneInfo tzEcuador = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
                var culturaEc = new CultureInfo("es-EC");

                // Si solo es fecha sin hora(detectamos esto: no tiene ":" ni "T")
                if (!fecha.Contains(":") && !fecha.Contains("T"))
                {
                    if (DateTime.TryParseExact(fecha, "dd/MM/yyyy", culturaEc, DateTimeStyles.None, out var fechaSolo))
                    {
                        // Mantener tal cual sin alterar hora
                        return DateTime.SpecifyKind(fechaSolo, DateTimeKind.Unspecified);
                    }
                }
                // Si viene con formato ISO u offset(ej: 2025 - 10 - 28T10: 00:00Z o - 05:00)
                if (DateTimeOffset.TryParse(fecha, out var dto))
                {
                    var fechaEc = TimeZoneInfo.ConvertTime(dto, tzEcuador).DateTime;
                    return fechaEc;
                }
                // Si tiene hora pero sin offset explícito
                if (DateTime.TryParse(fecha, culturaEc, DateTimeStyles.AssumeLocal, out var fechaConHora))
                {
                    var fechaEc = TimeZoneInfo.ConvertTime(fechaConHora, tzEcuador);
                    return fechaEc;
                }
            } 
            catch { }
            // fallback seguro
            return new DateTime(1900, 1, 1);
        }

    }
}
