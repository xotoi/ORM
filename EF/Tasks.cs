using System;
using System.Linq;
using EF.EFModel;

namespace EF
{
    class Tasks
    {
        static void Main(string[] args)
        {
            using (var db = new NorthwindContext())
            {
                var selectedCategoryId = 3;
                var orders = db.Orders
                    .Where(o => o.OrderDetails.Any(od => od.Product.Category.CategoryId == selectedCategoryId))
                    .Select(o => new
                    {
                        o.Customer.ContactName,
                        Order_Details = o.OrderDetails.Select(od => new
                        {
                            od.Product.ProductName,
                            od.OrderId,
                            od.Discount,
                            od.Quantity,
                            od.UnitPrice,
                            od.ProductId
                        })
                    });

                foreach (var order in orders)
                {
                    Console.WriteLine($"Customer: {order.ContactName} Products: {string.Join(", ", order.Order_Details.Select(od => od.ProductName))}");
                }

                Console.ReadKey();
            }
        }
    }
}
