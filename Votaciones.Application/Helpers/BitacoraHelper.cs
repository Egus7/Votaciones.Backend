using System.Text.Json;

namespace Votaciones.Application.Helpers
{
    public static class BitacoraHelper
    {
        public static Dictionary<string, object?> ObtenerValores<T>(T entidad, params string[] propiedades)
        {
            var valores = new Dictionary<string, object?>();

            if (entidad == null)
                return valores;

            var tipo = typeof(T);

            foreach (var nombrePropiedad in propiedades)
            {
                var propiedad = tipo.GetProperty(nombrePropiedad);

                if (propiedad == null)
                    continue;

                var valor = propiedad.GetValue(entidad);
                // Si es Enum, guardar su nombre
                if (valor != null && propiedad.PropertyType.IsEnum)
                {
                    valor = valor.ToString();
                }

                valores[nombrePropiedad] = valor;
            }
            return valores;
        }

        public static (Dictionary<string, object?> Anteriores, Dictionary<string, object?> Nuevos) ObtenerSoloCambios(
            Dictionary<string, object?> anteriores, Dictionary<string, object?> nuevos)
        {
            var cambiosAnteriores = new Dictionary<string, object?>();
            var cambiosNuevos = new Dictionary<string, object?>();

            foreach (var item in nuevos)
            {
                anteriores.TryGetValue(item.Key, out var valorAnterior);

                if (!SonIguales(valorAnterior, item.Value))
                {
                    cambiosAnteriores[item.Key] = valorAnterior;
                    cambiosNuevos[item.Key] = item.Value;
                }
            }
            return (cambiosAnteriores, cambiosNuevos);
        }

        // Reemplaza una FK por su descripción
        public static void AgregarRelacion(Dictionary<string, object?> valores, string campoDescripcion, string? descripcion)
        {
            valores[campoDescripcion] = descripcion;
        }

        #region Metodo privados
        // Se comparan los valores de dos objetos, si son iguales devuelve true, si no devuelve false
        private static bool SonIguales(object? anterior, object? nuevo)
        {
            // Ambos nulos
            if (anterior == null && nuevo == null)
                return true;

            // Uno nulo y otro no
            if (anterior == null || nuevo == null)
                return false;

            var tipo = anterior.GetType();

            // Tipos simples
            if (tipo.IsPrimitive || tipo == typeof(string) || tipo == typeof(decimal) ||
                tipo == typeof(DateTime) || tipo == typeof(Guid))
            {
                return Equals(anterior, nuevo);
            }

            // Objetos complejos
            return JsonSerializer.Serialize(anterior) == JsonSerializer.Serialize(nuevo);
        }
        #endregion
    }
}
