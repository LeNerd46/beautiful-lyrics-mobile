using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobileV2.PageModels
{
	public record OnboardingItem(string Title, string Description, string BottomText, string ButtonText, int TitlePosition, bool secondPage);

	public partial class OnboardingModel : ObservableObject
    {
		public ObservableCollection<OnboardingItem> Items { get; set; } = [];

		[ObservableProperty]
		public partial RadialGradientBrush Background { get; set; }

		public OnboardingModel()
		{
			Items.Add(new OnboardingItem("Beautiful Lyrics", "Revolutionize your lyrics with beautiful lyrics", "Get started to experience lyrics like never before", "Get Started", 2, false));
			Items.Add(new OnboardingItem("Beautiful Lyrics", "Beautiful Lyrics needs access to Spotify in order to function", "Connect with Spotify to continue", "Connect With Spotify", 2, true));
		}
	}
}
