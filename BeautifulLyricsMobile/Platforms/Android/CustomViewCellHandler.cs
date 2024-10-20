using Android.Content;
using Android.Graphics.Drawables;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Microsoft.Maui.Controls.Platform;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AView = Android.Views.View;

namespace BeautifulLyricsMobile.Platforms.Android
{
    class CustomViewCellHandler : Microsoft.Maui.Controls.Handlers.Compatibility.ViewCellRenderer
    {
        private AView pCellCore;
        private bool pSelected;
        private Drawable pUnselectedBackground;

		protected override AView GetCellCore(Cell item, AView convertView, global::Android.Views.ViewGroup parent, Context context)
		{
			pCellCore = base.GetCellCore(item, convertView, parent, context);

			pSelected = false;
			pUnselectedBackground = pCellCore.Background;

			return pCellCore;
		}

		protected override void OnCellPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnCellPropertyChanged(sender, e);

			if(e.PropertyName == "IsSelected")
			{
				pSelected = !(pSelected);

				if (pSelected)
					pCellCore.SetBackgroundColor(((CustomViewCell)sender).SelectedBackgroundColor.ToAndroid());
				else
					pCellCore.SetBackground(pUnselectedBackground);
			}
		}
	}
}
