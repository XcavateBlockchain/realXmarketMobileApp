using System.Collections.ObjectModel;
using MauiView = Microsoft.Maui.Controls.View;

namespace PlutoFramework.Components.Page
{
    public static class PageExtensions
    {
        public static readonly BindableProperty PopupsProperty =
            BindableProperty.CreateAttached(
                "Popups",
                typeof(ObservableCollection<MauiView>),
                typeof(Page),
                new ObservableCollection<MauiView>(),
                propertyChanged: OnPopupsChanged);

        public static ObservableCollection<MauiView> GetPopups(BindableObject view)
            => (ObservableCollection<MauiView>)view.GetValue(PopupsProperty);

        public static void SetPopups(BindableObject view, ObservableCollection<MauiView> value)
            => view.SetValue(PopupsProperty, value);

        private static void OnPopupsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is Page page && newValue is ObservableCollection<MauiView> newCollection)
            {
                foreach (var popup in newCollection)
                {
                    page.Popups.Add(popup);
                }
            }
        }
    }
}
