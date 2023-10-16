using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACSSolutions.CDRRelay
{
	internal class CDR
	{
		public String? historyid { get; set; }
		public DateTime? started { get; set; }
		public DateTime? answered { get; set; }
		public DateTime? ended { get; set; }
		public String? end_reason { get; set; }
		public String? from_number { get; set; }
		public String? from_name { get; set; }
		public String? from_dn { get; set; }
		public String? to_number { get; set;}
		public String? to_name { get; set;}
		public String? to_dn { get; set;}
		public String? dial_number { get; set; }
		public String? chain { get; set;}
	}
}
