using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Teh.BHUD.Blacklist_Buddy_Module.Models
{
    public class Categories
    {
        internal Categories CategoriesInstance;
        public struct Category
        {
            public int Count { get; set; }

            public void Inc()
            {
                Count++;
                Categories.All.Count++;
            }

            public void Zero()
            {
                Categories.All.Count -= Count;
                Count = 0;
            }

            internal Categories Categories { get; set; }
        }

        public Category Scam;
        public Category RMT;
        public Category GW2E;
        public Category Other;
        public Category Unknown;
        public Category All;

        public Categories()
        {
            CategoriesInstance = this;

            Scam = new Category() { Count = 0, Categories = this };
            RMT = new Category() { Count = 0, Categories = this };
            GW2E = new Category() { Count = 0, Categories = this };
            Other = new Category() { Count = 0, Categories = this };
            Unknown = new Category() { Count = 0, Categories = this };
            All = new Category() { Count = 0, Categories = this };
        }

    }
}
