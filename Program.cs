using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using ApiTest.GOshopAPI;

namespace ApiTest
{

    class Program
    {
        static void Main(string[] args)
        {

            var service = new GOshopAPISoapClient();

            //service.Endpoint.Address = new EndpointAddress("http://localhost:64019/Api.asmx");
            using (new OperationContextScope(service.InnerChannel))
            {

                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = new HttpRequestMessageProperty
                {
                    Headers =
                    {
                        { "X-GOSHOP-API-TOKEN", "768511d9c5604c2f893c28250e3b3001" }

                    }
                };



                ProductsListing(service);
                OptionsListing(service);
                Categories(service);
                Orders(service);
                Producers(service);
                Dictionaries(service);

                var producers = service.ProducersList();
                var dictionaries = service.FeaturesAndDictionariesList();

                for (var i = 0; i < 10; i++)
                {
                    AddSimpleProduct(service, producers);
                }
                for (var i = 0; i < 10; i++)
                {
                    AddProductWithVariants(service, producers, dictionaries);
                }


                StockAndPriceUpdating(service);


                Console.ReadKey();







            }

        }

        private static void StockAndPriceUpdating(GOshopAPISoapClient service)
        {

            int pageSize = 100;
            int currentPage = 1;
            int fetchedProducts;
            var allItems = new List<ProductOption>();

            do
            {
                var options = service.OptionsList(new OptionQuery
                {

                    Page = currentPage,
                    PageSize = pageSize

                });

                fetchedProducts = options.Length;
                allItems.AddRange(options);
                currentPage++;

            } while (fetchedProducts >= pageSize);


            var testUpdateById = allItems.Where(x => x.OptionId % 3 == 0).Select(x => x.OptionId);
            var testUpdateByEAN = allItems.Where(x => x.OptionId % 3 == 1).Select(x => x.EAN);
            var testUpdateBySKU = allItems.Where(x => x.OptionId % 3 == 2).Select(x => x.SKU);

            var request = new List<OptionUpdateRequest>();


            var random = new Random();

            foreach (var i in testUpdateById)
            {
                request.Add(new OptionUpdateRequest
                {
                    UpdateById = i,
                    CatalogPriceGross = random.NextDecimal(100, 200),
                    Stock = random.Next(0, 200),
                    PriceGross = random.NextDecimal(100, 200)
                });
            }

            foreach (var i in testUpdateByEAN)
            {
                request.Add(new OptionUpdateRequest
                {
                    UpdateByEAN = i,
                    CatalogPriceGross = random.NextDecimal(100, 200),
                    Stock = random.Next(0, 200),
                    PriceGross = random.NextDecimal(100, 200)
                });
            }

            foreach (var i in testUpdateBySKU)
            {
                request.Add(new OptionUpdateRequest
                {
                    UpdateBySKU = i,
                    CatalogPriceGross = random.NextDecimal(100, 200),
                    Stock = random.Next(0, 200),
                    PriceGross = random.NextDecimal(100, 200)
                });
            }

            var batchSize = 50;
            var rowsAffected = 0;
            Console.WriteLine($"Large update started, parts by {batchSize} in batch");

            for (var i = 0; i < request.Count; i += batchSize)
            {
                var items = request.Skip(i).Take(batchSize); 
                rowsAffected += service.OptionsStockAndPriceUpdate(items.ToArray());
                Console.WriteLine($"Batch part done, items processed: {i}");
            }

           
            Console.WriteLine($"Large update ended, rows affected {rowsAffected}");
        }
        private static void OptionsListing(GOshopAPISoapClient service)
        {
            int pageSize = 5;
            int currentPage = 1;
            int fetchedProducts;
            var definitions = service.FeaturesAndDictionariesList();
            do
            {
                var options = service.OptionsList(new OptionQuery
                {

                    Page = currentPage,
                    PageSize = pageSize,
                    IncludeOptionDictionaries = true

                });
                fetchedProducts = options.Length;

                foreach (var option in options)
                {
                    Console.WriteLine($"Option Listing: {option.OptionId}");

                    if (option.Dictionaries != null)
                        foreach (var dictionary in option.Dictionaries)
                        {
                            Console.WriteLine($"Dictionary value: {dictionary.DictionaryValue}, feature is: {definitions.Single(x => x.FeatureId == dictionary.FeatureId).FeatureName}");
                        }
                }

                currentPage++;

            } while (fetchedProducts >= pageSize);




        }
        private static void ProductsListing(GOshopAPISoapClient service)
        {
            int pageSize = 5;
            int currentPage = 1;
            int fetchedProducts;

            do
            {
                var products = service.ProductsList(new ProductQuery
                {
                    Active = true,
                    IncludeProductImages = true,
                    IncludeProductOptions = true,
                    //CategoryId = 21,
                    Page = currentPage,
                    PageSize = pageSize

                });
                fetchedProducts = products.Length;

                foreach (var product in products)
                {
                    Console.WriteLine($"Product: {product.Name}");

                    if (product.OptionsList != null && product.OptionsList.Length > 0)
                    {
                        foreach (var productOption in product.OptionsList)
                        {
                            var fullname= string.Join(", ", productOption.Dictionaries.Select(x=>$"{x.FeatureName}:{x.DictionaryValue}"));
                            Console.WriteLine($"\tOption: {fullname}");
                            Console.WriteLine($"\t\tEAN: {productOption.EAN}");
                            Console.WriteLine($"\t\tEAN: {productOption.EAN}");
                            Console.WriteLine($"\t\tSKU: {productOption.SKU}");
                            Console.WriteLine($"\t\tStock: {productOption.Stock}");
                        }
                    }

                    if (product.ImagesList != null && product.ImagesList.Length > 0)
                    {

                    }
                }

                currentPage++;

            } while (fetchedProducts >= pageSize);




        }
        private static void AddProductWithVariants(GOshopAPISoapClient service, ProductProducer[] producers, ProductFeature[] dictionaries)
        {
            var resultId = service.ProductAdd(new ProductAddStruct
            {
                Active = true,
                BaseEAN = Guid.NewGuid().ToString().Substring(0, 13),
                BaseSKU = Guid.NewGuid().ToString().Substring(0, 13),
                ProducerId = producers.OrderBy(n => Guid.NewGuid()).First().ProducerId,
                TaxRate = 23,
                Name = $"Test Product {Utilities.GetRandomString(5)}",
                Weight = 0.05m,
                FullHtmlDescription = "Full description <b>with html</b>"
            });


            service.AddVariantOption(new OptionAddRequest
            {
                CatalogPriceGross = new Random().NextDecimal(1000, 5000),
                PriceGross = new Random().NextDecimal(1000, 5000),
                EAN = Utilities.GetRandomString(13),
                SKU = Utilities.GetRandomString(8),
                ProductId = resultId,
                Stock = new Random().Next(0, 100)


            }, new ArrayOfInt
            {
                1,3,11
            });
            service.AddVariantOption(new OptionAddRequest
            {
                CatalogPriceGross = new Random().NextDecimal(1000, 5000),
                PriceGross = new Random().NextDecimal(1000, 5000),
                EAN = Utilities.GetRandomString(13),
                SKU = Utilities.GetRandomString(8),
                ProductId = resultId,
                Stock = new Random().Next(0, 100)


            }, new ArrayOfInt
            {
                1,3,12
            });

            try
            {
                service.AddBasicOption(new OptionAddRequest
                {
                    CatalogPriceGross = new Random().NextDecimal(1000, 5000),
                    PriceGross = new Random().NextDecimal(1000, 5000),
                    EAN = Utilities.GetRandomString(13),
                    SKU = Utilities.GetRandomString(8),
                    ProductId = resultId,
                    Stock = new Random().Next(0, 100)


                });
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            AddImagesToProduct(service, resultId);
        }
        private static void Dictionaries(GOshopAPISoapClient service)
        {
            var featureId = service.FeatureAdd($"Kolor {Utilities.GetRandomString(5)}");


            service.DictionaryAdd(featureId, "Zielony");
            service.DictionaryAdd(featureId, "Czerwony");
            service.DictionaryAdd(featureId, "Różowy");
            service.DictionaryAdd(featureId, "Biały");
            service.DictionaryAdd(featureId, "Czarny");



            featureId = service.FeatureAdd($"Rozmiar {Utilities.GetRandomString(5)}");


            service.DictionaryAdd(featureId, "S");
            service.DictionaryAdd(featureId, "L");
            service.DictionaryAdd(featureId, "XL");
            service.DictionaryAdd(featureId, "XXL");
            service.DictionaryAdd(featureId, "XXXL");


            foreach (var productFeature in service.FeaturesAndDictionariesList())
            {
                foreach (var productFeatureDictionary in productFeature.Dictionaries)
                {
                    Console.WriteLine($"{productFeature.FeatureName} -> {productFeatureDictionary.DictionaryValue}");
                }
            }
        }
        private static void Producers(GOshopAPISoapClient service)
        {
            var producers = service.ProducersList();
            foreach (var producer in producers)
            {
                Console.WriteLine($"Producer name:{producer.ProducerName}, #{producer.ProducerId}");
            }

            Console.WriteLine("Adding new producer");

            var newListOfProducers = service.ProducerAdd($"Producer {Guid.NewGuid()}");
            foreach (var producer in newListOfProducers.Where(x => !producers.Select(y => y.ProducerId).Contains(x.ProducerId)))
            {
                Console.WriteLine($"New producer added: {producer.ProducerName}, #{producer.ProducerId}");
            }
        }
        private static void Categories(GOshopAPISoapClient service)
        {

            var categories = service.CategoryTreeList();
            foreach (var category in categories)
            {
                Console.WriteLine($"Category name: {category.ProductCategoryName}, #{category.ProductCategoryId} at parent ID: {category.ParentId}");
            }

            var categoryId = service.CategoryAdd("Test", 10000000); //not existing parent id

            if (categoryId == null)
                Console.WriteLine("Not existing parent id, no action taken, result is null");

            categoryId = service.CategoryAdd($"Root level test {Utilities.GetRandomString(2)}", 0); // adding to root category

            Console.WriteLine($"New category is added with ID: {categoryId}");

            categoryId = service.CategoryAdd($"Category {Utilities.GetRandomString(2)} ", categories.First(x => x.ProductCategoryId != 0).ProductCategoryId);  // adding to other category category

            Console.WriteLine($"New category is added with ID: {categoryId}");
        }
        private static void AddSimpleProduct(GOshopAPISoapClient service, ProductProducer[] producers)
        {


            var resultId = service.ProductAdd(new ProductAddStruct
            {
                Active = true,
                BaseEAN = Guid.NewGuid().ToString().Substring(0, 13),
                BaseSKU = Guid.NewGuid().ToString().Substring(0, 13),
                ProducerId = producers.OrderBy(n => Guid.NewGuid()).First().ProducerId,
                TaxRate = 23,
                Name = $"Test Product {Utilities.GetRandomString(5)}",
                Weight = 0.05m,
                FullHtmlDescription = "Full description <b>with html</b>",
                ShortDescription = "test short descriptions",
                CategoryId = new ArrayOfInt { 26, 18 }
            });
            var catalogPrice = new Random(new Random().Next()).NextDecimal(1000, 5000);
            var grossPrice = new Random(new Random().Next()).NextDecimal(1000, 5000);

            service.AddBasicOption(new OptionAddRequest
            {
                CatalogPriceGross = catalogPrice,
                PriceGross = grossPrice,
                EAN = Utilities.GetRandomString(13),
                SKU = Utilities.GetRandomString(8),
                ProductId = resultId,
                Stock = new Random().Next(0, 100)


            });

            Console.WriteLine($"Product added: #{resultId}");

            AddImagesToProduct(service, resultId);

        }
        private static void AddImagesToProduct(GOshopAPISoapClient service, int productId)
        {
            using (var wc = new WebClient())
            {
                var addedImages = new List<ProductImage>();

                for (var i = 0; i < 3; i++)
                {
                    var bytearray = wc.DownloadData("https://picsum.photos/500/600");
                    addedImages.Add(service.ImageAdd(productId, bytearray, i % 2 == 0 ? "customhash" : ""));
                }

                foreach (var image in addedImages)
                {
                    Console.WriteLine($"New image URL: {image.ImageUrl} with hash: {image.SourceImageHash}");
                }
            }
        }

        private static void Orders(GOshopAPISoapClient service)
        {

            var availableStates = service.OrdersStatesList();
            Console.WriteLine($"Available order states count is: {availableStates.Length}");

            var orders = service.OrdersList(new OrderQuery
            {
                //CreatedLaterThan = DateTime.Now.AddYears(-50),
                Page = 1,
                PageSize = 10,
                InStateId = new ArrayOfInt
                {
                    1,2,2,2,2,2
                }

            });

            foreach (var order in orders)
            {
                Console.WriteLine(order.Email);

                service.OrderUpdate(new OrderUpdateQuery
                {
                    NewOuterSystemID = Guid.NewGuid().ToString(),
                    OrderId = order.OrderId,
                    NewShippingNumber = "shipping_number",
                    NewStateId = 2121

                });
            }
        }
    }
}
