
namespace Votaciones.Application.DTOs.PaginacionDTO
{
    public class PaginacionDTO<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int PageActual { get; set; }
        public int PageSize { get; set; }
        public int TotalRegistros { get; set; }
        public int TotalPages { get; set; }
    }
}
