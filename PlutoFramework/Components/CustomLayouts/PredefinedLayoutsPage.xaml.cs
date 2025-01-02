﻿namespace PlutoFramework.Components.CustomLayouts;

public partial class PredefinedLayoutsPage : ContentPage
{
	public PredefinedLayoutsPage()
	{
        NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetNavBarIsVisible(this, false);

        InitializeComponent();
	}
}