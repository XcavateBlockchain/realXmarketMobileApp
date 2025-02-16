//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using InvArch.NetApi.Generated.Storage;
using Substrate.NetApi.Model.Extrinsics;
using Substrate.NetApi.Model.Meta;
using Substrate.NetApi.Model.Types.Base;
using System.Collections.Generic;


namespace InvArch.NetApi.Generated
{
    
    
    /// <summary>
    /// >> Substrate Client Extension, including all Storage classes direct access.
    /// </summary>
    public sealed class SubstrateClientExt : Substrate.NetApi.SubstrateClient
    {
        
        /// <summary>
        /// StorageKeyDict for key definition informations.
        /// </summary>
        public System.Collections.Generic.Dictionary<System.Tuple<string, string>, System.Tuple<Substrate.NetApi.Model.Meta.Storage.Hasher[], System.Type, System.Type>> StorageKeyDict;
        
        /// <summary>
        /// SystemStorage storage calls.
        /// </summary>
        public SystemStorage SystemStorage;
        
        /// <summary>
        /// ParachainSystemStorage storage calls.
        /// </summary>
        public ParachainSystemStorage ParachainSystemStorage;
        
        /// <summary>
        /// TimestampStorage storage calls.
        /// </summary>
        public TimestampStorage TimestampStorage;
        
        /// <summary>
        /// ParachainInfoStorage storage calls.
        /// </summary>
        public ParachainInfoStorage ParachainInfoStorage;
        
        /// <summary>
        /// SudoStorage storage calls.
        /// </summary>
        public SudoStorage SudoStorage;
        
        /// <summary>
        /// UtilityStorage storage calls.
        /// </summary>
        public UtilityStorage UtilityStorage;
        
        /// <summary>
        /// TxPauseStorage storage calls.
        /// </summary>
        public TxPauseStorage TxPauseStorage;
        
        /// <summary>
        /// RandomnessCollectiveFlipStorage storage calls.
        /// </summary>
        public RandomnessCollectiveFlipStorage RandomnessCollectiveFlipStorage;
        
        /// <summary>
        /// BalancesStorage storage calls.
        /// </summary>
        public BalancesStorage BalancesStorage;
        
        /// <summary>
        /// TransactionPaymentStorage storage calls.
        /// </summary>
        public TransactionPaymentStorage TransactionPaymentStorage;
        
        /// <summary>
        /// TreasuryStorage storage calls.
        /// </summary>
        public TreasuryStorage TreasuryStorage;
        
        /// <summary>
        /// VestingStorage storage calls.
        /// </summary>
        public VestingStorage VestingStorage;
        
        /// <summary>
        /// AssetRegistryStorage storage calls.
        /// </summary>
        public AssetRegistryStorage AssetRegistryStorage;
        
        /// <summary>
        /// CurrenciesStorage storage calls.
        /// </summary>
        public CurrenciesStorage CurrenciesStorage;
        
        /// <summary>
        /// TokensStorage storage calls.
        /// </summary>
        public TokensStorage TokensStorage;
        
        /// <summary>
        /// XTokensStorage storage calls.
        /// </summary>
        public XTokensStorage XTokensStorage;
        
        /// <summary>
        /// AuthorshipStorage storage calls.
        /// </summary>
        public AuthorshipStorage AuthorshipStorage;
        
        /// <summary>
        /// CollatorSelectionStorage storage calls.
        /// </summary>
        public CollatorSelectionStorage CollatorSelectionStorage;
        
        /// <summary>
        /// SessionStorage storage calls.
        /// </summary>
        public SessionStorage SessionStorage;
        
        /// <summary>
        /// AuraStorage storage calls.
        /// </summary>
        public AuraStorage AuraStorage;
        
        /// <summary>
        /// AuraExtStorage storage calls.
        /// </summary>
        public AuraExtStorage AuraExtStorage;
        
        /// <summary>
        /// XcmpQueueStorage storage calls.
        /// </summary>
        public XcmpQueueStorage XcmpQueueStorage;
        
