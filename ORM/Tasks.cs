using LinqToDB;
using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB.Data;
using ORM.Linq2dbModel;

namespace ORM
{
    class Tasks
    {
        static void Main(string[] args)
        {
            using (var db = new NorthwindDB())
            {
                List_of_products_with_category_and_supplier(db);
            }

            Console.ReadKey();
        }

        public static void List_of_products_with_category_and_supplier(NorthwindDB db)
        {
            foreach (var product in db.Products.LoadWith(p => p.Category).LoadWith(p => p.Supplier).ToList())
            {
                Console.WriteLine($"Product name: {product.ProductName}; Category: {product.Category?.CategoryName}; Supplier: {product.Supplier?.ContactName}");
            }
        }

        public void List_of_employees_with_region(NorthwindDB db)
        {
            var query = (from e in db.Employees
                join et in db.EmployeeTerritories on e.EmployeeID equals et.EmployeeID into el
                from w in el.DefaultIfEmpty()
                join t in db.Territories on w.TerritoryID equals t.TerritoryID into zl
                from z in zl.DefaultIfEmpty()
                join r in db.Regions on z.RegionID equals r.RegionID into kl
                from k in kl.DefaultIfEmpty()
                select new { e.FirstName, e.LastName, Region = k }).Distinct();

            foreach (var record in query.ToList())
            {
                Console.WriteLine($"Employee: {record.FirstName} {record.LastName}; Region: {record.Region?.RegionDescription}");
            }
        }

        public void Count_of_employees_by_regions(NorthwindDB db)
        {
            var query = from r in db.Regions
                join t in db.Territories on r.RegionID equals t.RegionID into kl
                from k in kl.DefaultIfEmpty()
                join et in db.EmployeeTerritories on k.TerritoryID equals et.TerritoryID into zl
                from z in zl.DefaultIfEmpty()
                join e in db.Employees on z.EmployeeID equals e.EmployeeID into dl
                from d in dl.DefaultIfEmpty()
                select new { Region = r, d.EmployeeID };
            var result = from row in query.Distinct()
                group row by row.Region into ger
                select new { RegionDescription = ger.Key.RegionDescription, EmployeesCount = ger.Count(e => e.EmployeeID != 0) };

            foreach (var record in result.ToList())
            {
                Console.WriteLine($"Region: {record.RegionDescription}; Employees count: {record.EmployeesCount}");
            }
        }

        public void Employees_with_Shippers_according_to_Orders(NorthwindDB db)
        {
            var query = (from e in db.Employees
                join o in db.Orders on e.EmployeeID equals o.EmployeeID into el
                from w in el.DefaultIfEmpty()
                join s in db.Shippers on w.Shipper.ShipperID equals s.ShipperID into zl
                from z in zl.DefaultIfEmpty()
                select new { e.EmployeeID, e.FirstName, e.LastName, z.CompanyName }).Distinct().OrderBy(t => t.EmployeeID);

            foreach (var record in query.ToList())
            {
                Console.WriteLine($"Employee: {record.FirstName} {record.LastName} Shipper: {record.CompanyName}");
            }
        }

        public void Add_new_Employee_with_Territories(NorthwindDB db)
        {
            var newEmployee = new Employee { FirstName = "Petr", LastName = "Petrov" };
            try
            {
                db.BeginTransaction();
                newEmployee.EmployeeID = Convert.ToInt32(db.InsertWithIdentity(newEmployee));
                db.Territories.Where(t => t.TerritoryDescription.Length <= 5)
                    .Insert(db.EmployeeTerritories, t => new EmployeeTerritory { EmployeeID = newEmployee.EmployeeID, TerritoryID = t.TerritoryID });
                db.CommitTransaction();
            }
            catch
            {
                db.RollbackTransaction();
            }
        }

        public void Move_Products_to_another_Category(NorthwindDB db)
        {
            var updatedCount = db.Products.Update(p => p.CategoryID == 2, pr => new Product
            {
                CategoryID = 1
            });

            Console.WriteLine(updatedCount);
        }

        public void Insert_list_of_Products_with_Suppliers_and_Categories(NorthwindDB db)
        {
            var products = new List<Product>
            {
                new Product
                {
                    ProductName = "Car",
                    Category = new Category {CategoryName = "Vehicles"},
                    Supplier = new Supplier {CompanyName = "Stark industries"}
                },
                new Product
                {
                    ProductName = "Reactive car",
                    Category = new Category {CategoryName = "Vehicles"},
                    Supplier = new Supplier {CompanyName = "Stark industries"}
                }
            };

            try
            {
                db.BeginTransaction();
                //pass ids to products list
                foreach (var product in products)
                {
                    var category = db.Categories.FirstOrDefault(c => c.CategoryName == product.Category.CategoryName);
                    product.CategoryID = category?.CategoryID ?? Convert.ToInt32(db.InsertWithIdentity(
                                             new Category
                                             {
                                                 CategoryName = product.Category.CategoryName
                                             }));
                    var supplier = db.Suppliers.FirstOrDefault(s => s.CompanyName == product.Supplier.CompanyName);
                    product.SupplierID = supplier?.SupplierID ?? Convert.ToInt32(db.InsertWithIdentity(
                                             new Supplier
                                             {
                                                 CompanyName = product.Supplier.CompanyName
                                             }));
                }

                db.BulkCopy(products);
                db.CommitTransaction();
            }
            catch
            {
                db.RollbackTransaction();
            }
        }

        public void Replace_Product_with_the_same_in_NotShipped_Orders_plenty_of_queries(NorthwindDB db)
        {
            var orderDetails = db.OrderDetails.LoadWith(od => od.Order)
                .Where(od => od.Order.ShippedDate == null).ToList();
            foreach (var orderDetail in orderDetails)
            {
                db.OrderDetails.LoadWith(od => od.Product).Update(od => od.OrderID == orderDetail.OrderID && od.ProductID == orderDetail.ProductID,
                    od => new OrderDetail
                    {
                        ProductID = db.Products.First(p => !db.OrderDetails.Where(t => t.OrderID == od.OrderID)
                                .Any(t => t.ProductID == p.ProductID) && p.CategoryID == od.Product.CategoryID).ProductID
                    });
            }
        }

        public void Replace_Product_with_the_same_in_NotShippedOrders_one_query(NorthwindDB db)
        {
            var updatedRows = db.OrderDetails.LoadWith(od => od.Order).LoadWith(od => od.Product)
                .Where(od => od.Order.ShippedDate == null).Update(
                    od => new OrderDetail
                    {
                        ProductID = db.Products.First(p => p.CategoryID == od.Product.CategoryID && p.ProductID > od.ProductID) != null
                            ? db.Products.First(p => p.CategoryID == od.Product.CategoryID && p.ProductID > od.ProductID).ProductID
                            : db.Products.First(p => p.CategoryID == od.Product.CategoryID).ProductID
                    });
            Console.WriteLine($"{updatedRows} rows updated");
        }
    }
}
