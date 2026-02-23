using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    public class MutanterProductComponent : KMonoBehaviour
    {
        [System.Serializable]
        public struct Product
        {
            public Tag Id;
            public float BaseAmount;
            public float SuccessRateMultiplier;

            public Product(Tag id, float baseAmount, float successRateMultiplier)
            {
                Id = id;
                BaseAmount = baseAmount;
                SuccessRateMultiplier = successRateMultiplier;
            }
        }

        [SerializeField]
        public List<Product> Products = new List<Product>();

        // 静态产品数据库
        private static Dictionary<string, List<Product>> _mutanterProductsDatabase = new Dictionary<string, List<Product>>();

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            
            // 尝试从数据库加载产品
            string mutanterId = gameObject.GetComponent<KPrefabID>().PrefabID().Name;
            if (_mutanterProductsDatabase.ContainsKey(mutanterId))
            {
                Products = new List<Product>(_mutanterProductsDatabase[mutanterId]);
            }
        }

        // 添加产出物到数据库
        public static void AddProductToDatabase(string mutanterId, Product product)
        {
            if (!_mutanterProductsDatabase.ContainsKey(mutanterId))
            {
                _mutanterProductsDatabase[mutanterId] = new List<Product>();
            }
            _mutanterProductsDatabase[mutanterId].Add(product);
        }

        // 添加产出物
        public void AddProduct(Product product)
        {
            Products.Add(product);
        }

        // 获取所有产出物
        public List<Product> GetProducts()
        {
            return Products;
        }

        // 一次性生成产出物
        public List<GeneratedProduct> GenerateProducts(float successRate, int totalSubtasks)
        {
            List<GeneratedProduct> generatedProducts = new List<GeneratedProduct>();

            foreach (var product in Products)
            {
                // 计算产出物数量
                float amount = product.BaseAmount * totalSubtasks * successRate * product.SuccessRateMultiplier;
                int actualAmount = Mathf.Max(1, Mathf.FloorToInt(amount));

                generatedProducts.Add(new GeneratedProduct(product.Id, actualAmount));
            }

            return generatedProducts;
        }

        // 生成的产出物结构
        public struct GeneratedProduct
        {
            public Tag Id;
            public int Amount;

            public GeneratedProduct(Tag id, int amount)
            {
                Id = id;
                Amount = amount;
            }
        }
    }
}