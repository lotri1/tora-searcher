using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToraSearcher.Entities
{
    public class BookTreeNode
    {
        public string Name { get; set; }

        public List<BookTreeNode> Books { get; set; }
    }
}