        /// <summary>
        /// PolkadotXcmStorage storage calls.
        /// </summary>
        public PolkadotXcmStorage PolkadotXcmStorage;
        
        /// <summary>
        /// CumulusXcmStorage storage calls.
        /// </summary>
        public CumulusXcmStorage CumulusXcmStorage;
        
        /// <summary>
        /// OrmlXcmStorage storage calls.
        /// </summary>
        public OrmlXcmStorage OrmlXcmStorage;
        
        /// <summary>
        /// MessageQueueStorage storage calls.
        /// </summary>
        public MessageQueueStorage MessageQueueStorage;
        
        /// <summary>
        /// IdentityStorage storage calls.
        /// </summary>
        public IdentityStorage IdentityStorage;
        
        /// <summary>
        /// ContractsStorage storage calls.
        /// </summary>
        public ContractsStorage ContractsStorage;
        
        /// <summary>
        /// CheckedInflationStorage storage calls.
        /// </summary>
        public CheckedInflationStorage CheckedInflationStorage;
        
        /// <summary>
        /// OcifStakingStorage storage calls.
        /// </summary>
        public OcifStakingStorage OcifStakingStorage;
        
        /// <summary>
        /// INV4Storage storage calls.
        /// </summary>
        public INV4Storage INV4Storage;
        
        /// <summary>
        /// CoreAssetsStorage storage calls.
        /// </summary>
        public CoreAssetsStorage CoreAssetsStorage;
        
        public SubstrateClientExt(System.Uri uri, Substrate.NetApi.Model.Extrinsics.ChargeType chargeType) : 
                base(uri, chargeType)
        {
            StorageKeyDict = new System.Collections.Generic.Dictionary<System.Tuple<string, string>, System.Tuple<Substrate.NetApi.Model.Meta.Storage.Hasher[], System.Type, System.Type>>();
            this.SystemStorage = new SystemStorage(this);
            this.ParachainSystemStorage = new ParachainSystemStorage(this);
            this.TimestampStorage = new TimestampStorage(this);
            this.ParachainInfoStorage = new ParachainInfoStorage(this);
            this.SudoStorage = new SudoStorage(this);
            this.UtilityStorage = new UtilityStorage(this);
            this.TxPauseStorage = new TxPauseStorage(this);
            this.RandomnessCollectiveFlipStorage = new RandomnessCollectiveFlipStorage(this);
            this.BalancesStorage = new BalancesStorage(this);
            this.TransactionPaymentStorage = new TransactionPaymentStorage(this);
            this.TreasuryStorage = new TreasuryStorage(this);
            this.VestingStorage = new VestingStorage(this);
            this.AssetRegistryStorage = new AssetRegistryStorage(this);
            this.CurrenciesStorage = new CurrenciesStorage(this);
            this.TokensStorage = new TokensStorage(this);
            this.XTokensStorage = new XTokensStorage(this);
            this.AuthorshipStorage = new AuthorshipStorage(this);
            this.CollatorSelectionStorage = new CollatorSelectionStorage(this);
            this.SessionStorage = new SessionStorage(this);
            this.AuraStorage = new AuraStorage(this);
            this.AuraExtStorage = new AuraExtStorage(this);
            this.XcmpQueueStorage = new XcmpQueueStorage(this);
            this.PolkadotXcmStorage = new PolkadotXcmStorage(this);
            this.CumulusXcmStorage = new CumulusXcmStorage(this);
            this.OrmlXcmStorage = new OrmlXcmStorage(this);
            this.MessageQueueStorage = new MessageQueueStorage(this);
            this.IdentityStorage = new IdentityStorage(this);
            this.ContractsStorage = new ContractsStorage(this);
            this.CheckedInflationStorage = new CheckedInflationStorage(this);
            this.OcifStakingStorage = new OcifStakingStorage(this);
            this.INV4Storage = new INV4Storage(this);
            this.CoreAssetsStorage = new CoreAssetsStorage(this);
        }
    }
}
