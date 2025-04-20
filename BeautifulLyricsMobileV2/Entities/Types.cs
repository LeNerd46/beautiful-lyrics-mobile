using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobileV2.Entities
{
	interface ISyncedVocals
	{
		public void Animate(double songTimestamp, double deltaTime, bool isImmediate = true);
		public bool IsActive();
	}

	internal enum LyricState
	{
		Idle,
		Active,
		Sung
	}

	internal struct Springs
	{
		public Spring Scale { get; set; }
		public Spring YOffset { get; set; }
		public Spring Glow { get; set; }
	}

	internal struct LiveText
	{
		public object Object { get; set; }
		public Springs Springs { get; set; }
	}
}
