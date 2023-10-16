using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACSSolutions.CDRRelay
{
	internal class Settings
	{
		[Required]
		public UInt16 Port { get; set; }

		[Required]
		public String? ListName { get; set; }
	}
}
