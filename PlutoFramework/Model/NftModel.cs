﻿using Nethereum.Util;
using PlutoFramework.Components.Buttons;
using PlutoFramework.Components.Nft;
using PlutoFramework.Constants;
using PlutoFramework.Model;
using PlutoFramework.Model.SQLite;
using System.Collections.ObjectModel;
using UniqueryPlus.Collections;
using UniqueryPlus.External;
using UniqueryPlus.Nfts;

namespace PlutoFramework
{
    internal class NftModel
    {
        public static async Task NavigateToNftDetailPageAsync(INftBase nftBase, Endpoint endpoint, bool favourite, CancellationToken token)
        {
            try
            {
                var viewModel = new NftDetailViewModel();

                viewModel.Endpoint = endpoint;
                viewModel.CollectionId = nftBase.CollectionId;
                viewModel.ItemId = nftBase.Id;
                viewModel.Favourite = favourite;
                viewModel.OwnerAddress = nftBase.Owner;

                var savedCollection = await CollectionDatabase.GetCollectionAsync($"{nftBase.Type}-{nftBase.CollectionId}");

                if (savedCollection is not null)
                {
                    viewModel.CollectionFavourite = savedCollection.Favourite;
                    viewModel.CollectionBase = savedCollection.CollectionBase;
                    viewModel.CollectionNftImages = savedCollection.NftImages;
                }

                await UpdateViewModelAsync(viewModel, nftBase, token);

                if (endpoint.Key == EndpointEnum.AzeroTestnet)
                {
                    viewModel.AzeroIdReservedUntil = await Model.AzeroId.AzeroIdModel.GetReservedUntilStringForName(nftBase.Metadata.Name).ConfigureAwait(false);
                }

                await Application.Current.MainPage.Navigation.PushAsync(new NftDetailPage(viewModel));

                // load these details after
                viewModel.KodadotUnlockableUrl = await Model.Kodadot.UnlockablesModel.FetchKeywiseAsync(endpoint, nftBase.CollectionId).ConfigureAwait(false);

                try
                {
                    if (savedCollection is null)
                    {
                        savedCollection = await Model.CollectionModel.ToCollectionWrapperAsync(await nftBase.GetCollectionAsync(token).ConfigureAwait(false), CancellationToken.None).ConfigureAwait(false);

                        viewModel.CollectionBase = savedCollection.CollectionBase;
                        viewModel.CollectionNftImages = savedCollection.NftImages;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                ICollectionBase fullCollection = null;
                try
                {
                    fullCollection = await savedCollection.CollectionBase.GetFullAsync(token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                if (fullCollection is not null && fullCollection is ICollectionStats)
                {
                    viewModel.FloorPrice = ((ICollectionStats)fullCollection).FloorPrice;
                    viewModel.HighestSale = ((ICollectionStats)fullCollection).HighestSale;
                    viewModel.Volume = ((ICollectionStats)fullCollection).Volume;
                }

                var fullNft = await nftBase.GetFullAsync(token).ConfigureAwait(false);

                if (fullNft is INftNestable)
                {
                    viewModel.IsNestable = true;
                    viewModel.NestedNfts = new ObservableCollection<NftWrapper>((await ((INftNestable)fullNft).GetNestedNftsAsync(token).ConfigureAwait(false)).Select(nestedNftWrapper => Model.NftModel.ToNftWrapper(nestedNftWrapper.NftBase)));
                }

                if (fullNft is INftNestable && (fullNft as INftNestable).HasParentNft)
                {
                    viewModel.HasParentNft = true;
                    viewModel.ParentNftBase = await (fullNft as INftNestable).GetParentNftAsync(token).ConfigureAwait(false);
                    viewModel.ParentNftFavourite = await NftDatabase.IsNftFavouriteAsync(viewModel.ParentNftBase.Type, viewModel.ParentNftBase.CollectionId, viewModel.ParentNftBase.Id).ConfigureAwait(false);
                }

                if (
                    fullNft is INftNestable &&
                    fullCollection is not null &&
                    fullCollection is ICollectionNestable &&
                    (
                        (((ICollectionNestable)fullCollection).IsNestableByTokenOwner && fullNft.Owner == KeysModel.GetSubstrateKey()) ||
                        (((ICollectionNestable)fullCollection).IsNestableByCollectionOwner && fullCollection.Owner == KeysModel.GetSubstrateKey())
                    )
                )
                {
                    viewModel.NestButtonState = ButtonStateEnum.Enabled;
                }

                await UpdateViewModelAsync(viewModel, fullNft, token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static async Task UpdateViewModelAsync(NftDetailViewModel viewModel, INftBase nft, CancellationToken token)
        {
            viewModel.NftBase = nft;

            viewModel.Price = nft switch
            {
                INftBuyable nftBuyable => nftBuyable.IsForSale ? nftBuyable.Price ?? 0 : 0,
                INftBuyableWithReceiver nftBuyableWithReceiver => nftBuyableWithReceiver.IsForSale ? nftBuyableWithReceiver.Price ?? 0 : 0,
                INftEVMBuyableWithReceiver nftEVMBuyableWithReceiver => nftEVMBuyableWithReceiver.IsForSale ? nftEVMBuyableWithReceiver.Price ?? 0 : 0,
                _ => 0
            };

            viewModel.IsForSale = (nft is INftBuyable && ((INftBuyable)nft).IsForSale) ||
                (nft is INftBuyableWithReceiver && ((INftBuyableWithReceiver)nft).IsForSale) ||
                (nft is INftEVMBuyableWithReceiver && ((INftEVMBuyableWithReceiver)nft).IsForSale);

            viewModel.KodaIsVisible = nft is IKodaLink;
            viewModel.UniqueIsVisible = nft is IUniqueMarketplaceLink;

            viewModel.TransferButtonState = (nft is INftTransferable && ((INftTransferable)nft).IsTransferable) ? ButtonStateEnum.Enabled : ButtonStateEnum.Disabled;
            viewModel.SellButtonState = nft is INftSellable || nft is INftEVMSellable ? ButtonStateEnum.Enabled : ButtonStateEnum.Disabled;

            viewModel.ModifyButtonState = ButtonStateEnum.Disabled; // Maybe later
            viewModel.BurnButtonState = nft is INftBurnable && ((INftBurnable)nft).IsBurnable ? ButtonStateEnum.Enabled : ButtonStateEnum.Disabled;
        }

        public static async Task NavigateToCollectionDetailPageAsync(ICollectionBase collectionBase, Endpoint endpoint, bool favourite, CancellationToken token)
        {
            try
            {
                var viewModel = new CollectionDetailViewModel();

                viewModel.Endpoint = endpoint;
                viewModel.CollectionId = collectionBase.CollectionId;
                viewModel.Favourite = favourite;
                viewModel.OwnerAddress = collectionBase.Owner;

                await UpdateViewModelAsync(viewModel, collectionBase, token);

                await Application.Current.MainPage.Navigation.PushAsync(new CollectionDetailPage(viewModel));

                var fullCollection = await collectionBase.GetFullAsync(token);

                await UpdateViewModelAsync(viewModel, fullCollection, token);

                viewModel.Nfts = new ObservableCollection<NftWrapper>((await fullCollection.GetNftsAsync(25, null, token)).Select(Model.NftModel.ToNftWrapper));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static async Task UpdateViewModelAsync(CollectionDetailViewModel viewModel, ICollectionBase collection, CancellationToken token)
        {
            if (collection is ICollectionStats)
            {
                viewModel.FloorPrice = ((ICollectionStats)collection).FloorPrice;
                viewModel.HighestSale = ((ICollectionStats)collection).HighestSale;
                viewModel.Volume = ((ICollectionStats)collection).Volume;
            }

            viewModel.KodaIsVisible = collection is IKodaLink;
            viewModel.UniqueIsVisible = collection is IUniqueMarketplaceLink;

            viewModel.TransferButtonState = collection is ICollectionTransferable && ((ICollectionTransferable)collection).IsTransferable ? ButtonStateEnum.Enabled : ButtonStateEnum.Disabled;
            viewModel.ModifyButtonState = ButtonStateEnum.Disabled; // Maybe later

            viewModel.CollectionBase = collection;

            if (collection is ICollectionEVMClaimable)
            {
                var eventInfo = await ((ICollectionEVMClaimable)collection).GetEventInfoAsync(token).ConfigureAwait(false);

                if (eventInfo is not null)
                {
                    viewModel.EventStartTimestamp = (long)eventInfo.StartTimestamp;
                    viewModel.EventEndTimestamp = (long)eventInfo.EndTimestamp;

                    var timestampNow = DateTime.Now.ToUnixTimestamp();

                    var canBeClaimed = (eventInfo.StartTimestamp <= timestampNow && timestampNow < eventInfo.EndTimestamp);
                    Console.WriteLine("Can be claimed" + canBeClaimed);
                    viewModel.ClaimButtonState = canBeClaimed ? ButtonStateEnum.Enabled : ButtonStateEnum.Disabled;
                }
                else
                {
                    Console.WriteLine("EVM event info was null");
                }
            }

            if (collection is ICollectionMintConfig)
            {
                viewModel.MintPrice = (collection as ICollectionMintConfig).MintPrice;
                viewModel.MintStartBlock = (collection as ICollectionMintConfig).MintStartBlock;
                viewModel.MintEndBlock = (collection as ICollectionMintConfig).MintEndBlock;
                viewModel.MintType = (collection as ICollectionMintConfig).MintType;
                viewModel.NftMaxSuply = (collection as ICollectionMintConfig).NftMaxSuply;
            }

            if (collection is ICollectionCreatedAt)
            {
                viewModel.DateOfCreation = (collection as ICollectionCreatedAt).CreatedAt.UtcDateTime;
            }

            if (collection is ICollectionNftsSoulbound)
            {
                viewModel.NftsSoulbound = (collection as ICollectionNftsSoulbound).NftsSoulbound;
            }
        }
    }
}
