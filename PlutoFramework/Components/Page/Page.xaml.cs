using MauiView = Microsoft.Maui.Controls.View;
using System.Collections.ObjectModel;

namespace PlutoFramework.Components.Page
{
    [ContentProperty(nameof(Content))]
    public partial class Page : ContentPage
    {
        public Page()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            Shell.SetNavBarIsVisible(this, false);

            InitializeComponent();

            Popups = new ObservableCollection<MauiView>();
            Popups.CollectionChanged += Popups_CollectionChanged;
        }

        private void Popups_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (MauiView popup in e.NewItems)
                {
                    popupsContainer.Children.Add(popup);
                }
            }

            if (e.OldItems != null)
            {
                foreach (MauiView popup in e.OldItems)
                {
                    popupsContainer.Children.Remove(popup);
                }
            }
        }

        new public static readonly BindableProperty ContentProperty = BindableProperty.Create(
            nameof(Content),
            typeof(MauiView),
            typeof(Page),
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var control = (Page)bindable;
                control.mainContentPresenter.Content = (MauiView)newValue;
            });

        public new MauiView Content
        {
            get => (MauiView)GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public ObservableCollection<MauiView> Popups { get; }
    }
}