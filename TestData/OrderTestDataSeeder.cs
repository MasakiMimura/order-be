using OrderBE.Models;
using OrderBE.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net.Sockets;

namespace OrderBE.TestData
{
    public class OrderTestDataSeeder
    {
        private readonly OrderDbContext _context;
        private readonly ILogger<OrderTestDataSeeder> _logger;

        public OrderTestDataSeeder(OrderDbContext context, ILogger<OrderTestDataSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Order> SeedSingleOrderAsync(string? memberCardNo = null, decimal total = 1000.00m, string status = "IN_ORDER")
        {
            var order = new Order
            {
                CreatedAt = DateTime.UtcNow,
                MemberCardNo = memberCardNo,
                Total = total,
                Status = status,
                Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ProductId = 1,
                        ProductName = "カフェラテ",
                        ProductPrice = 450.00m,
                        ProductDiscountPercent = 0.00m,
                        Quantity = 2
                    },
                    new OrderItem
                    {
                        ProductId = 2,
                        ProductName = "クロワッサン",
                        ProductPrice = 300.00m,
                        ProductDiscountPercent = 10.00m,
                        Quantity = 1
                    }
                }
            };

            _context.Orders.Add(order);

            try
            {
                await _context.SaveChangesAsync();
                return order;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Insert failed due to update issue.");
                throw new InvalidOperationException("Insert failed due to database constraint", ex);
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "PostgreSQL connection error during insert.");
                throw;
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Network connection error during insert.");
                throw;
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Database connection timeout during insert.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during insert.");
                throw new Exception($"Repository error during insert: {ex.Message}", ex);
            }
        }

        public async Task<List<Order>> SeedTestOrdersAsync(int count = 3)
        {
            var orders = new List<Order>();

            try
            {
                for (int i = 0; i < count; i++)
                {
                    string? memberCardNo = i % 2 == 0 ? null : $"MEMBER{i:D5}";
                    decimal total = 1000.00m + (i * 100.00m);

                    var order = new Order
                    {
                        CreatedAt = DateTime.UtcNow,
                        MemberCardNo = memberCardNo,
                        Total = total,
                        Status = "IN_ORDER",
                        Items = new List<OrderItem>
                        {
                            new OrderItem
                            {
                                ProductId = 1,
                                ProductName = "カフェラテ",
                                ProductPrice = 450.00m,
                                ProductDiscountPercent = 0.00m,
                                Quantity = 2
                            },
                            new OrderItem
                            {
                                ProductId = 2,
                                ProductName = "クロワッサン",
                                ProductPrice = 300.00m,
                                ProductDiscountPercent = 10.00m,
                                Quantity = 1
                            }
                        }
                    };

                    _context.Orders.Add(order);
                    orders.Add(order);
                }

                await _context.SaveChangesAsync();
                return orders;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Insert failed due to update issue.");
                throw new InvalidOperationException("Insert failed due to database constraint", ex);
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "PostgreSQL connection error during insert.");
                throw;
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Network connection error during insert.");
                throw;
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Database connection timeout during insert.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during insert.");
                throw new Exception($"Repository error during insert: {ex.Message}", ex);
            }
        }
    }
}
