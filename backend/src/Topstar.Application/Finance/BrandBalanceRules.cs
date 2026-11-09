namespace Topstar.Application.Finance;

public static class BrandBalanceRules
{
    public static bool ValidAmount(decimal amount)=>amount>0 && amount==decimal.Truncate(amount) && amount<=999999999999999999m;
    public static bool CanSpend(decimal amount,decimal available,decimal due)=>ValidAmount(amount)&&amount<=available&&amount<=due;
    public static bool CanWithdraw(decimal amount,decimal available,decimal minimum,decimal fee)=>ValidAmount(amount)&&amount<=available&&amount>=minimum&&fee>=0&&fee<amount;
    public static decimal Refundable(decimal budget,decimal allocated,decimal approvedRefunds,decimal alreadyReturned,bool cancelled)
        =>Math.Max(0,approvedRefunds+(cancelled?Math.Max(0,budget-allocated):0)-alreadyReturned);
}
