using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobile.Controls
{
	public class TouchScrollView : ScrollView
	{
		public event EventHandler Touch;
		public event EventHandler Release;

		public void OnTouch() => Touch?.Invoke(this, EventArgs.Empty);
		public void OnRelease() => Release?.Invoke(this, EventArgs.Empty);
	}
}
