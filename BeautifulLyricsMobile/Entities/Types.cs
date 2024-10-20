using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsAndroid.Entities
{
	interface IBaseVocals { }

	interface ISyncedVocals : IBaseVocals
	{
		public event EventHandler<bool> ActivityChanged;
		public event EventHandler RequestedTimeSkip;

		public void Animate(double songTimestamp, double deltaTime, bool isImmeiate = true);
		public void SetBlur(double blurDistance);
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
		public double GradientProgress { get; set; }
		public double Scale { get; set; }
		public double YOffset { get; set; }

		public double BlurRadius { get; set; }
		public double ShadowOpacity { get; set; }

        public object Object { get; set; }

        public Springs Springs { get; set; }
	}
}