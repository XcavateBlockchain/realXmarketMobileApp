﻿using System;
using CommunityToolkit.Mvvm.ComponentModel;
using PlutoFramework.Model;

namespace PlutoFramework.Components.Identity
{
	public partial class IdentityViewModel : ObservableObject
	{
		[ObservableProperty]
		private string name;

        [ObservableProperty]
        private string verificationIcon;

        [ObservableProperty]
        private bool verificationIconIsVisible;

        public IdentityViewModel()
		{
            name = "Loading";

            verificationIconIsVisible = false;
		}

        public async Task GetIdentityAsync(PolkadotPeople.NetApi.Generated.SubstrateClientExt client, CancellationToken token)
        {
            if (client is null)
            {
                return;
            }

            VerificationIconIsVisible = false;

            var identity = await IdentityModel.GetIdentityForAddressAsync(client, KeysModel.GetSubstrateKey(), token);

            if (identity == null)
            {
                Name = "None";
                return;
            }

            Name = identity.DisplayName;
            VerificationIconIsVisible = true;

            switch (identity.FinalJudgement)
            {
                case Judgement.Unknown:
                    if (Application.Current.RequestedTheme == AppTheme.Light)
                    {
                        VerificationIcon = "unknownblack.png";
                    }
                    else
                    {
                        VerificationIcon = "unknownwhite.png";
                    }
                    break;
                case Judgement.LowQuality:
                    if (Application.Current.RequestedTheme == AppTheme.Light)
                    {
                        VerificationIcon = "unknownblack.png";
                    }
                    else
                    {
                        VerificationIcon = "unknownwhite.png";
                    }
                    break;
                case Judgement.OutOfDate:
                    if (Application.Current.RequestedTheme == AppTheme.Light)
                    {
                        VerificationIcon = "unknownblack.png";
                    }
                    else
                    {
                        VerificationIcon = "unknownwhite.png";
                    }
                    break;
                case Judgement.Reasonable:
                    VerificationIcon = "greentick.png";
                    break;
                case Judgement.KnownGood:
                    VerificationIcon = "greentick.png";
                    break;
                case Judgement.Erroneous:
                    VerificationIcon = "redallert.png";
                    break;
            }
        }

        public void SetEmpty()
        {
            if (Name != "Loading")
            {
                return;
            }

            Name = "None";
        }
    }
}

