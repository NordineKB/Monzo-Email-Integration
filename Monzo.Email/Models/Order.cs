using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Monzo.Email.Models
{
	public class Order
	{
		public string Name { get; set; }
		public string Image { get; set; }
		public DateTime OrderDate { get; set; }
		public int Price { get; set; }
	}
}
