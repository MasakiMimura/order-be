using OrderBE.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net.Sockets;

namespace OrderBE.TestData
{
    public class OrderTestDataCleaner
    {
        private readonly OrderDbContext _context;
        private readonly ILogger<OrderTestDataCleaner> _logger;

        public OrderTestDataCleaner(OrderDbContext context, ILogger<OrderTestDataCleaner> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task CleanupTestDataAsync()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM order_item");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM \"order\"");

                await transaction.CommitAsync();
                _logger.LogInformation("Test data cleanup completed successfully.");
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Cleanup failed due to update issue.");
                throw new InvalidOperationException("Cleanup failed due to database constraint", ex);
            }
            catch (NpgsqlException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "PostgreSQL connection error during cleanup.");
                throw;
            }
            catch (SocketException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Network connection error during cleanup.");
                throw;
            }
            catch (TimeoutException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database connection timeout during cleanup.");
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Unexpected error during cleanup.");
                throw new Exception($"Repository error during cleanup: {ex.Message}", ex);
            }
        }

        public async Task CleanupOrdersByStatusAsync(string status)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var orderIds = await _context.Orders
                    .Where(o => o.Status == status)
                    .Select(o => o.OrderId)
                    .ToListAsync();

                if (orderIds.Any())
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM order_item WHERE order_id = ANY(@p0)",
                        orderIds.ToArray()
                    );

                    await _context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM \"order\" WHERE status = @p0",
                        status
                    );
                }

                await transaction.CommitAsync();
                _logger.LogInformation($"Orders with status '{status}' cleanup completed successfully.");
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Cleanup failed due to update issue.");
                throw new InvalidOperationException("Cleanup failed due to database constraint", ex);
            }
            catch (NpgsqlException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "PostgreSQL connection error during cleanup.");
                throw;
            }
            catch (SocketException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Network connection error during cleanup.");
                throw;
            }
            catch (TimeoutException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database connection timeout during cleanup.");
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Unexpected error during cleanup.");
                throw new Exception($"Repository error during cleanup: {ex.Message}", ex);
            }
        }
    }
}
