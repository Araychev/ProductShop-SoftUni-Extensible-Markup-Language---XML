using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;
using ProductShop.Data;
using ProductShop.Dtos.Export;
using ProductShop.Dtos.Import;
using ProductShop.Models;

namespace ProductShop
{
    public class StartUp
    {
        public static void Main(string[] args)
        {
            ProductShopContext context = new ProductShopContext();

           //  DbReset(context);

            //var inputXml = File.ReadAllText("Datasets/categories-products.xml");

            Console.WriteLine(GetCategoriesByProductsCount(context));

        }

        private static void DbReset(ProductShopContext context)
        {

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            Console.WriteLine("Db Reset Succeed!");
        }

         // 01. Import Users
        public static string ImportUsers(ProductShopContext context, string inputXml)
        {
            var serializer = new XmlSerializer(typeof(UserImportDto[]), new XmlRootAttribute("Users"));
            var deserialUsers = (ICollection<UserImportDto>)serializer.Deserialize(new StringReader(inputXml));

            var users = deserialUsers
                .Select(u => new User()
                {
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Age = u.Age
                }).ToList();

            context.Users.AddRange(users);
            context.SaveChanges();

            return $"Successfully imported {users.Count}";
        }

        //02. Import Products
        public static string ImportProducts(ProductShopContext context, string inputXml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ProductImportDto[]), new XmlRootAttribute("Products"));

            var reader = new StringReader(inputXml);

            var productsDtos = (ProductImportDto[])serializer.Deserialize(reader);

            var products = productsDtos
                .Select(x => new Product
                {
                    Name = x.Name,
                    Price = x.Price,
                    SellerId = x.SellerId,
                    BuyerId = x.BuyerId
                })
                .ToList();

            context.Products.AddRange(products);

            var count = context.SaveChanges();

