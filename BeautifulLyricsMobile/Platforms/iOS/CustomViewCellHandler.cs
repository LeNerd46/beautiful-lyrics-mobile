using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;

namespace BeautifulLyricsMobile.Platforms.iOS
{
	// In case I want to target iOS in the future, don't have to worry about this
	// Truthfully, I do intend on bringing this to iOS, it's just not my priority right now
    class CustomViewCellHandler : ViewCellRenderer
    {
		public override UITableViewCell GetCell(Cell item, UITableViewCell reusableCell, UITableView tv)
		{
			var cell = base.GetCell(item, reusableCell, tv);

			cell.SelectedBackgroundView = new UIView
			{
				BackgroundColor = ((CustomViewCell)item).SelectedBackgroundColor.ToPlatform()
			};

			return cell;
		}
	}
}
