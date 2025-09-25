namespace ShelfMarket.UI.ViewModels
{
    public sealed class ContractCreatedEventArgs : EventArgs
    {
        public ContractCreatedEventArgs(int contractId)
        {
            ContractId = contractId;
        }

        public int ContractId { get; }
    }
}