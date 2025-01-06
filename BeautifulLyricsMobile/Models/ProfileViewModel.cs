using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BeautifulLyricsMobile.Models
{
	public class ProfileViewModel : INotifyPropertyChanged
	{
		public ObservableCollection<CollectionMetadata> Items { get; set; } = [];

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class CollectionMetadata
	{
		public string Id { get; set; }
		public string Title { get; set; }
		public string Image { get; set; }
		public string Info { get; set; }
	}
}