            return $"Successfully imported {count}";
        }

        //03. Import Categories
        public static string ImportCategories(ProductShopContext context, string inputXml)
        {

            XmlSerializer serializer = new XmlSerializer(typeof(CategoryImportDto[]), new XmlRootAttribute("Categories"));

            var reader = new StringReader(inputXml);

            var categoryDtos = (CategoryImportDto[])serializer.Deserialize(reader);

            var categories = categoryDtos
                .Select(x => new Category
                {
                    
                    Name = x.Name
                })
                .ToList();

            foreach (Category category in categories)
            {
                if (category.Name != null)
                {
                    context.Categories.Add(category);
                }
            }

            var count = context.SaveChanges();

            return $"Successfully imported {count}";
        }

       // 04. Import Categories and Products
       public static string ImportCategoryProducts(ProductShopContext context, string inputXml)
       {
           XmlSerializer serializer = new XmlSerializer(typeof(CategoryProductImportDto[]), new XmlRootAttribute("CategoryProducts"));

           var reader = new StringReader(inputXml);

           var categoryProductDtos = (CategoryProductImportDto[])serializer.Deserialize(reader);

           var categoryProducts = categoryProductDtos
               .Select(x => new CategoryProduct
               {
                   CategoryId = x.CategoryId,
                   ProductId = x.ProductId
               })
               .ToList();

           foreach (var categoryProduct in categoryProducts)
           {
               bool IdsExist = context.Categories.Any(x => x.Id == categoryProduct.CategoryId) &&
                               context.Products.Any(x => x.Id == categoryProduct.ProductId);

               if (IdsExist)
               {
                   context.CategoryProducts.Add(categoryProduct);
               }
           }

           var count = context.SaveChanges();

           return $"Successfully imported {count}";
       }

      // 05. Export Products In Range
      public static string GetProductsInRange(ProductShopContext context)
      {
          var productsInfo = context.Products
              .Where(p => p.Price >= 500 && p.Price <= 1000)
              .Select(p => new ProductExportDto()
              {
                  Name = p.Name,
                  Price = p.Price,
                  BuyerFullName = string.Join(" ", p.Buyer.FirstName, p.Buyer.LastName)
              })
              .OrderBy(s => s.Price)
              .Take(10)
              .ToList();

          var serializerXml = new XmlSerializer(typeof(List<ProductExportDto>), 
              new XmlRootAttribute("Products"));
          var xmlResult = new StringWriter();
          var nameSpaces = new XmlSerializerNamespaces();
          nameSpaces.Add("", "");
          serializerXml.Serialize(xmlResult, productsInfo, nameSpaces);

          return xmlResult.ToString().Trim();

      }

       //  06. Export Sold Products
       public static string GetSoldProducts(ProductShopContext context)
       {
           var usersInfo = context.Users
               .Where(u => u.ProductsSold.Count > 0)
               .Select(u => new UserExportDto()
               {
                   FirstName = u.FirstName,
                   LastName = u.LastName,
                   ProductsSold = u.ProductsSold.Select(p => new ProductExportDto()
                   {
                       Name = p.Name,
                       Price = p.Price
                   }).ToList()
               })
               .OrderBy(s => s.LastName)
               .ThenBy(s => s.FirstName)
               .Take(5)
               .ToList();

           var serializerXml = new XmlSerializer(typeof(List<UserExportDto>),
               new XmlRootAttribute("Users"));
           var xmlResult = new StringWriter();
           var nameSpaces = new XmlSerializerNamespaces();
           nameSpaces.Add("", "");
           serializerXml.Serialize(xmlResult, usersInfo, nameSpaces);

           return xmlResult.ToString().Trim();
       }

      // 07. Export Categories By Products Count

      public static string GetCategoriesByProductsCount(ProductShopContext context)
      {
          var categoryInfo = context.Categories
              .Select(c => new CategoryExportDto()
              {
                  Name = c.Name,
                  Count = c.CategoryProducts.Count,
                  AveragePrice = c.CategoryProducts.Average(x => x.Product.Price),
                  TotalRevenue = c.CategoryProducts.Sum(x => x.Product.Price)
              })
              .OrderByDescending(s => s.Count)
              .ThenBy(s => s.TotalRevenue)
              .ToList();

          var serializerXml = new XmlSerializer(typeof(List<CategoryExportDto>),
              new XmlRootAttribute("Categories"));
          var xmlResult = new StringWriter();
          var nameSpaces = new XmlSerializerNamespaces();
          nameSpaces.Add("", "");
          serializerXml.Serialize(xmlResult, categoryInfo, nameSpaces);

          return xmlResult.ToString().Trim();
      }

      //08. Export Users and Products

      public static string GetUsersWithProducts(ProductShopContext context)
      {
          var userProductsInfo = context.Users
              .Include(x => x.ProductsSold)
              .ToList()
              .Where(u => u.ProductsSold.Count >= 1)
              .Select(u => new UserProductExportDto()
              {
                  FirstName = u.FirstName,
                  LastName = u.LastName,
                  Age = u.Age,
                  ProductsSold = new SoldProductDto()
                  {
                      Count = u.ProductsSold.Count,
                      Products = u.ProductsSold.Select(x => new ProductExportDto
                          {
                              Name = x.Name,
                              Price = x.Price
                          })
                          .OrderByDescending(p => p.Price)
                          .ToList()
                  }
              })
              .OrderByDescending(s => s.ProductsSold.Count)
              .Take(10)
              .ToList();

          var finalResult = new UserStartExportDto()
          {
              CountUsers = context.Users.Count(x => x.ProductsSold.Count >= 1),
              Users = userProductsInfo.ToList()
          };

          var serializerXml = new XmlSerializer(typeof(UserStartExportDto),
              new XmlRootAttribute("Users"));
          var xmlResult = new StringWriter();
          var nameSpaces = new XmlSerializerNamespaces();
          nameSpaces.Add("", "");
          serializerXml.Serialize(xmlResult, finalResult, nameSpaces);

          return xmlResult.ToString().Trim();
      }

    }
}