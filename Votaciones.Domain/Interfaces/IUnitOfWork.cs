namespace Votaciones.Domain.Interfaces
{
    public interface IUnitOfWork
    {
        // Para guardar los cambios en la base de datos
        Task<int> SaveChangesAsync();
        // Para manejar transacciones de manera explícita
        Task BeginTransactionAsync();
        // Para confirmar la transacción
        Task CommitTransactionAsync();
        // Para revertir la transacción en caso de error
        Task RollbackTransactionAsync();
    }
}
