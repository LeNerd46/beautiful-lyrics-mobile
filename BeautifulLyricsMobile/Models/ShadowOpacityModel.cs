using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobile.Models
{
	public partial class ShadowOpacityModel : INotifyPropertyChanged
	{
		private double shadowOpacity;

		public double SHadowOpacity
		{
			get => shadowOpacity;
			set
			{
				shadowOpacity = value;
				OnPropertyChanged(nameof(SHadowOpacity));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
