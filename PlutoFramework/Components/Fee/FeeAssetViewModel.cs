﻿using CommunityToolkit.Mvvm.ComponentModel;

namespace PlutoFramework.Components.Fee
{
	public partial class FeeAssetViewModel : ObservableObject
	{
		[ObservableProperty]
		private string asset;

		public FeeAssetViewModel()
		{
			asset = "Loading";
		}
	}
}

