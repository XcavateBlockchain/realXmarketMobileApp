﻿using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using PlutoFramework.Model;

namespace PlutoFramework.Components.CustomLayouts
{
	public partial class AddCustomItemViewModel : ObservableObject
	{
        [ObservableProperty]
        private ObservableCollection<ComponentInfo> componentInfos = new ObservableCollection<ComponentInfo>();

        public AddCustomItemViewModel()
        {
            try
            {
                var selectedItemInfos = Model.CustomLayoutModel.ParsePlutoComponentInfos(
                    Preferences.Get("PlutoLayout",
                    Model.CustomLayoutModel.DEFAULT_PLUTO_LAYOUT)
                );

                var allItemInfos = Model.CustomLayoutModel.ParsePlutoComponentInfos(Model.CustomLayoutModel.AllComponentsString);

                for(int i = 0; i < allItemInfos.Count(); i++)
                {
                    foreach(var selectedItem in selectedItemInfos)
                    {

                        if (allItemInfos[i].ComponentId == selectedItem.ComponentId)
                        {
                            Console.WriteLine("Removed: " + allItemInfos[i].ComponentId);
                            allItemInfos.RemoveAt(i);
                            i--;
                            break;
                        }
                    }
                }

                componentInfos = allItemInfos;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Layout Error");

                Console.WriteLine(ex);
                //componentInfos = Model.CustomLayoutModel.ParsePlutoComponentInfos(Model.CustomLayoutModel.AllComponentsString);
            }
        }
	}
}

